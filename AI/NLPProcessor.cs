using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoAI.Core;
using RhinoAI.Integration;
using RhinoAI.Utils;

namespace RhinoAI.AI
{
    /// <summary>
    /// Enhanced Natural Language Processing component for AI interactions
    /// Integrates advanced context awareness, intent classification, and semantic validation
    /// </summary>
    public class NLPProcessor
    {
        private readonly OpenAIClient _openAIClient;
        private readonly ClaudeClient _claudeClient;
        private readonly OllamaClient _ollamaClient;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly Dictionary<string, CommandTemplate> _commandTemplates;
        
        // Enhanced NLP components
        private readonly EnhancedNLPProcessor _enhancedProcessor;
        private readonly bool _useEnhancedProcessing;

        public NLPProcessor(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize AI clients based on configuration
            _openAIClient = new OpenAIClient(_configManager, _logger);
            _claudeClient = new ClaudeClient(_configManager, _logger);
            _ollamaClient = new OllamaClient(_configManager, _logger);
            
            _commandTemplates = InitializeCommandTemplates();
            
            // Initialize enhanced processor
            _enhancedProcessor = new EnhancedNLPProcessor();
            _useEnhancedProcessing = true; // Enable enhanced processing by default
        }

        /// <summary>
        /// Process natural language input and convert to Rhino commands
        /// Uses enhanced processing with fallback to original implementation
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>AI response or command execution result</returns>
        public async Task<string> ProcessNaturalLanguageAsync(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return "Please provide a valid input.";

                _logger.LogInformation("Processing natural language input: {0}", input);

                // Try enhanced processing first
                if (_useEnhancedProcessing)
                {
                    try
                    {
                        var enhancedResult = await _enhancedProcessor.ProcessAsync(input);
                        if (enhancedResult.IsSuccess)
                        {
                            _logger.LogInformation("Enhanced processing successful: {0}", enhancedResult.Message);
                            return enhancedResult.Message;
                        }
                        else if (enhancedResult.Type == ProcessingResultType.Warning)
                        {
                            _logger.LogWarning("Enhanced processing warning: {0}", enhancedResult.Message);
                            return enhancedResult.Message;
                        }
                        else
                        {
                            _logger.LogWarning($"Enhanced processing failed, falling back to original: {enhancedResult.ErrorMessage}");
                            // Fall through to original processing
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Enhanced processing error, falling back to original: {ex.Message}");
                        // Fall through to original processing
                    }
                }

                // Fallback to original processing
                return await ProcessWithOriginalMethod(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language input");
                return $"Error processing request: {ex.Message}";
            }
        }

        /// <summary>
        /// Original processing method maintained for fallback compatibility
        /// </summary>
        private async Task<string> ProcessWithOriginalMethod(string input)
        {
            // First try to match with command templates
            var commandResult = TryMatchCommand(input);
            if (commandResult != null)
            {
                _logger.LogInformation("Matched command template, executing: {0}", commandResult.CommandName);
                return await ExecuteRhinoCommand(commandResult, input);
            }

            // If no direct command match, use AI to interpret
            return await ProcessWithAI(input);
        }

        /// <summary>
        /// Process input using AI models
        /// </summary>
        private async Task<string> ProcessWithAI(string input)
        {
            var systemPrompt = CreateSystemPrompt();
            var userRequest = new AICommandRequest
            {
                UserInput = input,
                AvailableCommands = new List<CommandTemplate>(_commandTemplates.Values)
            };

            var requestJson = JsonSerializer.Serialize(userRequest);
            var aiResponseJson = "";

            // Try AI services in order: OpenAI, Claude, then Ollama
            if (_openAIClient?.IsConfigured == true)
            {
                aiResponseJson = await _openAIClient.ProcessTextAsync(systemPrompt, requestJson);
            }
            else if (_claudeClient?.IsConfigured == true)
            {
                aiResponseJson = await _claudeClient.ProcessTextAsync(systemPrompt, requestJson);
            }
            else if (_ollamaClient?.IsConfigured == true)
            {
                aiResponseJson = await _ollamaClient.ProcessTextAsync(systemPrompt, requestJson);
            }
            else
            {
                return "No AI service is configured. Please configure OpenAI, Claude API keys, or Ollama.";
            }

            // Parse AI response for commands
            var aiResponse = ParseAIResponse(aiResponseJson);
            if (aiResponse?.Actions?.Count > 0)
            {
                var results = new List<string>();
                foreach (var action in aiResponse.Actions)
                {
                    if (!string.IsNullOrEmpty(action.CommandName))
                    {
                        _logger.LogInformation("AI suggested command: {0}", action.CommandName);
                        var commandTemplate = _commandTemplates.GetValueOrDefault(action.CommandName);
                        if (commandTemplate != null)
                        {
                            var parameters = Utils.ParameterExtractor.Extract(input, commandTemplate.Parameters.ToList());
                            results.Add(await ExecuteRhinoCommand(commandTemplate, parameters));
                        }
                    }
                }
                return string.Join(System.Environment.NewLine, results);
            }

            return !string.IsNullOrEmpty(aiResponse?.ResponseText) ? aiResponse.ResponseText : "Sorry, I could not understand the request.";
        }

        /// <summary>
        /// Create system prompt for AI context
        /// </summary>
        private string CreateSystemPrompt()
        {
            return @"You are an AI assistant for Rhino 3D. Your task is to interpret the user's request and identify a sequence of commands to execute.
Respond ONLY with a valid JSON object.
- The JSON should contain an ""Actions"" array.
- Each action should have a ""CommandName"".
- Identify all relevant commands. If the user wants a box and a sphere, return actions for both.
- Do NOT extract parameters.

Example Request: ""Create a red box at 10,10,0 with size 5 and a blue sphere inside it with radius 2""

Example Response:
{
  ""Actions"": [
    { ""CommandName"": ""CreateBox"" },
    { ""CommandName"": ""CreateSphere"" }
  ]
}";
        }

        /// <summary>
        /// Try to match input with predefined command templates
        /// </summary>
        private CommandTemplate TryMatchCommand(string input)
        {
            var lowerInput = input.ToLowerInvariant();

            foreach (var template in _commandTemplates.Values)
            {
                foreach (var keyword in template.Keywords)
                {
                    if (lowerInput.Contains(keyword.ToLowerInvariant()))
                    {
                        return template;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parse AI response for command information
        /// </summary>
        private AICommandResponse ParseAIResponse(string aiResponseJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(aiResponseJson)) return null;

                // The AI might return JSON within a code block, so we need to extract it.
                var json = aiResponseJson.Trim();
                if (json.StartsWith("```json"))
                {
                    json = json.Substring(7);
                }
                if (json.StartsWith("```"))
                {
                    json = json.Substring(3);
                }
                if (json.EndsWith("```"))
                {
                    json = json.Substring(0, json.Length - 3);
                }
                
                return JsonSerializer.Deserialize<AICommandResponse>(json.Trim(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize AI response JSON.");
                return new AICommandResponse { ResponseText = aiResponseJson }; // Return the raw text if parsing fails
            }
        }

        /// <summary>
        /// Execute a Rhino command based on the template and input
        /// </summary>
        private async Task<string> ExecuteRhinoCommand(CommandTemplate template, Dictionary<string, object> parameters)
        {
            try
            {
                // Ensure parameters are not null
                parameters = parameters ?? new Dictionary<string, object>();

                switch (template.CommandName)
                {
                    case "CreateSphere":
                        return CreateSphere(parameters);
                    
                    case "CreateBox":
                        return CreateBox(parameters);
                    
                    case "CreateCylinder":
                        return CreateCylinder(parameters);
                    
                    case "CreateSphereArray":
                        return CreateSphereArray(parameters);
                    
                    case "CreateBoxArray":
                        return CreateBoxArray(parameters);
                    
                    case "SelectAll":
                        return SelectAllObjects();
                    
                    case "SelectByName":
                        return SelectObjectsByName(parameters);
                    
                    case "DeselectAll":
                        return DeselectAllObjects();
                    
                    case "Move":
                        return MoveObjects(parameters);
                    
                    case "Scale":
                        return ScaleObjects(parameters);
                    
                    case "BooleanUnion":
                        return BooleanUnion();
                    
                    case "BooleanDifference":
                        return BooleanDifference();
                    
                    case "BooleanIntersection":
                        return BooleanIntersection();
                    
                    case "Explode":
                        return ExplodeObjects();
                    
                    case "Join":
                        return JoinObjects();
                    
                    default:
                        return $"Command '{template.CommandName}' is not yet implemented.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing Rhino command: {template.CommandName}");
                return $"Error executing command: {ex.Message}";
            }
        }

        /// <summary>
        /// Execute a Rhino command based on the template and input
        /// </summary>
        private async Task<string> ExecuteRhinoCommand(CommandTemplate template, string originalInput)
        {
            var parameters = Utils.ParameterExtractor.Extract(originalInput, template.Parameters.ToList());
            return await ExecuteRhinoCommand(template, parameters);
        }

        /// <summary>
        /// Create a sphere based on parameters
        /// </summary>
        private string CreateSphere(Dictionary<string, object> parameters)
        {
            var center = GetPointParameter(parameters, "center", Point3d.Origin);
            var radius = GetDoubleParameter(parameters, "radius", 1.0);
            var color = GetColorParameter(parameters, "color");
            var name = GetStringParameter(parameters, "name", "");

            var sphere = new Sphere(center, radius);
            var brep = sphere.ToBrep();

            if (brep?.IsValid == true)
            {
                var attributes = new ObjectAttributes();
                if (color.HasValue)
                {
                    attributes.ObjectColor = color.Value;
                    attributes.ColorSource = ObjectColorSource.ColorFromObject;
                }

                var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                if (id != Guid.Empty)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        var rhinoObject = RhinoDoc.ActiveDoc.Objects.FindId(id);
                        if (rhinoObject != null)
                        {
                            rhinoObject.Attributes.Name = name;
                            rhinoObject.CommitChanges();
                        }
                    }

                    // Auto-select the newly created object
                    RhinoDoc.ActiveDoc.Objects.Select(id);

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    var colorText = color.HasValue ? $" in {GetColorName(color.Value)}" : "";
                    var nameText = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                    return $"Created sphere{colorText}{nameText} at {center} with radius {radius:F2}";
                }
            }

            return "Failed to create sphere";
        }

        /// <summary>
        /// Create a box based on parameters
        /// </summary>
        private string CreateBox(Dictionary<string, object> parameters)
        {
            var center = GetPointParameter(parameters, "center", Point3d.Origin);
            var color = GetColorParameter(parameters, "color");
            var name = GetStringParameter(parameters, "name", "");
            
            // Try to get size as Vector3d first, then fallback to individual dimensions
            var size = GetVectorParameter(parameters, "size", new Vector3d(1, 1, 1));
            var width = size.X;
            var length = size.Y;
            var height = size.Z;
            
            // If size wasn't provided, try individual dimensions
            if (size == new Vector3d(1, 1, 1))
            {
                width = GetDoubleParameter(parameters, "width", 1.0);
                length = GetDoubleParameter(parameters, "length", 1.0);
                height = GetDoubleParameter(parameters, "height", 1.0);
            }

            var interval = new Interval(-width / 2, width / 2);
            var intervalY = new Interval(-length / 2, length / 2);
            var intervalZ = new Interval(-height / 2, height / 2);

            var box = new Box(Plane.WorldXY, interval, intervalY, intervalZ);
            var brep = box.ToBrep();

            if (brep?.IsValid == true)
            {
                // Move box to center position
                var translation = Transform.Translation(center - Point3d.Origin);
                brep.Transform(translation);

                var attributes = new ObjectAttributes();
                if (color.HasValue)
                {
                    attributes.ObjectColor = color.Value;
                    attributes.ColorSource = ObjectColorSource.ColorFromObject;
                }

                var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                if (id != Guid.Empty)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        var rhinoObject = RhinoDoc.ActiveDoc.Objects.FindId(id);
                        if (rhinoObject != null)
                        {
                            rhinoObject.Attributes.Name = name;
                            rhinoObject.CommitChanges();
                        }
                    }

                    // Auto-select the newly created object
                    RhinoDoc.ActiveDoc.Objects.Select(id);

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    var colorText = color.HasValue ? $" in {GetColorName(color.Value)}" : "";
                    var nameText = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                    return $"Created box{colorText}{nameText} at {center} with size {width:F1}×{length:F1}×{height:F1}";
                }
            }

            return "Failed to create box";
        }

        /// <summary>
        /// Create a cylinder based on parameters
        /// </summary>
        private string CreateCylinder(Dictionary<string, object> parameters)
        {
            var center = GetPointParameter(parameters, "center", Point3d.Origin);
            var radius = GetDoubleParameter(parameters, "radius", 1.0);
            var height = GetDoubleParameter(parameters, "height", 2.0);
            var color = GetColorParameter(parameters, "color");
            var name = GetStringParameter(parameters, "name", "");

            var basePlane = new Plane(center, Vector3d.ZAxis);
            var cylinder = new Cylinder(new Circle(basePlane, radius), height);
            var brep = cylinder.ToBrep(true, true);

            if (brep?.IsValid == true)
            {
                var attributes = new ObjectAttributes();
                if (color.HasValue)
                {
                    attributes.ObjectColor = color.Value;
                    attributes.ColorSource = ObjectColorSource.ColorFromObject;
                }

                var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                if (id != Guid.Empty)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        var rhinoObject = RhinoDoc.ActiveDoc.Objects.FindId(id);
                        if (rhinoObject != null)
                        {
                            rhinoObject.Attributes.Name = name;
                            rhinoObject.CommitChanges();
                        }
                    }

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    var colorText = color.HasValue ? $" in {GetColorName(color.Value)}" : "";
                    var nameText = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                    return $"Created cylinder{colorText}{nameText} at {center} with radius {radius:F2} and height {height:F2}";
                }
            }

