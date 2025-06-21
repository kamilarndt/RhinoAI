using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Rhino.Geometry;
using System.Drawing;

namespace RhinoAI.AI
{
    /// <summary>
    /// Enhanced intent classification for natural language processing
    /// </summary>
    public class IntentClassifier
    {
        private readonly Dictionary<string, IntentPattern> _patterns;

        public IntentClassifier()
        {
            _patterns = InitializeIntentPatterns();
        }

        public async Task<IntentResult> ClassifyIntentAsync(string input, ConversationContext context)
        {
            var bestMatch = new IntentResult
            {
                Category = IntentCategory.Unknown,
                Confidence = 0.0
            };

            foreach (var pattern in _patterns.Values)
            {
                var confidence = CalculateConfidence(input, pattern, context);
                if (confidence > bestMatch.Confidence)
                {
                    bestMatch = new IntentResult
                    {
                        Category = pattern.Category,
                        CommandTemplate = pattern.Template,
                        Confidence = confidence,
                        Keywords = pattern.Keywords.Where(k => input.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList()
                    };
                }
            }

            // Enhance with context-based adjustments
            if (context?.RecentOperations?.Any() == true)
            {
                bestMatch.Confidence += context.GetRelevanceScore(bestMatch.Category) * 0.1;
            }

            return bestMatch;
        }

        private double CalculateConfidence(string input, IntentPattern pattern, ConversationContext context)
        {
            var keywordMatches = pattern.Keywords.Count(k => input.Contains(k, StringComparison.OrdinalIgnoreCase));
            var keywordScore = (double)keywordMatches / pattern.Keywords.Length;

            // Boost score based on exact matches
            var exactMatches = pattern.Keywords.Count(k => 
                Regex.IsMatch(input, $@"\b{Regex.Escape(k)}\b", RegexOptions.IgnoreCase));
            var exactScore = (double)exactMatches / pattern.Keywords.Length * 0.3;

            return Math.Min(1.0, keywordScore + exactScore);
        }

        private Dictionary<string, IntentPattern> InitializeIntentPatterns()
        {
            return new Dictionary<string, IntentPattern>
            {
                ["create_sphere"] = new IntentPattern
                {
                    Category = IntentCategory.DirectCommand,
                    Keywords = new[] { "create", "sphere", "ball", "round" },
                    Template = new CommandTemplate
                    {
                        CommandName = "CreateSphere",
                        Parameters = new[] { "center", "radius", "color", "name" },
                        Description = "Creates a spherical object",
                        Keywords = new[] { "sphere", "ball", "round" }
                    }
                },
                ["create_box"] = new IntentPattern
                {
                    Category = IntentCategory.DirectCommand,
                    Keywords = new[] { "create", "box", "cube", "rectangular" },
                    Template = new CommandTemplate
                    {
                        CommandName = "CreateBox",
                        Parameters = new[] { "center", "dimensions", "color", "name" },
                        Description = "Creates a box or cube object",
                        Keywords = new[] { "box", "cube", "rectangular" }
                    }
                },
                ["create_cylinder"] = new IntentPattern
                {
                    Category = IntentCategory.DirectCommand,
                    Keywords = new[] { "create", "cylinder", "tube", "pipe" },
                    Template = new CommandTemplate
                    {
                        CommandName = "CreateCylinder",
                        Parameters = new[] { "center", "radius", "height", "color", "name" },
                        Description = "Creates a cylindrical object",
                        Keywords = new[] { "cylinder", "tube", "pipe" }
                    }
                },
                ["move_objects"] = new IntentPattern
                {
                    Category = IntentCategory.Modification,
                    Keywords = new[] { "move", "translate", "shift", "relocate" },
                    Template = new CommandTemplate
                    {
                        CommandName = "MoveObjects",
                        Parameters = new[] { "translation", "objects" },
                        Description = "Moves selected objects",
                        Keywords = new[] { "move", "translate", "shift" }
                    }
                },
                ["scale_objects"] = new IntentPattern
                {
                    Category = IntentCategory.Modification,
                    Keywords = new[] { "scale", "resize", "size", "bigger", "smaller" },
                    Template = new CommandTemplate
                    {
                        CommandName = "ScaleObjects",
                        Parameters = new[] { "scale", "objects" },
                        Description = "Scales selected objects",
                        Keywords = new[] { "scale", "resize", "size" }
                    }
                }
            };
        }
    }

