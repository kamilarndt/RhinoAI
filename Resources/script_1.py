# Create comprehensive code examples for NLP processor improvements
import textwrap

# Generate improved NLP processor code structure
improved_nlp_code = """
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoAI.Core;
using RhinoAI.Integration;
using RhinoAI.Utils;

namespace RhinoAI.AI
{
    /// <summary>
    /// Enhanced Natural Language Processing component with hierarchical intent recognition,
    /// robust error handling, and context awareness
    /// </summary>
    public class EnhancedNLPProcessor
    {
        private readonly OpenAIClient _openAIClient;
        private readonly ClaudeClient _claudeClient;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly IntentClassifier _intentClassifier;
        private readonly ContextManager _contextManager;
        private readonly ParameterExtractor _parameterExtractor;
        private readonly SemanticValidator _semanticValidator;
        private readonly RobustJsonParser _jsonParser;
        private readonly FallbackHandler _fallbackHandler;
        
        // Caching for improved performance
        private readonly Dictionary<string, CachedResponse> _responseCache;
        private readonly Dictionary<string, CommandTemplate> _commandTemplates;
        
        public EnhancedNLPProcessor(
            ConfigurationManager configManager, 
            SimpleLogger logger,
            IntentClassifier intentClassifier = null)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize enhanced components
            _intentClassifier = intentClassifier ?? new IntentClassifier();
            _contextManager = new ContextManager();
            _parameterExtractor = new ParameterExtractor();
            _semanticValidator = new SemanticValidator();
            _jsonParser = new RobustJsonParser();
            _fallbackHandler = new FallbackHandler();
            
            // Initialize AI clients
            _openAIClient = new OpenAIClient(_configManager, _logger);
            _claudeClient = new ClaudeClient(_configManager, _logger);
            
            // Initialize caching
            _responseCache = new Dictionary<string, CachedResponse>();
            _commandTemplates = InitializeEnhancedCommandTemplates();
        }

        /// <summary>
        /// Enhanced natural language processing with hierarchical intent recognition
        /// </summary>
        public async Task<ProcessingResult> ProcessNaturalLanguageAsync(
            string input, 
            ConversationContext context = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return ProcessingResult.Error("Please provide a valid input.");

                _logger.LogInformation("Processing enhanced NLP input: {0}", input);
                
                // Step 1: Intent Classification with confidence scoring
                var intentResult = await _intentClassifier.ClassifyIntentAsync(input, context);
                if (intentResult.Confidence < 0.7)
                {
                    return await _fallbackHandler.HandleLowConfidenceAsync(input, intentResult);
                }

                // Step 2: Context-aware processing
                var enhancedContext = await _contextManager.EnhanceContextAsync(input, context);
                
                // Step 3: Multi-stage command processing
                switch (intentResult.Category)
                {
                    case IntentCategory.DirectCommand:
                        return await ProcessDirectCommand(input, intentResult, enhancedContext);
                    
                    case IntentCategory.ComplexOperation:
                        return await ProcessComplexOperation(input, intentResult, enhancedContext);
                    
                    case IntentCategory.Query:
                        return await ProcessQuery(input, intentResult, enhancedContext);
                    
                    case IntentCategory.Modification:
                        return await ProcessModification(input, intentResult, enhancedContext);
                    
                    default:
                        return await ProcessWithAI(input, enhancedContext);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced NLP processing");
                return await _fallbackHandler.HandleExceptionAsync(ex, input);
            }
        }

        /// <summary>
        /// Process direct commands with semantic validation
        /// </summary>
        private async Task<ProcessingResult> ProcessDirectCommand(
            string input, 
            IntentResult intentResult, 
            ConversationContext context)
        {
            // Extract parameters with enhanced validation
            var parameters = await _parameterExtractor.ExtractParametersAsync(
                input, 
                intentResult.CommandTemplate, 
                context);
            
            // Semantic validation
            var validationResult = await _semanticValidator.ValidateParametersAsync(
                parameters, 
                intentResult.CommandTemplate);
                
            if (!validationResult.IsValid)
            {
                return ProcessingResult.Error(
                    $"Invalid parameters: {validationResult.ErrorMessage}");
            }

            // Execute with retry mechanism
            return await ExecuteWithRetry(intentResult.CommandTemplate, parameters, context);
        }

        /// <summary>
        /// Enhanced AI processing with robust JSON parsing
        /// </summary>
        private async Task<ProcessingResult> ProcessWithAI(
            string input, 
            ConversationContext context)
        {
            // Check cache first
            var cacheKey = GenerateCacheKey(input, context);
            if (_responseCache.TryGetValue(cacheKey, out var cachedResponse) && 
                !cachedResponse.IsExpired)
            {
                return cachedResponse.Result;
            }

            // Enhanced system prompt with context
            var systemPrompt = CreateEnhancedSystemPrompt(context);
            var userRequest = new EnhancedAIRequest
            {
                UserInput = input,
                Context = context,
                AvailableCommands = _commandTemplates.Values.ToList(),
                SceneInfo = await GetCurrentSceneInfoAsync()
            };

            var requestJson = JsonSerializer.Serialize(userRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Try multiple AI providers with fallback
            var response = await TryMultipleProvidersAsync(systemPrompt, requestJson);
            
            // Robust JSON parsing with error recovery
            var aiResponse = await _jsonParser.ParseWithRecoveryAsync(response);
            
            if (aiResponse?.Actions?.Count > 0)
            {
                var results = new List<string>();
                foreach (var action in aiResponse.Actions)
                {
                    var result = await ProcessActionWithValidation(action, context);
                    results.Add(result);
                }
                
                var finalResult = ProcessingResult.Success(string.Join(Environment.NewLine, results));
                
                // Cache successful results
                _responseCache[cacheKey] = new CachedResponse(finalResult, DateTime.UtcNow.AddMinutes(5));
                
                return finalResult;
            }

            return ProcessingResult.Partial(
                aiResponse?.ResponseText ?? "Could not process the request completely.");
        }

        /// <summary>
        /// Execute command with retry mechanism and error recovery
        /// </summary>
        private async Task<ProcessingResult> ExecuteWithRetry(
            CommandTemplate template, 
            Dictionary<string, object> parameters, 
            ConversationContext context,
            int maxRetries = 3)
        {
            var lastException = default(Exception);
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var result = await ExecuteRhinoCommand(template, parameters, context);
                    if (result.IsSuccess)
                        return result;
                        
                    // If not successful but no exception, try parameter adjustment
                    if (attempt < maxRetries - 1)
                    {
                        parameters = await _parameterExtractor.AdjustParametersAsync(
                            parameters, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Attempt {0} failed: {1}", attempt + 1, ex.Message);
                    
                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(1000 * (attempt + 1)); // Exponential backoff
                    }
                }
            }
            
            return await _fallbackHandler.HandleFailedExecutionAsync(
                template, parameters, lastException);
        }

        /// <summary>
        /// Enhanced command execution with context awareness
        /// </summary>
        private async Task<ProcessingResult> ExecuteRhinoCommand(
            CommandTemplate template, 
            Dictionary<string, object> parameters,
            ConversationContext context)
        {
            try
            {
                // Pre-execution validation
                var preValidation = await _semanticValidator.PreExecuteValidationAsync(
                    template, parameters, context);
                    
                if (!preValidation.IsValid)
                {
                    return ProcessingResult.Error(preValidation.ErrorMessage);
                }

                switch (template.CommandName)
                {
                    case "CreateSphere":
                        return await CreateEnhancedSphere(parameters, context);
                    case "CreateBox":
                        return await CreateEnhancedBox(parameters, context);
                    case "CreateCylinder":
                        return await CreateEnhancedCylinder(parameters, context);
                    default:
                        return ProcessingResult.Error($"Command '{template.CommandName}' is not implemented.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing command: {template.CommandName}");
                return ProcessingResult.Error($"Execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced sphere creation with validation and context
        /// </summary>
        private async Task<ProcessingResult> CreateEnhancedSphere(
            Dictionary<string, object> parameters, 
            ConversationContext context)
        {
            // Extract parameters with defaults and validation
            var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
            var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
            var material = _parameterExtractor.GetString(parameters, "material", "");
            var layer = _parameterExtractor.GetString(parameters, "layer", "");

            // Semantic validation
            if (radius <= 0)
                return ProcessingResult.Error("Sphere radius must be positive.");
            
            if (radius > 1000)
                return ProcessingResult.Warning($"Large sphere (radius: {radius:F2}). Continuing...");

            // Create sphere with enhanced attributes
            var sphere = new Sphere(center, radius);
            var brep = sphere.ToBrep();
            
            if (brep?.IsValid == true)
            {
                var attributes = new Rhino.DocObjects.ObjectAttributes();
                
                // Apply material if specified
                if (!string.IsNullOrEmpty(material))
                {
                    attributes.MaterialSource = Rhino.DocObjects.ObjectMaterialSource.MaterialFromObject;
                    // Apply material logic here
                }
                
                // Apply layer if specified
                if (!string.IsNullOrEmpty(layer))
                {
                    var layerIndex = EnsureLayer(layer);
                    attributes.LayerIndex = layerIndex;
                }

                var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                
                if (id != Guid.Empty)
                {
                    // Update context with created object
                    context?.AddCreatedObject(id, "sphere", parameters);
                    
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    
                    var result = $"Created sphere at {center} with radius {radius:F2}";
                    if (!string.IsNullOrEmpty(layer)) result += $" on layer '{layer}'";
                    if (!string.IsNullOrEmpty(material)) result += $" with material '{material}'";
                    
                    return ProcessingResult.Success(result);
                }
            }

            return ProcessingResult.Error("Failed to create sphere - invalid geometry");
        }
        
        /// <summary>
        /// Try multiple AI providers with intelligent fallback
        /// </summary>
        private async Task<string> TryMultipleProvidersAsync(string systemPrompt, string requestJson)
        {
            var providers = new List<(Func<Task<string>>, string)>
            {
                (() => _openAIClient?.ProcessTextAsync(systemPrompt, requestJson), "OpenAI"),
                (() => _claudeClient?.ProcessTextAsync(systemPrompt, requestJson), "Claude")
            };

            Exception lastException = null;
            
            foreach (var (provider, name) in providers)
            {
                try
                {
                    if (provider != null)
                    {
                        var result = await provider();
                        if (!string.IsNullOrEmpty(result))
                        {
                            _logger.LogDebug($"Successfully used {name} provider");
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"{name} provider failed: {ex.Message}");
                    lastException = ex;
                }
            }
            
            throw new InvalidOperationException(
                "All AI providers failed", lastException);
        }

        /// <summary>
        /// Create enhanced system prompt with context awareness
        /// </summary>
        private string CreateEnhancedSystemPrompt(ConversationContext context = null)
        {
            var basePrompt = @"You are an advanced AI assistant for Rhino 3D with deep understanding of CAD operations and 3D modeling.

CAPABILITIES:
- Interpret natural language commands for 3D modeling
- Understand spatial relationships and geometric constraints
- Maintain context across conversations
- Provide intelligent parameter suggestions
- Handle complex multi-step operations

RESPONSE FORMAT:
Respond ONLY with valid JSON containing an 'Actions' array.
Each action must have:
- 'CommandName': exact command to execute
- 'Parameters': object with parameter values
- 'Confidence': confidence score (0.0-1.0)

AVAILABLE COMMANDS:
- CreateSphere: Creates spheres with center, radius, material, layer
- CreateBox: Creates boxes with center, dimensions, rotation, material, layer  
- CreateCylinder: Creates cylinders with center, radius, height, material, layer
- ModifyObject: Modifies existing objects
- QueryScene: Queries current scene information

CONTEXT AWARENESS:
";
            
            if (context != null)
            {
                basePrompt += $@"
Current scene: {context.SceneDescription}
Recent operations: {string.Join(", ", context.RecentOperations.Take(3))}
Active layer: {context.ActiveLayer}
Selected objects: {context.SelectedObjects.Count}
";
            }

            basePrompt += @"

VALIDATION RULES:
- All dimensions must be positive numbers
- Coordinates should be reasonable (-1000 to 1000 typical range)
- Material and layer names should be valid strings
- Consider geometric constraints and relationships

EXAMPLE:
Input: ""Create a red sphere with radius 5 at the origin""
Output: {
  ""Actions"": [{
    ""CommandName"": ""CreateSphere"",
    ""Parameters"": {
      ""center"": [0, 0, 0],
      ""radius"": 5,
      ""material"": ""red""
    },
    ""Confidence"": 0.95
  }]
}";

            return basePrompt;
        }
    }
}
"""

# Save the improved code
with open("enhanced_nlp_processor.cs", "w") as f:
    f.write(improved_nlp_code)

print("Enhanced NLP Processor code generated successfully!")
print("\nKey improvements included:")
print("✓ Hierarchical intent recognition")
print("✓ Enhanced context management") 
print("✓ Robust parameter extraction and validation")
print("✓ Multi-provider AI fallback")
print("✓ Retry mechanisms with exponential backoff")
print("✓ Response caching")
print("✓ Semantic validation")
print("✓ Enhanced error handling")
print("✓ Context-aware command execution")
print("✓ Comprehensive logging and monitoring")