            return "Failed to create cylinder";
        }

        /// <summary>
        /// Create an array of spheres based on parameters
        /// </summary>
        private string CreateSphereArray(Dictionary<string, object> parameters)
        {
            var center = GetPointParameter(parameters, "center", Point3d.Origin);
            var radius = GetDoubleParameter(parameters, "radius", 1.0);
            var rows = GetIntParameter(parameters, "rows", 3);
            var columns = GetIntParameter(parameters, "columns", 3);
            var spacing = GetDoubleParameter(parameters, "spacing", radius * 2.5);
            var color = GetColorParameter(parameters, "color");
            var name = GetStringParameter(parameters, "name", "");

            var createdCount = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var sphereCenter = new Point3d(
                        center.X + j * spacing,
                        center.Y + i * spacing,
                        center.Z
                    );

                    var sphere = new Sphere(sphereCenter, radius);
                    var brep = sphere.ToBrep();

                    if (brep?.IsValid == true)
                    {
                        var attributes = new ObjectAttributes();
                        if (color.HasValue)
                        {
                            attributes.ObjectColor = color.Value;
                            attributes.ColorSource = ObjectColorSource.ColorFromObject;
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            attributes.Name = $"{name}_sphere_{i}_{j}";
                        }

                        var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                        if (id != Guid.Empty)
                        {
                            createdCount++;
                        }
                    }
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Created {createdCount} spheres in a {rows}x{columns} array with radius {radius:F2} and spacing {spacing:F2}";
        }

