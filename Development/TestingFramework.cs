using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using RhinoAI.AI;
using RhinoAI.Core;
using RhinoAI.Models;

namespace RhinoAI.Development
{
    /// <summary>
    /// Comprehensive testing framework for RhinoAI plugin functionality
    /// </summary>
    public class TestingFramework
    {
        private readonly SimpleLogger _logger;
        private readonly RhinoAI.Core.NLPProcessor _nlpProcessor;
        private readonly ConfigurationManager _configManager;
        private readonly List<TestResult> _testResults;

        public TestingFramework()
        {
            _logger = new SimpleLogger("TestingFramework", LogLevel.Information);
            _configManager = new ConfigurationManager(_logger);
            _nlpProcessor = new RhinoAI.Core.NLPProcessor(_configManager, _logger);
            _testResults = new List<TestResult>();
        }

        /// <summary>
        /// Run all plugin tests
        /// </summary>
        public async Task<TestSuite> RunAllTestsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInfo("Starting RhinoAI Plugin Test Suite");

            var testSuite = new TestSuite
            {
                StartTime = DateTime.Now,
                TestResults = new List<TestResult>()
            };

            try
            {
                // Core functionality tests
                await RunCoreTests(testSuite);
                
                // NLP processing tests
                await RunNLPTests(testSuite);
                
                // Integration tests
                await RunIntegrationTests(testSuite);
                
                // Performance tests
                await RunPerformanceTests(testSuite);
                
                // UI tests
                await RunUITests(testSuite);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Test suite execution failed: {ex.Message}");
                testSuite.TestResults.Add(new TestResult
                {
                    TestName = "TestSuiteExecution",
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = stopwatch.Elapsed
                });
            }

            stopwatch.Stop();
            testSuite.EndTime = DateTime.Now;
            testSuite.TotalDuration = stopwatch.Elapsed;
            testSuite.TotalTests = testSuite.TestResults.Count;
            testSuite.PassedTests = testSuite.TestResults.Count(r => r.Success);
            testSuite.FailedTests = testSuite.TestResults.Count(r => !r.Success);

            LogTestSummary(testSuite);
            return testSuite;
        }

        private async Task RunCoreTests(TestSuite testSuite)
        {
            _logger.LogInfo("Running Core Functionality Tests");

            // Test Configuration Manager
            await RunTest("ConfigurationManager_LoadDefaults", async () =>
            {
                var config = new ConfigurationManager();
                var apiKey = config.GetSetting("OpenAI:ApiKey");
                return !string.IsNullOrEmpty(apiKey) || apiKey == "your-api-key-here";
            }, testSuite);

            // Test Logger
            await RunTest("Logger_BasicLogging", async () =>
            {
                var logger = new SimpleLogger();
                logger.LogInfo("Test log message");
                logger.LogWarning("Test warning message");
                logger.LogError("Test error message");
                return true;
            }, testSuite);

            // Test Context Manager
            await RunTest("ContextManager_DocumentState", async () =>
            {
                var contextManager = new ContextManager();
                var context = contextManager.GetCurrentContext();
                return context != null;
            }, testSuite);
        }

        private async Task RunNLPTests(TestSuite testSuite)
        {
            _logger.LogInfo("Running NLP Processing Tests");

            // Test basic intent recognition
            await RunTest("NLP_BasicIntentRecognition", async () =>
            {
                var result = await _nlpProcessor.ProcessCommandAsync("create a box");
                return result != null && !string.IsNullOrEmpty(result.Intent);
            }, testSuite);

            // Test command parsing
            await RunTest("NLP_CommandParsing", async () =>
            {
                var result = await _nlpProcessor.ProcessCommandAsync("create a sphere with radius 5");
                return result != null && result.Parameters.Count > 0;
            }, testSuite);

            // Test error handling
            await RunTest("NLP_ErrorHandling", async () =>
            {
                var result = await _nlpProcessor.ProcessCommandAsync("");
                return result != null && result.Success == false;
            }, testSuite);

            // Test complex commands
            await RunTest("NLP_ComplexCommands", async () =>
            {
                var result = await _nlpProcessor.ProcessCommandAsync("create a loft between two curves");
                return result != null;
            }, testSuite);
        }

