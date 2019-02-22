﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionizer;
using Reusable.IOnymous;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    public interface ITestRunner
    {
        Task RunAsync
        (
            [NotNull, ItemNotNull] IEnumerable<TestBundle> testBundles
        );
    }

    [UsedImplicitly]
    internal class TestRunner : ITestRunner
    {
        private readonly ILogger _logger;
        private readonly RuntimeVariableDictionaryFactory _runtimeVariableDictionaryFactory;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            RuntimeVariableDictionaryFactory runtimeVariableDictionaryFactory
        )
        {
            _logger = logger;
            _runtimeVariableDictionaryFactory = runtimeVariableDictionaryFactory;
        }

        public async Task RunAsync(IEnumerable<TestBundle> testBundles)
        {
            var actions = new ActionBlock<TestBundle>
            (
                RunAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                }
            );

            foreach (var testBundle in testBundles)
            {
                await actions.SendAsync(testBundle);
            }

            actions.Complete();
            await actions.Completion;
        }

        private async Task RunAsync(TestBundle testBundle)
        {
            var tests =
                from testCase in testBundle.Tests
                from dataSource in testCase.DataSources(testBundle)
                select (testCase, dataSource);

            var runtimeVariables = _runtimeVariableDictionaryFactory.Create(new object[] { testBundle }, testBundle.Variables.Flatten());

            var cache = new Dictionary<SoftString, (DataTable Data, string Query, TimeSpan Elapsed)>();

            using (_logger.BeginScope().WithCorrelationHandle("TestBundle").AttachElapsed())
            using (Disposable.Create(() =>
            {
                foreach (var (data, _, _) in cache.Values)
                {
                    data.Dispose();
                }
            }))
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { TestBundleFileName = testBundle.FileName }));
                foreach (var current in tests)
                {
                    using (_logger.BeginScope().WithCorrelationHandle("TestCase").AttachElapsed())
                    {
                        _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { TestCaseId = current.testCase.Id }));
                        try
                        {
                            if (!cache.TryGetValue(current.dataSource.Id, out var cacheItem))
                            {
                                var getDataStopwatch = Stopwatch.StartNew();
                                var (data, query) = await current.dataSource.GetDataAsync(testBundle.DirectoryName, runtimeVariables);
                                cache[current.dataSource.Id] = cacheItem = (data, query, getDataStopwatch.Elapsed);
                            }

                            var (result, runElapsed, when) = RunTest(current.testCase, cacheItem.Data);

                            var context = new TestContext
                            {
                                TestBundle = testBundle,
                                TestCase = current.testCase,
                                TestWhen = when,
                                DataSource = current.dataSource,
                                Data = cacheItem.Data,
                                RuntimeVariables = _runtimeVariableDictionaryFactory.Create
                                (
                                    new object[]
                                    {
                                        testBundle,
                                        current.testCase,
                                        //current.dataSource, // todo - not used - should be query
                                        new TestCounter
                                        {
                                            GetDataElapsed = cacheItem.Elapsed,
                                            RunTestElapsed = runElapsed
                                        },
                                    },
                                    testBundle.Variables.Flatten()
                                ),
                                Query = cacheItem.Query,
                                Result = result
                            };

                            var messengers =
                                context
                                    .TestWhen
                                    .Messengers(context.TestBundle)
                                    .Select(messenger => messenger.SendAsync(context));
                            
                            await Task.WhenAll(messengers);

                            if (when.Halt)
                            {
                                break;
                            }

                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Completed());
                        }
                        catch (DynamicException ex) when (ex.NameMatches("^DataSource"))
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Faulted(), ex);
                            // It'd be pointless to continue when there is no data.
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(nameof(RunAsync)).Faulted(), ex);
                        }
                    }
                }
            }
        }

        private (TestResult Result, TimeSpan Elapsed, TestWhen When) RunTest(TestCase testCase, DataTable data)
        {
            var assertStopwatch = Stopwatch.StartNew();
            if (!(data.Compute(testCase.Assert, testCase.Filter) is bool result))
            {
                throw DynamicException.Create("Assert", $"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
            }

            var assertElapsed = assertStopwatch.Elapsed;

            var testResult = result ? TestResult.Passed : TestResult.Failed;

            _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { Result = testResult }));

            return
                testCase.When.TryGetValue(testResult, out var when)
                    ? (testResult, assertElapsed, when)
                    : (testResult, assertElapsed, default);
        }
    }
}