        /// <summary>
        /// Create an array of boxes based on parameters
        /// </summary>
        private string CreateBoxArray(Dictionary<string, object> parameters)
        {
            var center = GetPointParameter(parameters, "center", Point3d.Origin);
            var size = GetDoubleParameter(parameters, "size", 2.0);
            var rows = GetIntParameter(parameters, "rows", 3);
            var columns = GetIntParameter(parameters, "columns", 3);
            var spacing = GetDoubleParameter(parameters, "spacing", size * 1.5);
            var color = GetColorParameter(parameters, "color");
            var name = GetStringParameter(parameters, "name", "");

            var createdCount = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    var boxCenter = new Point3d(
                        center.X + j * spacing,
                        center.Y + i * spacing,
                        center.Z
                    );

                    var interval = new Interval(-size / 2, size / 2);
                    var box = new Box(
                        new Plane(boxCenter, Vector3d.ZAxis),
                        interval, interval, interval
                    );

                    var brep = box.ToBrep();

                    if (brep?.IsValid == true)
                    {
                        var attributes = new ObjectAttributes();
                        if (color.HasValue)
                        {
                            attributes.ObjectColor = color.Value;
                            attributes.ColorSource = ObjectColorSource.ColorFromObject;
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            attributes.Name = $"{name}_box_{i}_{j}";
                        }

                        var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                        if (id != Guid.Empty)
                        {
                            createdCount++;
                        }
                    }
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Created {createdCount} boxes in a {rows}x{columns} array with size {size:F2} and spacing {spacing:F2}";
        }

