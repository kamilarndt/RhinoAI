using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
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
        private readonly AIManager _aiManager;

        public RhinoAICommand()
        {
            _logger = new SimpleLogger("RhinoAI", LogLevel.Information);
            _configManager = new ConfigurationManager(_logger);
            _aiManager = new AIManager(_configManager, _logger);
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
                var result = gs.GetLiteralString();
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
                        var response = await _aiManager.ProcessNaturalLanguageAsync(userInput);
                        RhinoApp.WriteLine($"AI Response: {response}");
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
} 