    /// <summary>
    /// Manages conversation context and history
    /// </summary>
    public class ContextManager
    {
        private readonly Queue<ConversationTurn> _history;
        private readonly int _maxHistorySize = 10;

        public ContextManager()
        {
            _history = new Queue<ConversationTurn>();
        }

        public async Task<ConversationContext> EnhanceContextAsync(string input, ConversationContext existingContext)
        {
            // Add current input to history
            _history.Enqueue(new ConversationTurn
            {
                Input = input,
                Timestamp = DateTime.UtcNow
            });

            // Maintain history size
            while (_history.Count > _maxHistorySize)
            {
                _history.Dequeue();
            }

            var context = existingContext ?? new ConversationContext();

            // Update context with recent operations
            context.RecentOperations = ExtractRecentOperations();
            context.ActiveLayer = GetActiveLayerAsync().Result;
            context.SelectedObjects = GetSelectedObjectsAsync().Result;
            context.SceneDescription = GenerateSceneDescriptionAsync().Result;

            return context;
        }

        private List<string> ExtractRecentOperations()
        {
            return _history
                .Where(h => h.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                .Select(h => ExtractOperation(h.Input))
                .Where(op => !string.IsNullOrEmpty(op))
                .ToList();
        }

        private string ExtractOperation(string input)
        {
            var operations = new[] { "create", "move", "scale", "delete", "copy", "rotate" };
            foreach (var op in operations)
            {
                if (input.Contains(op, StringComparison.OrdinalIgnoreCase))
                {
                    return op;
                }
            }
            return string.Empty;
        }

        private Task<string> GetActiveLayerAsync()
        {
            // This would integrate with Rhino to get the current active layer
            return Task.FromResult("Default");
        }

        private Task<List<Guid>> GetSelectedObjectsAsync()
        {
            // This would integrate with Rhino to get currently selected objects
            return Task.FromResult(new List<Guid>());
        }

        private Task<string> GenerateSceneDescriptionAsync()
        {
            // This would analyze the current Rhino scene and generate a description
            return Task.FromResult("Empty scene");
        }
    }

    /// <summary>
    /// Enhanced parameter extraction with context awareness
    /// </summary>
    public class ParameterExtractor
    {
        private readonly Dictionary<string, Func<string, object>> _extractors;

        public ParameterExtractor()
        {
            _extractors = InitializeExtractors();
        }

        public async Task<Dictionary<string, object>> ExtractParametersAsync(
            string input, 
            CommandTemplate template, 
            ConversationContext context)
        {
            var parameters = new Dictionary<string, object>();

            foreach (var parameterName in template.Parameters)
            {
                var value = await ExtractParameterAsync(input, parameterName, context);
                if (value != null)
                {
                    parameters[parameterName] = value;
                }
            }

            return parameters;
        }

        private Task<object> ExtractParameterAsync(
            string input, 
            string parameterName, 
            ConversationContext context)
        {
            return Task.FromResult<object>(parameterName.ToLower() switch
            {
                "center" or "position" => ExtractPoint3d(input, context),
                "radius" => ExtractRadius(input),
                "dimensions" or "size" => ExtractDimensions(input),
                "height" => ExtractHeight(input),
                "color" => ExtractColor(input),
                "name" => ExtractName(input),
                "translation" => ExtractTranslation(input),
                "scale" => ExtractScale(input),
                "material" => ExtractMaterial(input),
                "layer" => ExtractLayer(input),
                _ => null
            });
        }

        private Point3d? ExtractPoint3d(string input, ConversationContext context)
        {
            // Try to extract coordinates like "at 5,10,0" or "at (5,10,0)"
            var coordMatch = Regex.Match(input, @"(?:at|position|center)\s*\(?(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,?\s*(-?\d+(?:\.\d+)?)?\)?", RegexOptions.IgnoreCase);
            if (coordMatch.Success)
            {
                var x = double.Parse(coordMatch.Groups[1].Value);
                var y = double.Parse(coordMatch.Groups[2].Value);
                var z = coordMatch.Groups[3].Success ? double.Parse(coordMatch.Groups[3].Value) : 0.0;
                return new Point3d(x, y, z);
            }

            // Try relative positions
            var relativePos = ExtractRelativePosition(input);
            if (relativePos.HasValue && context?.LastCreatedObject != null)
            {
                return context.LastCreatedObject.Position + relativePos.Value;
            }

            // Default to origin if no position specified
            return Point3d.Origin;
        }