        /// <summary>
        /// Get point parameter with default
        /// </summary>
        private Point3d GetPointParameter(Dictionary<string, object> parameters, string key, Point3d defaultValue)
        {
            return parameters.TryGetValue(key, out var value) && value is Point3d point ? point : defaultValue;
        }

        /// <summary>
        /// Get double parameter with default
        /// </summary>
        private double GetDoubleParameter(Dictionary<string, object> parameters, string key, double defaultValue)
        {
            return parameters.TryGetValue(key, out var value) && value is double d ? d : defaultValue;
        }

        /// <summary>
        /// Get int parameter with default
        /// </summary>
        private int GetIntParameter(Dictionary<string, object> parameters, string key, int defaultValue)
        {
            return parameters.TryGetValue(key, out var value) && value is int i ? i : defaultValue;
        }

        /// <summary>
        /// Get vector parameter with default
        /// </summary>
        private Vector3d GetVectorParameter(Dictionary<string, object> parameters, string key, Vector3d defaultValue)
        {
            return parameters.TryGetValue(key, out var value) && value is Vector3d vector ? vector : defaultValue;
        }

        /// <summary>
        /// Get color parameter with default
        /// </summary>
        private Color? GetColorParameter(Dictionary<string, object> parameters, string key)
        {
            return parameters.TryGetValue(key, out var value) && value is Color color ? color : (Color?)null;
        }

