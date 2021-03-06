// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.NodejsTools.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using TestUtilities.Nodejs;

namespace TestAdapterTests
{
    [TestClass]
    public class TestExecutorTests
    {
        [ClassInitialize]
        public static void DoDeployment(TestContext context)
        {
            AssertListener.Initialize();
            NodejsTestData.Deploy();
        }

        [TestMethod, Priority(0), TestCategory("Ignore")]
        public void Run()
        {
            var executor = new TestExecutor();
            var recorder = new MockTestExecutionRecorder();
            var runContext = new MockRunContext();
            var expectedTests = TestInfo.TestAdapterATests.Concat(TestInfo.TestAdapterBTests).ToArray();
            var testCases = expectedTests.Select(tr => tr.TestCase);

            executor.RunTests(testCases, runContext, recorder);
            PrintTestResults(recorder.Results);

            foreach (var expectedResult in expectedTests)
            {
                var actualResult = recorder.Results.SingleOrDefault(tr => tr.TestCase.FullyQualifiedName == expectedResult.TestCase.FullyQualifiedName);

                Assert.IsNotNull(actualResult);
                Assert.AreEqual(expectedResult.Outcome, actualResult.Outcome, expectedResult.TestCase.FullyQualifiedName + " had incorrect result");
            }
        }

        [TestMethod, Priority(0), TestCategory("Ignore")]
        public void RunAll()
        {
            var executor = new TestExecutor();
            var recorder = new MockTestExecutionRecorder();
            var runContext = new MockRunContext();
            var expectedTests = TestInfo.TestAdapterATests.Concat(TestInfo.TestAdapterBTests).ToArray();

            executor.RunTests(new[] { TestInfo.TestAdapterLibProjectFilePath, TestInfo.TestAdapterAProjectFilePath, TestInfo.TestAdapterBProjectFilePath }, runContext, recorder);
            PrintTestResults(recorder.Results);

            foreach (var expectedResult in expectedTests)
            {
                var actualResult = recorder.Results.SingleOrDefault(tr => tr.TestCase.FullyQualifiedName == expectedResult.TestCase.FullyQualifiedName);

                Assert.IsNotNull(actualResult, expectedResult.TestCase.FullyQualifiedName + " not found in results");
                Assert.AreEqual(expectedResult.Outcome, actualResult.Outcome, expectedResult.TestCase.FullyQualifiedName + " had incorrect result");
            }
        }

        [TestMethod, Priority(0)]
        public void Cancel()
        {
            var executor = new TestExecutor();
            var recorder = new MockTestExecutionRecorder();
            var runContext = new MockRunContext();
            var expectedTests = TestInfo.TestAdapterATests.Union(TestInfo.TestAdapterBTests).ToArray();
            var testCases = expectedTests.Select(tr => tr.TestCase);

            var thread = new System.Threading.Thread(o =>
            {
                executor.RunTests(testCases, runContext, recorder);
            });
            thread.Start();

            // One of the tests being run is hard coded to take 10 secs
            Assert.IsTrue(thread.IsAlive);

            System.Threading.Thread.Sleep(100);

            executor.Cancel();
            System.Threading.Thread.Sleep(100);

            // It should take less than 10 secs to cancel
            // Depending on which assemblies are loaded, it may take some time
            // to obtain the interpreters service.
            Assert.IsTrue(thread.Join(10000));

            System.Threading.Thread.Sleep(100);

            Assert.IsFalse(thread.IsAlive);

            // Canceled test cases do not get recorded
            Assert.IsTrue(recorder.Results.Count < expectedTests.Length);
        }

        private static void PrintTestResults(IEnumerable<TestResult> results)
        {
            foreach (var result in results)
            {
                Console.WriteLine("Test: " + result.TestCase.FullyQualifiedName);
                Console.WriteLine("Result: " + result.Outcome);
                foreach (var msg in result.Messages)
                {
                    Console.WriteLine("Message " + msg.Category + ":");
                    Console.WriteLine(msg.Text);
                }
                Console.WriteLine("");
            }
        }
    }
}