        private double? ExtractRadius(string input)
        {
            var radiusMatch = Regex.Match(input, @"(?:radius|r)\s*(?:of|=|:)?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (radiusMatch.Success)
            {
                return double.Parse(radiusMatch.Groups[1].Value);
            }

            // Try to extract from size descriptions
            var sizeMatch = Regex.Match(input, @"(?:size|diameter)\s*(?:of|=|:)?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (sizeMatch.Success)
            {
                return double.Parse(sizeMatch.Groups[1].Value) / 2.0; // Convert diameter to radius
            }

            return null;
        }

        private Vector3d? ExtractDimensions(string input)
        {
            // Try to extract dimensions like "size 5,3,2" or "5x3x2"
            var dimMatch = Regex.Match(input, @"(?:size|dimensions?)\s*(?:of|=|:)?\s*(\d+(?:\.\d+)?)\s*[x,]\s*(\d+(?:\.\d+)?)\s*[x,]?\s*(\d+(?:\.\d+)?)?", RegexOptions.IgnoreCase);
            if (dimMatch.Success)
            {
                var x = double.Parse(dimMatch.Groups[1].Value);
                var y = double.Parse(dimMatch.Groups[2].Value);
                var z = dimMatch.Groups[3].Success ? double.Parse(dimMatch.Groups[3].Value) : x; // Default to cube if only 2 dimensions
                return new Vector3d(x, y, z);
            }

            return null;
        }

        private double? ExtractHeight(string input)
        {
            var heightMatch = Regex.Match(input, @"(?:height|h)\s*(?:of|=|:)?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (heightMatch.Success)
            {
                return double.Parse(heightMatch.Groups[1].Value);
            }
            return null;
        }

        private Color? ExtractColor(string input)
        {
            var colorNames = new Dictionary<string, Color>
            {
                { "red", Color.Red },
                { "green", Color.Green },
                { "blue", Color.Blue },
                { "yellow", Color.Yellow },
                { "orange", Color.Orange },
                { "purple", Color.Purple },
                { "pink", Color.Pink },
                { "cyan", Color.Cyan },
                { "magenta", Color.Magenta },
                { "white", Color.White },
                { "black", Color.Black },
                { "gray", Color.Gray },
                { "grey", Color.Gray },
                { "brown", Color.Brown }
            };

            foreach (var colorName in colorNames.Keys)
            {
                if (input.Contains(colorName, StringComparison.OrdinalIgnoreCase))
                {
                    return colorNames[colorName];
                }
            }

            return null;
        }

        private string ExtractName(string input)
        {
            var nameMatch = Regex.Match(input, @"(?:named?|call(?:ed)?(?:\s+it)?|with\s+name)\s+['""]?(\w+)['""]?", RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                return nameMatch.Groups[1].Value;
            }
            return null;
        }

        private Vector3d? ExtractTranslation(string input)
        {
            var translationMatch = Regex.Match(input, @"(?:by|move)\s*(?:by)?\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,?\s*(-?\d+(?:\.\d+)?)?", RegexOptions.IgnoreCase);
            if (translationMatch.Success)
            {
                var x = double.Parse(translationMatch.Groups[1].Value);
                var y = double.Parse(translationMatch.Groups[2].Value);
                var z = translationMatch.Groups[3].Success ? double.Parse(translationMatch.Groups[3].Value) : 0.0;
                return new Vector3d(x, y, z);
            }
            return null;
        }

        private double? ExtractScale(string input)
        {
            var scaleMatch = Regex.Match(input, @"(?:scale|resize)\s*(?:by|factor|to)?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (scaleMatch.Success)
            {
                return double.Parse(scaleMatch.Groups[1].Value);
            }
            return null;
        }

        private string ExtractMaterial(string input)
        {
            var materials = new[] { "wood", "metal", "plastic", "glass", "concrete", "stone" };
            foreach (var material in materials)
            {
                if (input.Contains(material, StringComparison.OrdinalIgnoreCase))
                {
                    return material;
                }
            }
            return null;
        }

        private string ExtractLayer(string input)
        {
            var layerMatch = Regex.Match(input, @"(?:on\s+layer|layer)\s+['""]?(\w+)['""]?", RegexOptions.IgnoreCase);
            return layerMatch.Success ? layerMatch.Groups[1].Value : null;
        }

        private Vector3d? ExtractRelativePosition(string input)
        {
            if (input.Contains("next to", StringComparison.OrdinalIgnoreCase))
                return new Vector3d(5, 0, 0);
            if (input.Contains("above", StringComparison.OrdinalIgnoreCase))
                return new Vector3d(0, 0, 5);
            if (input.Contains("below", StringComparison.OrdinalIgnoreCase))
                return new Vector3d(0, 0, -5);

            return null;
        }

        public Point3d GetPoint3d(Dictionary<string, object> parameters, string key, Point3d defaultValue)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                if (value is Point3d point) return point;
                if (value is double[] coords && coords.Length >= 3)
                    return new Point3d(coords[0], coords[1], coords[2]);
            }
            return defaultValue;
        }

        public double GetDouble(Dictionary<string, object> parameters, string key, double defaultValue)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                if (value is double d) return d;
                if (value is int i) return i;
                if (double.TryParse(value?.ToString(), out var parsed)) return parsed;
            }
            return defaultValue;
        }

