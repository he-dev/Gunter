﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Gunter.Data;
using Gunter.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Reusable.Commander;
using Reusable.Exceptionize;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
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
        private readonly ICommandExecutor _commandLineExecutor;
        private readonly RuntimePropertyProvider _runtimePropertyProvider;

        public TestRunner
        (
            ILogger<TestRunner> logger,
            ICommandExecutor commandLineExecutor,
            RuntimePropertyProvider runtimePropertyProvider
        )
        {
            _logger = logger;
            _commandLineExecutor = commandLineExecutor;
            _runtimePropertyProvider = runtimePropertyProvider;
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

            var testBundleRuntimeVariables =
                _runtimePropertyProvider
                    .AddObjects(new object[] { testBundle })
                    .AddProperties(testBundle.Variables.Flatten());

            using (_logger.BeginScope().WithCorrelationHandle("ProcessTestBundle").UseStopwatch())
            using (var cache = new MemoryCache(new MemoryCacheOptions()))
            {
                _logger.Log(Abstraction.Layer.Service().Meta(new { TestFileName = testBundle.FileName }));
                foreach (var current in tests)
                {
                    using (_logger.BeginScope().WithCorrelationHandle("ProcessTestCase").UseStopwatch())
                    {
                        _logger.Log(Abstraction.Layer.Service().Meta(new { TestCaseId = current.testCase.Id }));
                        try
                        {
                            if (!cache.TryGetValue<Snapshot>(current.dataSource.Id, out var logView))
                            {
                                cache.Set(current.dataSource.Id, logView = await current.dataSource.ExecuteAsync(testBundleRuntimeVariables));
                            }

                            var (result, runElapsed, commands) = RunTest(current.testCase, logView.Data);

                            var context = new TestContext
                            {
                                TestBundle = testBundle,
                                TestCase = current.testCase,
                                //TestWhen = when,
                                Query = current.dataSource,
                                Command = logView.Command,
                                Data = logView.Data,
                                Result = result,
                                RuntimeProperties =
                                    _runtimePropertyProvider
                                        .AddObjects(
                                            testBundle,
                                            current.testCase,
                                            new TestCounter
                                            {
                                                GetDataElapsed = logView.GetDataElapsed,
                                                RunTestElapsed = runElapsed
                                            })
                                        .AddProperties(testBundle.Variables.Flatten())
                            };

                            foreach (var cmd in commands)
                            {
                                await _commandLineExecutor.ExecuteAsync(cmd, context);
                            }

                            _logger.Log(Abstraction.Layer.Business().Routine(_logger.Scope().CorrelationHandle.ToString()).Completed());
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (DynamicException ex) when (ex.NameMatches("^DataSource"))
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(_logger.Scope().CorrelationHandle.ToString()).Faulted(), ex);
                            // It'd be pointless to continue when there is no data.
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(Abstraction.Layer.Business().Routine(_logger.Scope().CorrelationHandle.ToString()).Faulted(), ex);
                        }
                    }
                }

                _logger.Log(Abstraction.Layer.Business().Routine(_logger.Scope().CorrelationHandle.ToString()).Completed());
            }
        }

        private (TestResult Result, TimeSpan Elapsed, IList<string> Commands) RunTest(TestCase testCase, DataTable data)
        {
            using (_logger.BeginScope().WithCorrelationHandle("RunTest").UseStopwatch())
            {
                if (!(data.Compute(testCase.Assert, testCase.Filter) is bool result))
                {
                    throw DynamicException.Create("Assert", $"'{nameof(TestCase.Assert)}' must evaluate to '{nameof(Boolean)}'.");
                }

                var assertElapsed = _logger.Scope().Stopwatch().Elapsed;
                var testResult =
                    result
                        ? TestResult.Passed
                        : TestResult.Failed;

                _logger.Log(Abstraction.Layer.Service().Meta(new { TestResult = testResult }));

                return
                    testCase.When.TryGetValue(testResult, out var then)
                        ? (testResult, assertElapsed, then)
                        : (testResult, assertElapsed, new List<string>());
            }
        }
    }
}