using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RhinoAI.Core
{
    /// <summary>
    /// Natural Language Processing component for command interpretation
    /// </summary>
    public class NLPProcessor
    {
        private readonly ConfigurationManager _configManager;
        private readonly SimpleLogger _logger;
        private readonly AIManager _aiManager;

        public NLPProcessor(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiManager = new AIManager(_configManager, _logger);
            
            // Initialize AI components
            if (!_aiManager.InitializeAll())
            {
                _logger.LogWarning("Some AI components failed to initialize");
            }
        }

        /// <summary>
        /// Process a natural language command and return structured result
        /// </summary>
        public async Task<CommandResult> ProcessCommandAsync(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    return new CommandResult
                    {
                        Success = false,
                        ErrorMessage = "Input cannot be empty",
                        Suggestions = new List<string> { "Try: 'create a box'", "Try: 'make a sphere'" }
                    };
                }

                _logger.LogInformation($"Processing command: {input}");

                // Use AIManager to process the natural language
                var response = await _aiManager.ProcessNaturalLanguageAsync(input);
                
                return new CommandResult
                {
                    Success = true,
                    Intent = ExtractIntent(input),
                    FeedbackMessage = response,
                    Parameters = ExtractParameters(input)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing command: {ex.Message}");
                
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Suggestions = new List<string> 
                    { 
                        "Check your command syntax",
                        "Try simpler commands first",
                        "Ensure AI services are configured"
                    }
                };
            }
        }

        /// <summary>
        /// Extract intent from natural language input
        /// </summary>
        private string ExtractIntent(string input)
        {
            var lowercaseInput = input.ToLowerInvariant();
            
            if (lowercaseInput.Contains("create") || lowercaseInput.Contains("make") || lowercaseInput.Contains("add"))
                return "CREATE";
            if (lowercaseInput.Contains("move") || lowercaseInput.Contains("translate"))
                return "MOVE";
            if (lowercaseInput.Contains("rotate") || lowercaseInput.Contains("turn"))
                return "ROTATE";
            if (lowercaseInput.Contains("scale") || lowercaseInput.Contains("resize"))
                return "SCALE";
            if (lowercaseInput.Contains("delete") || lowercaseInput.Contains("remove"))
                return "DELETE";
            if (lowercaseInput.Contains("analyze") || lowercaseInput.Contains("measure"))
                return "ANALYZE";
            
            return "GENERAL";
        }

        /// <summary>
        /// Extract parameters from natural language input
        /// </summary>
        private Dictionary<string, string> ExtractParameters(string input)
        {
            var parameters = new Dictionary<string, string>();
            
            // Simple parameter extraction (this could be enhanced with more sophisticated NLP)
            var lowercaseInput = input.ToLowerInvariant();
            
            // Extract numeric values
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (double.TryParse(words[i], out double value))
                {
                    if (i > 0)
                    {
                        var previousWord = words[i - 1].ToLowerInvariant();
                        if (previousWord.Contains("radius"))
                            parameters["radius"] = value.ToString();
                        else if (previousWord.Contains("size") || previousWord.Contains("width") || previousWord.Contains("height"))
                            parameters["size"] = value.ToString();
                        else if (previousWord.Contains("distance"))
                            parameters["distance"] = value.ToString();
                    }
                }
            }

            // Extract object types
            if (lowercaseInput.Contains("box") || lowercaseInput.Contains("cube"))
                parameters["objectType"] = "box";
            else if (lowercaseInput.Contains("sphere") || lowercaseInput.Contains("ball"))
                parameters["objectType"] = "sphere";
            else if (lowercaseInput.Contains("cylinder"))
                parameters["objectType"] = "cylinder";
            else if (lowercaseInput.Contains("cone"))
                parameters["objectType"] = "cone";

            return parameters;
        }
    }

    /// <summary>
    /// Result of command processing
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Intent { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public string FeedbackMessage { get; set; } = "";
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public List<string> Suggestions { get; set; } = new List<string>();
    }
} 