        /// <summary>
        /// Get string parameter with default
        /// </summary>
        private string GetStringParameter(Dictionary<string, object> parameters, string key, string defaultValue)
        {
            return parameters.TryGetValue(key, out var value) && value is string s ? s : defaultValue;
        }

        /// <summary>
        /// Get color name for display
        /// </summary>
        private string GetColorName(Color color)
        {
            if (color == Color.Red) return "red";
            if (color == Color.Green) return "green";
            if (color == Color.Blue) return "blue";
            if (color == Color.Yellow) return "yellow";
            if (color == Color.Orange) return "orange";
            if (color == Color.Purple) return "purple";
            if (color == Color.Pink) return "pink";
            if (color == Color.Cyan) return "cyan";
            if (color == Color.Magenta) return "magenta";
            if (color == Color.White) return "white";
            if (color == Color.Black) return "black";
            if (color == Color.Gray) return "gray";
            if (color == Color.Brown) return "brown";
            return color.Name;
        }

        /// <summary>
        /// Move selected objects
        /// </summary>
        private string MoveObjects(Dictionary<string, object> parameters)
        {
            var translation = GetVectorParameter(parameters, "translation", Vector3d.Zero);
            
            if (translation == Vector3d.Zero)
            {
                return "No translation vector specified";
            }

            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return "No objects selected for moving";
            }

            var transform = Transform.Translation(translation);
            int movedCount = 0;

            foreach (var obj in selectedObjects)
            {
                var result = RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, true);
                if (result != Guid.Empty)
                {
                    movedCount++;
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Moved {movedCount} object(s) by {translation}";
        }

        /// <summary>
        /// Scale selected objects
        /// </summary>
        private string ScaleObjects(Dictionary<string, object> parameters)
        {
            var scale = GetVectorParameter(parameters, "scale", Vector3d.Unset);
            
            if (scale == Vector3d.Unset)
            {
                return "No scale factor specified";
            }

            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return "No objects selected for scaling";
            }

            var transform = Transform.Scale(Point3d.Origin, scale.X);
            int scaledCount = 0;

            foreach (var obj in selectedObjects)
            {
                var result = RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, true);
                if (result != Guid.Empty)
                {
                    scaledCount++;
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Scaled {scaledCount} object(s) by factor {scale.X:F2}";
        }

        /// <summary>
        /// Perform boolean union on selected objects
        /// </summary>
        private string BooleanUnion()
        {
            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length < 2)
            {
                return "At least 2 objects must be selected for boolean union";
            }

            var breps = new List<Brep>();
            foreach (var obj in selectedObjects)
            {
                if (obj.Geometry is Brep brep)
                {
                    breps.Add(brep);
                }
            }

            if (breps.Count < 2)
            {
                return "At least 2 solid objects must be selected";
            }

