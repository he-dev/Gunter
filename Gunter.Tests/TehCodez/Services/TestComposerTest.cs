﻿using System;
using System.Linq;
using Gunter.Data;
using Gunter.Reporting;
using Gunter.Services;
using Gunter.Tests.Data.Fakes;
using Gunter.Tests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gunter.Tests.Services
{
    [TestClass]
    public class TestComposerTest
    {
        [TestMethod]
        public void ComposeTests_AllOptions_TestUnits()
        {
            var testFile = new TestFile
            {
                DataSources = { new FakeDataSource(2) },
                Tests =
                {
                    new TestCase
                    {
                        Enabled = true,
                        Severity = TestSeverity.Info,
                        Message = "Service runs smoothly.",
                        DataSources = { 2 },
                        Filter = "[LogLevel] = 'info' AND [Environment] = 'test'",
                        Expression = "COUNT([LogLevel]) = 2",
                        Assert = true,
                        ContinueOnFailure = false,
                        Alerts = { 1 }
                    }
                },
                Alerts = { new TestAlert { Id = 1, Reports = { 2 }} },
                Reports = { new Report { Id = 2 } }
            };

            var testUnits = TestComposer.ComposeTests(testFile, VariableResolver.Empty).ToList();
            Assert.AreEqual(1, testUnits.Count);
            Assert.AreEqual(2, testUnits.Single().DataSource.Id);
            Assert.AreEqual(1, testUnits.Single().Alerts.Count());
            Assert.AreEqual(1, testUnits.Single().Reports.Count());
        }
    }
}