        public string GetString(Dictionary<string, object> parameters, string key, string defaultValue)
        {
            return parameters.TryGetValue(key, out var value) ? value?.ToString() ?? defaultValue : defaultValue;
        }

        private Dictionary<string, Func<string, object>> InitializeExtractors()
        {
            return new Dictionary<string, Func<string, object>>();
        }

        public Task<Dictionary<string, object>> AdjustParametersAsync(
            Dictionary<string, object> parameters, 
            string errorMessage)
        {
            // Logic to adjust parameters based on error feedback
            var adjusted = new Dictionary<string, object>(parameters);

            if (errorMessage.Contains("radius", StringComparison.OrdinalIgnoreCase))
            {
                if (adjusted.TryGetValue("radius", out var radiusObj) && radiusObj is double radius)
                {
                    if (radius <= 0) adjusted["radius"] = 1.0;
                    if (radius > 1000) adjusted["radius"] = 10.0;
                }
            }

            return Task.FromResult(adjusted);
        }
    }

    /// <summary>
    /// Semantic validation for CAD operations
    /// </summary>
    public class SemanticValidator
    {
        public Task<ValidationResult> ValidateParametersAsync(
            Dictionary<string, object> parameters, 
            CommandTemplate template)
        {
            var result = new ValidationResult { IsValid = true };

            switch (template.CommandName)
            {
                case "CreateSphere":
                    result = ValidateSphereParameters(parameters);
                    break;

                case "CreateBox":
                    result = ValidateBoxParameters(parameters);
                    break;

                case "CreateCylinder":
                    result = ValidateCylinderParameters(parameters);
                    break;
            }

            return Task.FromResult(result);
        }

        public async Task<ValidationResult> PreExecuteValidationAsync(
            CommandTemplate template, 
            Dictionary<string, object> parameters, 
            ConversationContext context)
        {
            // Additional validation before execution
            var validationTask = ValidateParametersAsync(parameters, template);
            var result = validationTask.Result;

            if (result.IsValid)
            {
                // Check for space conflicts, layer existence, etc.
                var contextValidationTask = ValidateContextConstraints(parameters, context);
                result = contextValidationTask.Result;
            }

            return result;
        }

        private ValidationResult ValidateSphereParameters(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("radius", out var radiusObj))
            {
                if (radiusObj is double radius && radius <= 0)
                    return ValidationResult.Invalid("Sphere radius must be positive");
            }

            return ValidationResult.Valid();
        }

        private ValidationResult ValidateBoxParameters(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("dimensions", out var dimObj))
            {
                if (dimObj is Vector3d dimensions)
                {
                    if (dimensions.X <= 0 || dimensions.Y <= 0 || dimensions.Z <= 0)
                        return ValidationResult.Invalid("Box dimensions must be positive");
                }
            }

