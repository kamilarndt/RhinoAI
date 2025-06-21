using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using RhinoAI.AI;
using RhinoAI.Core;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Main RhinoAI command for natural language processing
    /// </summary>
    public class RhinoAICommand : Command
    {
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly RhinoAI.Core.NLPProcessor _nlpProcessor;

        public RhinoAICommand()
        {
            _logger = new SimpleLogger("RhinoAI", LogLevel.Information);
            _configManager = new ConfigurationManager(_logger);
            _nlpProcessor = new RhinoAI.Core.NLPProcessor(_configManager, _logger);
        }

        public override string EnglishName => "RhinoAI";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                _logger.LogInformation("Starting RhinoAI Command");

                // Get natural language input from user
                var gs = new Rhino.Input.Custom.GetString();
                gs.SetCommandPrompt("Enter your command in natural language");
                gs.AcceptNothing(false);
                var result = gs.Get();
                if (result != Rhino.Input.GetResult.String)
                {
                    return Result.Cancel;
                }

                var userInput = gs.StringResult();
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    RhinoApp.WriteLine("Please provide a valid command.");
                    return Result.Cancel;
                }

                RhinoApp.WriteLine($"Processing: '{userInput}'");
                RhinoApp.WriteLine("AI is thinking... Please wait.");

                // Process the command asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await _nlpProcessor.ProcessCommandAsync(userInput);
                        
                        if (result.Success)
                        {
                            RhinoApp.WriteLine($"✓ Command executed successfully!");
                            RhinoApp.WriteLine($"Intent: {result.Intent}");
                            
                            if (result.Parameters.Count > 0)
                            {
                                RhinoApp.WriteLine("Parameters:");
                                foreach (var param in result.Parameters)
                                {
                                    RhinoApp.WriteLine($"  - {param.Key}: {param.Value}");
                                }
                            }

                            if (!string.IsNullOrEmpty(result.FeedbackMessage))
                            {
                                RhinoApp.WriteLine($"Feedback: {result.FeedbackMessage}");
                            }
                        }
                        else
                        {
                            RhinoApp.WriteLine($"✗ Command failed: {result.ErrorMessage}");
                            
                            if (result.Suggestions.Count > 0)
                            {
                                RhinoApp.WriteLine("Suggestions:");
                                foreach (var suggestion in result.Suggestions)
                                {
                                    RhinoApp.WriteLine($"  - {suggestion}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Command processing failed: {ex.Message}");
                        RhinoApp.WriteLine($"Error processing command: {ex.Message}");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RhinoAI Command failed: {ex.Message}");
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }

    /// <summary>
    /// Interactive RhinoAI command with continuous conversation
    /// </summary>
    public class RhinoAIInteractiveCommand : Command
    {
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly RhinoAI.Core.NLPProcessor _nlpProcessor;

        public RhinoAIInteractiveCommand()
        {
            _logger = new SimpleLogger("RhinoAIInteractive", LogLevel.Information);
            _configManager = new ConfigurationManager(_logger);
            _nlpProcessor = new RhinoAI.Core.NLPProcessor(_configManager, _logger);
        }

        public override string EnglishName => "RhinoAIInteractive";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                _logger.LogInformation("Starting RhinoAI Interactive Command");
                
                RhinoApp.WriteLine("=== RhinoAI Interactive Mode ===");
                RhinoApp.WriteLine("Type 'exit' or 'quit' to end the session.");
                RhinoApp.WriteLine("Type 'help' for available commands.");
                RhinoApp.WriteLine("");

                while (true)
                {
                    // Get input from user
                    var gs = new Rhino.Input.Custom.GetString();
                    gs.SetCommandPrompt("RhinoAI> ");
                    gs.AcceptNothing(false);
                    var result = gs.Get();
                    if (result != Rhino.Input.GetResult.String)
                    {
                        break;
                    }

                    var userInput = gs.StringResult()?.Trim();
                    if (string.IsNullOrEmpty(userInput))
                    {
                        continue;
                    }

                    // Check for exit commands
                    if (userInput.ToLower() == "exit" || userInput.ToLower() == "quit")
                    {
                        RhinoApp.WriteLine("Goodbye!");
                        break;
                    }

                    // Check for help command
                    if (userInput.ToLower() == "help")
                    {
                        ShowHelp();
                        continue;
                    }

                    // Process the command
                    await ProcessInteractiveCommand(userInput);
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RhinoAI Interactive Command failed: {ex.Message}");
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }

        private async Task ProcessInteractiveCommand(string userInput)
        {
            try
            {
                RhinoApp.WriteLine("Processing...");
                
                var result = await _nlpProcessor.ProcessCommandAsync(userInput);
                
                if (result.Success)
                {
                    RhinoApp.WriteLine($"✓ {result.FeedbackMessage ?? "Command executed successfully!"}");
                }
                else
                {
                    RhinoApp.WriteLine($"✗ {result.ErrorMessage}");
                    
                    if (result.Suggestions.Count > 0)
                    {
                        RhinoApp.WriteLine("Try:");
                        foreach (var suggestion in result.Suggestions)
                        {
                            RhinoApp.WriteLine($"  - {suggestion}");
                        }
                    }
                }
                
                RhinoApp.WriteLine("");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Interactive command processing failed: {ex.Message}");
                RhinoApp.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ShowHelp()
        {
            RhinoApp.WriteLine("=== RhinoAI Help ===");
            RhinoApp.WriteLine("Available commands:");
            RhinoApp.WriteLine("  - Create geometric objects: 'create a box', 'make a sphere with radius 5'");
            RhinoApp.WriteLine("  - Modify objects: 'move the selected objects up by 10', 'rotate by 45 degrees'");
            RhinoApp.WriteLine("  - Analysis: 'calculate the volume', 'measure the distance'");
            RhinoApp.WriteLine("  - View commands: 'zoom to fit', 'change to perspective view'");
            RhinoApp.WriteLine("  - Layer management: 'create a new layer', 'hide the current layer'");
            RhinoApp.WriteLine("  - help - Show this help message");
            RhinoApp.WriteLine("  - exit/quit - End interactive session");
            RhinoApp.WriteLine("");
            RhinoApp.WriteLine("Examples:");
            RhinoApp.WriteLine("  'create a box at origin with size 10'");
            RhinoApp.WriteLine("  'make a circle with radius 5 at point 0,0,0'");
            RhinoApp.WriteLine("  'extrude the selected curves by 10 units'");
            RhinoApp.WriteLine("");
        }
    }
} 