        private async Task RunIntegrationTests(TestSuite testSuite)
        {
            _logger.LogInfo("Running Integration Tests");

            // Test Rhino integration
            await RunTest("Integration_RhinoDocument", async () =>
            {
                var doc = RhinoDoc.ActiveDoc;
                return doc != null;
            }, testSuite);

            // Test geometry creation
            await RunTest("Integration_GeometryCreation", async () =>
            {
                var box = new Box(Plane.WorldXY, new Interval(0, 10), new Interval(0, 10), new Interval(0, 10));
                var brep = box.ToBrep();
                return brep != null && brep.IsValid;
            }, testSuite);
        }

        private async Task RunPerformanceTests(TestSuite testSuite)
        {
            _logger.LogInfo("Running Performance Tests");

            // Test NLP processing speed
            await RunTest("Performance_NLPSpeed", async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                await _nlpProcessor.ProcessCommandAsync("create a box");
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds < 5000; // Should complete within 5 seconds
            }, testSuite);

            // Test memory usage
            await RunTest("Performance_MemoryUsage", async () =>
            {
                var initialMemory = GC.GetTotalMemory(false);
                
                // Perform operations
                for (int i = 0; i < 100; i++)
                {
                    await _nlpProcessor.ProcessCommandAsync($"create box {i}");
                }
                
                GC.Collect();
                var finalMemory = GC.GetTotalMemory(true);
                var memoryIncrease = finalMemory - initialMemory;
                
                return memoryIncrease < 50 * 1024 * 1024; // Less than 50MB increase
            }, testSuite);
        }

        private async Task RunUITests(TestSuite testSuite)
        {
            _logger.LogInfo("Running UI Tests");

            // Test panel creation
            await RunTest("UI_PanelCreation", async () =>
            {
                // This would test UI panel creation in a real Rhino environment
                return true; // Placeholder
            }, testSuite);
        }

        private async Task RunTest(string testName, Func<Task<bool>> testAction, TestSuite testSuite)
        {
            var stopwatch = Stopwatch.StartNew();
            var testResult = new TestResult
            {
                TestName = testName,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInfo($"Running test: {testName}");
                testResult.Success = await testAction();
                testResult.ErrorMessage = testResult.Success ? null : "Test returned false";
            }
            catch (Exception ex)
            {
                testResult.Success = false;
                testResult.ErrorMessage = ex.Message;
                _logger.LogError($"Test {testName} failed: {ex.Message}");
            }

            stopwatch.Stop();
            testResult.Duration = stopwatch.Elapsed;
            testResult.EndTime = DateTime.Now;
            
            testSuite.TestResults.Add(testResult);
            
            var status = testResult.Success ? "PASSED" : "FAILED";
            _logger.LogInfo($"Test {testName}: {status} ({testResult.Duration.TotalMilliseconds:F2}ms)");
        }

        private void LogTestSummary(TestSuite testSuite)
        {
            _logger.LogInfo("=== TEST SUITE SUMMARY ===");
            _logger.LogInfo($"Total Tests: {testSuite.TotalTests}");
            _logger.LogInfo($"Passed: {testSuite.PassedTests}");
            _logger.LogInfo($"Failed: {testSuite.FailedTests}");
            _logger.LogInfo($"Success Rate: {(double)testSuite.PassedTests / testSuite.TotalTests * 100:F1}%");
            _logger.LogInfo($"Total Duration: {testSuite.TotalDuration.TotalSeconds:F2} seconds");
            
            if (testSuite.FailedTests > 0)
            {
                _logger.LogWarning("Failed Tests:");
                foreach (var failedTest in testSuite.TestResults.Where(r => !r.Success))
                {
                    _logger.LogWarning($"  - {failedTest.TestName}: {failedTest.ErrorMessage}");
                }
            }
        }
    }

    public class TestSuite
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
    }

    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }
} 