            try
            {
                var unionResults = Brep.CreateBooleanUnion(breps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (unionResults?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects)
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add union result
                    foreach (var result in unionResults)
                    {
                        RhinoDoc.ActiveDoc.Objects.AddBrep(result);
                    }

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    return $"Boolean union completed. Created {unionResults.Length} object(s)";
                }
            }
            catch (Exception ex)
            {
                return $"Boolean union failed: {ex.Message}";
            }

            return "Boolean union failed";
        }

        /// <summary>
        /// Perform boolean difference on selected objects
        /// </summary>
        private string BooleanDifference()
        {
            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length < 2)
            {
                return "At least 2 objects must be selected for boolean difference";
            }

            var breps = new List<Brep>();
            foreach (var obj in selectedObjects)
            {
                if (obj.Geometry is Brep brep)
                {
                    breps.Add(brep);
                }
            }

            if (breps.Count < 2)
            {
                return "At least 2 solid objects must be selected";
            }

            try
            {
                var firstBrep = breps[0];
                var cutterBreps = breps.Skip(1).ToList();
                
                var differenceResults = Brep.CreateBooleanDifference(new[] { firstBrep }, cutterBreps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (differenceResults?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects)
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add difference result
                    foreach (var result in differenceResults)
                    {
                        RhinoDoc.ActiveDoc.Objects.AddBrep(result);
                    }

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    return $"Boolean difference completed. Created {differenceResults.Length} object(s)";
                }
            }
            catch (Exception ex)
            {
                return $"Boolean difference failed: {ex.Message}";
            }

            return "Boolean difference failed";
        }

        /// <summary>
        /// Perform boolean intersection on selected objects
        /// </summary>
        private string BooleanIntersection()
        {
            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length < 2)
            {
                return "At least 2 objects must be selected for boolean intersection";
            }

            var breps = new List<Brep>();
            foreach (var obj in selectedObjects)
            {
                if (obj.Geometry is Brep brep)
                {
                    breps.Add(brep);
                }
            }

            if (breps.Count < 2)
            {
                return "At least 2 solid objects must be selected";
            }

            try
            {
                var firstBrep = breps[0];
                var intersectBreps = breps.Skip(1).ToList();
                
                var intersectionResults = Brep.CreateBooleanIntersection(new[] { firstBrep }, intersectBreps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (intersectionResults?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects)
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add intersection result
                    foreach (var result in intersectionResults)
                    {
                        RhinoDoc.ActiveDoc.Objects.AddBrep(result);
                    }

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    return $"Boolean intersection completed. Created {intersectionResults.Length} object(s)";
                }
            }
            catch (Exception ex)
            {
                return $"Boolean intersection failed: {ex.Message}";
            }

            return "Boolean intersection failed";
        }

        /// <summary>
        /// Explode selected objects
        /// </summary>
        private string ExplodeObjects()
        {
            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return "No objects selected for exploding";
            }

            int explodedCount = 0;
            int newObjectsCount = 0;

            foreach (var obj in selectedObjects)
            {
                if (obj.Geometry is Brep brep)
                {
                    var faces = brep.Faces;
                    if (faces.Count > 1)
                    {
                        // Extract individual faces
                        foreach (BrepFace face in faces)
                        {
                            var faceSurface = face.ToBrep();
                            if (faceSurface?.IsValid == true)
                            {
                                RhinoDoc.ActiveDoc.Objects.AddBrep(faceSurface);
                                newObjectsCount++;
                            }
                        }
                        
                        // Delete original object
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                        explodedCount++;
                    }
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Exploded {explodedCount} object(s) into {newObjectsCount} new object(s)";
        }

        /// <summary>
        /// Join selected objects
        /// </summary>
        private string JoinObjects()
        {
            var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false)?.ToArray();
            if (selectedObjects == null || selectedObjects.Length < 2)
            {
                return "At least 2 objects must be selected for joining";
            }

            var breps = new List<Brep>();
            foreach (var obj in selectedObjects)
            {
                if (obj.Geometry is Brep brep)
                {
                    breps.Add(brep);
                }
            }

            if (breps.Count < 2)
            {
                return "At least 2 solid objects must be selected";
            }

            try
            {
                var joinedBreps = Brep.JoinBreps(breps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                if (joinedBreps?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects)
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add joined result
                    foreach (var result in joinedBreps)
                    {
                        RhinoDoc.ActiveDoc.Objects.AddBrep(result);
                    }

                    RhinoDoc.ActiveDoc.Views.Redraw();
                    return $"Joined {breps.Count} object(s) into {joinedBreps.Length} object(s)";
                }
            }
            catch (Exception ex)
            {
                return $"Join operation failed: {ex.Message}";
            }

            return "Join operation failed";
        }

        /// <summary>
        /// Select all objects in the document
        /// </summary>
        private string SelectAllObjects()
        {
            var allObjects = RhinoDoc.ActiveDoc.Objects.GetObjectList(ObjectType.AnyObject);
            int selectedCount = 0;

            foreach (var obj in allObjects)
            {
                if (RhinoDoc.ActiveDoc.Objects.Select(obj.Id))
                {
                    selectedCount++;
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Selected {selectedCount} object(s)";
        }

        /// <summary>
        /// Select objects by name
        /// </summary>
        private string SelectObjectsByName(Dictionary<string, object> parameters)
        {
            var name = GetStringParameter(parameters, "name", "");
            if (string.IsNullOrEmpty(name))
            {
                return "No object name specified";
            }

            var allObjects = RhinoDoc.ActiveDoc.Objects.GetObjectList(ObjectType.AnyObject);
            int selectedCount = 0;

            foreach (var obj in allObjects)
            {
                if (obj.Attributes.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (RhinoDoc.ActiveDoc.Objects.Select(obj.Id))
                    {
                        selectedCount++;
                    }
                }
            }

            RhinoDoc.ActiveDoc.Views.Redraw();
            return $"Selected {selectedCount} object(s) named '{name}'";
        }

        /// <summary>
        /// Deselect all objects
        /// </summary>
        private string DeselectAllObjects()
        {
            RhinoDoc.ActiveDoc.Objects.UnselectAll();
            RhinoDoc.ActiveDoc.Views.Redraw();
            return "Deselected all objects";
        }

        /// <summary>
        /// Initialize command templates for pattern matching
        /// </summary>
        private Dictionary<string, CommandTemplate> InitializeCommandTemplates()
        {
            return new Dictionary<string, CommandTemplate>
            {
                // Creation commands
                {
                    "CreateSphere", new CommandTemplate
                    {
                        CommandName = "CreateSphere",
                        Keywords = new[] { "sphere", "ball" },
                        Description = "Creates a sphere.",
                        Parameters = new[] { "center", "radius", "color", "name" }
                    }
                },
                {
                    "CreateBox", new CommandTemplate
                    {
                        CommandName = "CreateBox",
                        Keywords = new[] { "box", "cube" },
                        Description = "Creates a box.",
                        Parameters = new[] { "center", "size", "color", "name" }
                    }
                },
                {
                    "CreateCylinder", new CommandTemplate
                    {
                        CommandName = "CreateCylinder",
                        Keywords = new[] { "cylinder", "pipe", "tube" },
                        Description = "Creates a cylinder.",
                        Parameters = new[] { "center", "radius", "height", "color", "name" }
                    }
                },
                {
                    "CreateSphereArray", new CommandTemplate
                    {
                        CommandName = "CreateSphereArray",
                        Keywords = new[] { "array of spheres", "multiple spheres", "sphere array", "spheres array", "3x3 spheres", "grid of spheres" },
                        Description = "Creates an array of spheres.",
                        Parameters = new[] { "rows", "columns", "spacing", "radius", "center", "color", "name" }
                    }
                },
                {
                    "CreateBoxArray", new CommandTemplate
                    {
                        CommandName = "CreateBoxArray",
                        Keywords = new[] { "array of boxes", "multiple boxes", "box array", "boxes array", "grid of boxes" },
                        Description = "Creates an array of boxes.",
                        Parameters = new[] { "rows", "columns", "spacing", "size", "center", "color", "name" }
                    }
                },
                
                // Selection commands
                {
                    "SelectAll", new CommandTemplate
                    {
                        CommandName = "SelectAll",
                        Keywords = new[] { "select all", "select everything" },
                        Description = "Selects all objects.",
                        Parameters = new string[] { }
                    }
                },
                {
                    "SelectByName", new CommandTemplate
                    {
                        CommandName = "SelectByName",
                        Keywords = new[] { "select", "choose" },
                        Description = "Selects objects by name.",
                        Parameters = new[] { "name" }
                    }
                },
                {
                    "DeselectAll", new CommandTemplate
                    {
                        CommandName = "DeselectAll",
                        Keywords = new[] { "deselect all", "clear selection" },
                        Description = "Deselects all objects.",
                        Parameters = new string[] { }
                    }
                },
                
                // Transformation commands
                {
                    "Move", new CommandTemplate
                    {
                        CommandName = "Move",
                        Keywords = new[] { "move", "translate", "shift" },
                        Description = "Moves selected objects.",
                        Parameters = new[] { "translation" }
                    }
                },
                {
                    "Scale", new CommandTemplate
                    {
                        CommandName = "Scale",
                        Keywords = new[] { "scale", "resize", "size" },
                        Description = "Scales selected objects.",
                        Parameters = new[] { "scale" }
                    }
                },
                
                // Boolean operations
                {
                    "BooleanUnion", new CommandTemplate
                    {
                        CommandName = "BooleanUnion",
                        Keywords = new[] { "union", "add", "combine", "merge" },
                        Description = "Performs boolean union on selected objects.",
                        Parameters = new string[] { }
                    }
                },
                {
                    "BooleanDifference", new CommandTemplate
                    {
                        CommandName = "BooleanDifference",
                        Keywords = new[] { "difference", "subtract", "cut" },
                        Description = "Performs boolean difference on selected objects.",
                        Parameters = new string[] { }
                    }
                },
                {
                    "BooleanIntersection", new CommandTemplate
                    {
                        CommandName = "BooleanIntersection",
                        Keywords = new[] { "intersection", "intersect" },
                        Description = "Performs boolean intersection on selected objects.",
                        Parameters = new string[] { }
                    }
                },
                
                // Object operations
                {
                    "Explode", new CommandTemplate
                    {
                        CommandName = "Explode",
                        Keywords = new[] { "explode", "break", "separate" },
                        Description = "Explodes selected objects.",
                        Parameters = new string[] { }
                    }
                },
                {
                    "Join", new CommandTemplate
                    {
                        CommandName = "Join",
                        Keywords = new[] { "join", "connect", "weld" },
                        Description = "Joins selected objects.",
                        Parameters = new string[] { }
                    }
                }
            };
        }

        /// <summary>
        /// Get enhanced processing statistics
        /// </summary>
        public ProcessingStats GetProcessingStats()
        {
            return _enhancedProcessor?.GetProcessingStats() ?? new ProcessingStats();
        }

        /// <summary>
        /// Toggle enhanced processing on/off
        /// </summary>
        public void SetEnhancedProcessing(bool enabled)
        {
            // This would be controlled by user preference or configuration
        }

        /// <summary>
        /// Clear processing cache
        /// </summary>
        public void ClearCache()
        {
            _enhancedProcessor?.ClearCache();
        }

        /// <summary>
        /// Reset conversation context
        /// </summary>
        public void ResetContext()
        {
            _enhancedProcessor?.ResetContext();
        }

        public ProcessingResult ProcessCommandAsync(string input)
        {
            var result = ProcessingResult.Success($"Processed command: {input}");
            // ... existing code ...
            return result;
        }

        private Task<string> GetCurrentLayerAsync()
        {
            // In a real implementation, this would be an async call to Rhino
            return Task.FromResult(RhinoDoc.ActiveDoc.Layers.CurrentLayer.Name);
        }

        private Task<int> GetObjectCountAsync()
        {
            // In a real implementation, this would be an async call to Rhino
            return Task.FromResult(RhinoDoc.ActiveDoc.Objects.Count);
        }
    }

    #region AI Data Models

    public class AICommandRequest
    {
        public string UserInput { get; set; }
        public List<CommandTemplate> AvailableCommands { get; set; }
    }

    public class AICommandResponse
    {
        [JsonPropertyName("Actions")]
        public List<AIAction> Actions { get; set; }

        [JsonPropertyName("ResponseText")]
        public string ResponseText { get; set; }
    }

    public class AIAction
    {
        [JsonPropertyName("CommandName")]
        public string CommandName { get; set; }

        [JsonPropertyName("Parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    #endregion
} 