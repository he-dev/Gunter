using System;
using System.Data;
using Gunter.Data;
using Gunter.Data.Configuration;

namespace Gunter.Workflow.Data
{
    public class TestContext : IDisposable
    {
        public TestContext(Theory theory, TestCase testCase, IQuery query)
        {
            Theory = theory;
            TestCase = testCase;
            Query = query;
        }

        public Theory Theory { get; }

        public TestCase TestCase { get; }

        public IQuery Query { get; }

        public string QueryCommand { get; set; }

        public DataTable? Data { get; set; }

        public TimeSpan GetDataElapsed { get; set; }

        public TimeSpan FilterDataElapsed { get; set; }

        public TimeSpan EvaluateDataElapsed { get; set; }

        public TestResult Result { get; set; } = TestResult.Undefined;

        public void Dispose() => Data?.Dispose();
    }
}