            return ValidationResult.Valid();
        }

        private ValidationResult ValidateCylinderParameters(Dictionary<string, object> parameters)
        {
            if (!parameters.ContainsKey("radius") || !parameters.ContainsKey("height"))
            {
                return ValidationResult.Invalid("Cylinder requires radius and height");
            }
            return ValidationResult.Valid();
        }

        private Task<ValidationResult> ValidateContextConstraints(
            Dictionary<string, object> parameters, 
            ConversationContext context)
        {
            // Check layer existence, object conflicts, etc.
            return Task.FromResult(ValidationResult.Valid());
        }
    }

    /// <summary>
    /// Data classes for enhanced NLP processing
    /// </summary>
    public enum IntentCategory
    {
        DirectCommand,
        ComplexOperation,
        Query,
        Modification,
        Unknown
    }

    public class IntentResult
    {
        public IntentCategory Category { get; set; }
        public CommandTemplate CommandTemplate { get; set; }
        public double Confidence { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
    }

    public class IntentPattern
    {
        public IntentCategory Category { get; set; }
        public string[] Keywords { get; set; }
        public CommandTemplate Template { get; set; }
    }

    public class ConversationContext
    {
        public List<string> RecentOperations { get; set; } = new List<string>();
        public string ActiveLayer { get; set; }
        public List<Guid> SelectedObjects { get; set; } = new List<Guid>();
        public string SceneDescription { get; set; }
        public CreatedObject LastCreatedObject { get; set; }

        public double GetRelevanceScore(IntentCategory category)
        {
            // Calculate relevance based on recent operations
            if (RecentOperations?.Any() != true) return 0.0;

            var relevantOps = category switch
            {
                IntentCategory.DirectCommand => new[] { "create" },
                IntentCategory.Modification => new[] { "move", "scale", "rotate" },
                IntentCategory.ComplexOperation => new[] { "boolean", "array", "pattern" },
                _ => new string[0]
            };

            var matches = RecentOperations.Count(op => relevantOps.Contains(op, StringComparer.OrdinalIgnoreCase));
            return (double)matches / RecentOperations.Count;
        }

        public void AddCreatedObject(Guid id, string type, Dictionary<string, object> parameters)
        {
            LastCreatedObject = new CreatedObject
            {
                Id = id,
                Type = type,
                Parameters = parameters,
                Position = parameters.TryGetValue("center", out var centerObj) && centerObj is Point3d center 
                    ? center 
                    : Point3d.Origin
            };
        }
    }

    public class CreatedObject
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public Point3d Position { get; set; }
    }

    public class ConversationTurn
    {
        public string Input { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Valid() => new ValidationResult { IsValid = true };
        public static ValidationResult Invalid(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
    }

    public class ProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public ProcessingResultType Type { get; set; }

        public static ProcessingResult Success(string message) => 
            new ProcessingResult { IsSuccess = true, Message = message, Type = ProcessingResultType.Success };

        public static ProcessingResult Error(string error) => 
            new ProcessingResult { IsSuccess = false, ErrorMessage = error, Type = ProcessingResultType.Error };

        public static ProcessingResult Warning(string message) => 
            new ProcessingResult { IsSuccess = true, Message = message, Type = ProcessingResultType.Warning };

        public static ProcessingResult Partial(string message) => 
            new ProcessingResult { IsSuccess = true, Message = message, Type = ProcessingResultType.Partial };
    }

    public enum ProcessingResultType
    {
        Success,
        Error,
        Warning,
        Partial
    }

    public class CommandTemplate
    {
        public string CommandName { get; set; }
        public string[] Parameters { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
    }

    public class EnhancedAIRequest
    {
        public string UserInput { get; set; }
        public ConversationContext Context { get; set; }
        public List<CommandTemplate> AvailableCommands { get; set; }
        public string SceneInfo { get; set; }
    }

    public class CachedResponse
    {
        public ProcessingResult Result { get; set; }
        public DateTime ExpiryTime { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiryTime;

        public CachedResponse(ProcessingResult result, DateTime expiryTime)
        {
            Result = result;
            ExpiryTime = expiryTime;
        }
    }
} 