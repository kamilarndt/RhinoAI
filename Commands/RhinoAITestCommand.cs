using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using RhinoAI.Development;
using RhinoAI.Core;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Test command for RhinoAI plugin functionality validation
    /// </summary>
    public class RhinoAITestCommand : Command
    {
        private readonly SimpleLogger _logger;

        public RhinoAITestCommand()
        {
            _logger = new SimpleLogger("RhinoAITest", LogLevel.Information);
        }

        public override string EnglishName => "RhinoAITest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                _logger.LogInformation("Starting RhinoAI Test Command");

                // Get test options from user
                var testOptions = new GetOption();
                testOptions.SetCommandPrompt("Select test to run");
                testOptions.AddOption("All");
                testOptions.AddOption("Core");
                testOptions.AddOption("NLP");
                testOptions.AddOption("Integration");
                testOptions.AddOption("Performance");
                testOptions.AddOption("UI");

                var result = testOptions.Get();
                if (result != GetResult.Option)
                {
                    return Result.Cancel;
                }

                var selectedTest = testOptions.Option().EnglishName;
                
                // Run tests asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var testFramework = new TestingFramework();
                        TestSuite testSuite;

                        switch (selectedTest)
                        {
                            case "All":
                                testSuite = await testFramework.RunAllTestsAsync();
                                break;
                            default:
                                RhinoApp.WriteLine($"Running {selectedTest} tests...");
                                testSuite = await testFramework.RunAllTestsAsync(); // For now, run all
                                break;
                        }

                        // Display results
                        DisplayTestResults(testSuite);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Test execution failed: {ex.Message}");
                        RhinoApp.WriteLine($"Test execution failed: {ex.Message}");
                    }
                });

                RhinoApp.WriteLine("Tests are running in the background. Check the command line for results.");
                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RhinoAI Test Command failed: {ex.Message}");
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }

        private void DisplayTestResults(TestSuite testSuite)
        {
            RhinoApp.WriteLine("=== RHINOAI TEST RESULTS ===");
            RhinoApp.WriteLine($"Total Tests: {testSuite.TotalTests}");
            RhinoApp.WriteLine($"Passed: {testSuite.PassedTests}");
            RhinoApp.WriteLine($"Failed: {testSuite.FailedTests}");
            RhinoApp.WriteLine($"Success Rate: {testSuite.SuccessRate:F1}%");
            RhinoApp.WriteLine($"Duration: {testSuite.TotalDuration.TotalSeconds:F2} seconds");

            if (testSuite.FailedTests > 0)
            {
                RhinoApp.WriteLine("\nFailed Tests:");
                foreach (var failedTest in testSuite.TestResults)
                {
                    if (!failedTest.Success)
                    {
                        RhinoApp.WriteLine($"  - {failedTest.TestName}: {failedTest.ErrorMessage}");
                    }
                }
            }

            RhinoApp.WriteLine("=== END TEST RESULTS ===");
        }
    }
} 