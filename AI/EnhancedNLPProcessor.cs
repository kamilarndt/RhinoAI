using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Drawing;

namespace RhinoAI.AI
{
    /// <summary>
    /// Enhanced Natural Language Processor with advanced context awareness,
    /// intent classification, and semantic validation
    /// </summary>
    public class EnhancedNLPProcessor
    {
        private readonly IntentClassifier _intentClassifier;
        private readonly ContextManager _contextManager;
        private readonly ParameterExtractor _parameterExtractor;
        private readonly SemanticValidator _semanticValidator;
        private readonly Dictionary<string, CachedResponse> _responseCache;
        private ConversationContext _currentContext;

        public EnhancedNLPProcessor()
        {
            _intentClassifier = new IntentClassifier();
            _contextManager = new ContextManager();
            _parameterExtractor = new ParameterExtractor();
            _semanticValidator = new SemanticValidator();
            _responseCache = new Dictionary<string, CachedResponse>();
            _currentContext = new ConversationContext();
        }

        /// <summary>
        /// Main processing method that handles natural language input with full context awareness
        /// </summary>
        public async Task<ProcessingResult> ProcessAsync(string input)
        {
            try
            {
                // Check cache first
                var cacheKey = GenerateCacheKey(input, _currentContext);
                if (_responseCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
                {
                    return cached.Result;
                }

                // Step 1: Enhance context with current input
                _currentContext = await _contextManager.EnhanceContextAsync(input, _currentContext);

                // Step 2: Classify intent
                var intentResult = await _intentClassifier.ClassifyIntentAsync(input, _currentContext);
                
                if (intentResult.Confidence < 0.3)
                {
                    return ProcessingResult.Error("I'm not sure what you want me to do. Could you be more specific?");
                }

                // Step 3: Extract parameters
                var parameters = await _parameterExtractor.ExtractParametersAsync(
                    input, intentResult.CommandTemplate, _currentContext);

                // Step 4: Semantic validation
                var validationResult = await _semanticValidator.PreExecuteValidationAsync(
                    intentResult.CommandTemplate, parameters, _currentContext);

                if (!validationResult.IsValid)
                {
                    // Try to adjust parameters automatically
                    parameters = await _parameterExtractor.AdjustParametersAsync(parameters, validationResult.ErrorMessage);
                    
                    // Re-validate
                    validationResult = await _semanticValidator.ValidateParametersAsync(parameters, intentResult.CommandTemplate);
                    
                    if (!validationResult.IsValid)
                    {
                        return ProcessingResult.Error($"Parameter validation failed: {validationResult.ErrorMessage}");
                    }
                }

                // Step 5: Execute command
                var result = await ExecuteCommandAsync(intentResult.CommandTemplate, parameters);

                // Step 6: Cache successful results
                if (result.IsSuccess)
                {
                    _responseCache[cacheKey] = new CachedResponse(result, DateTime.UtcNow.AddMinutes(5));
                }

                return result;
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Processing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes the identified command with extracted parameters
        /// </summary>
        private async Task<ProcessingResult> ExecuteCommandAsync(CommandTemplate template, Dictionary<string, object> parameters)
        {
            return template.CommandName switch
            {
                "CreateSphere" => await CreateSphereAsync(parameters),
                "CreateBox" => await CreateBoxAsync(parameters),
                "CreateCylinder" => await CreateCylinderAsync(parameters),
                "MoveObjects" => await MoveObjectsAsync(parameters),
                "ScaleObjects" => await ScaleObjectsAsync(parameters),
                "BooleanUnion" => await BooleanUnionAsync(parameters),
                "BooleanDifference" => await BooleanDifferenceAsync(parameters),
                "BooleanIntersection" => await BooleanIntersectionAsync(parameters),
                "ExplodeObjects" => await ExplodeObjectsAsync(parameters),
                "JoinObjects" => await JoinObjectsAsync(parameters),
                _ => ProcessingResult.Error($"Command '{template.CommandName}' is not implemented")
            };
        }

        #region Command Implementations

        private async Task<ProcessingResult> CreateSphereAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

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

                    if (!string.IsNullOrEmpty(name))
                    {
                        attributes.Name = name;
                    }

                    var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        // Update context with created object
                        _currentContext.AddCreatedObject(id, "Sphere", parameters);
                        
                        // Select the created object
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return ProcessingResult.Success($"Created sphere in {colorName}{nameInfo} at {center} with radius {radius}");
                    }
                }

                return ProcessingResult.Error("Failed to create sphere");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error creating sphere: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> CreateBoxAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var dimensions = GetVector3dParameter(parameters, "dimensions", new Vector3d(1, 1, 1));
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var box = new Box(Plane.WorldXY, new Interval(-dimensions.X/2, dimensions.X/2),
                                 new Interval(-dimensions.Y/2, dimensions.Y/2),
                                 new Interval(-dimensions.Z/2, dimensions.Z/2));
                
                // Translate to center
                var transform = Transform.Translation(center - Point3d.Origin);
                box.Transform(transform);
                
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
                        attributes.Name = name;
                    }

                    var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        _currentContext.AddCreatedObject(id, "Box", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return ProcessingResult.Success($"Created box in {colorName}{nameInfo} at {center} with dimensions {dimensions.X}x{dimensions.Y}x{dimensions.Z}");
                    }
                }

                return ProcessingResult.Error("Failed to create box");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error creating box: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> CreateCylinderAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
                var height = _parameterExtractor.GetDouble(parameters, "height", 2.0);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var basePlane = new Plane(center, Vector3d.ZAxis);
                var circle = new Circle(basePlane, radius);
                var cylinder = new Cylinder(circle, height);
                var brep = cylinder.ToBrep(true, true);

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
                        attributes.Name = name;
                    }

                    var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        _currentContext.AddCreatedObject(id, "Cylinder", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return ProcessingResult.Success($"Created cylinder in {colorName}{nameInfo} at {center} with radius {radius} and height {height}");
                    }
                }

                return ProcessingResult.Error("Failed to create cylinder");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error creating cylinder: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> MoveObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var translation = GetVector3dParameter(parameters, "translation", Vector3d.Zero);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return ProcessingResult.Warning("No objects selected to move");
                }

                var transform = Transform.Translation(translation);
                var movedCount = 0;

                foreach (var obj in selectedObjects)
                {
                    if (RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, true) != Guid.Empty)
                    {
                        movedCount++;
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return ProcessingResult.Success($"Moved {movedCount} object(s) by {translation}");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error moving objects: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> ScaleObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var scale = _parameterExtractor.GetDouble(parameters, "scale", 1.0);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return ProcessingResult.Warning("No objects selected to scale");
                }

                var transform = Transform.Scale(Point3d.Origin, scale);
                var scaledCount = 0;

                foreach (var obj in selectedObjects)
                {
                    if (RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, true) != Guid.Empty)
                    {
                        scaledCount++;
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return ProcessingResult.Success($"Scaled {scaledCount} object(s) by factor {scale:F2}");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error scaling objects: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> BooleanUnionAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return ProcessingResult.Warning("Need at least 2 objects selected for boolean union");
                }

                var unionResult = Brep.CreateBooleanUnion(breps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                
                if (unionResult?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects.Where(o => o.Geometry is Brep))
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add union result
                    var unionId = RhinoDoc.ActiveDoc.Objects.AddBrep(unionResult[0]);
                    RhinoDoc.ActiveDoc.Objects.Select(unionId);
                    RhinoDoc.ActiveDoc.Views.Redraw();

                    return ProcessingResult.Success($"Created boolean union from {breps.Length} objects");
                }

                return ProcessingResult.Error("Boolean union operation failed");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error in boolean union: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> BooleanDifferenceAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return ProcessingResult.Warning("Need at least 2 objects selected for boolean difference");
                }

                var firstBrep = breps[0];
                var otherBreps = breps.Skip(1).ToArray();

                var differenceResult = Brep.CreateBooleanDifference(new[] { firstBrep }, otherBreps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                
                if (differenceResult?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects.Where(o => o.Geometry is Brep))
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add difference result
                    var diffId = RhinoDoc.ActiveDoc.Objects.AddBrep(differenceResult[0]);
                    RhinoDoc.ActiveDoc.Objects.Select(diffId);
                    RhinoDoc.ActiveDoc.Views.Redraw();

                    return ProcessingResult.Success($"Created boolean difference from {breps.Length} objects");
                }

                return ProcessingResult.Error("Boolean difference operation failed");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error in boolean difference: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> BooleanIntersectionAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return ProcessingResult.Warning("Need at least 2 objects selected for boolean intersection");
                }

                var firstBrep = breps[0];
                var otherBreps = breps.Skip(1).ToArray();

                var intersectionResult = Brep.CreateBooleanIntersection(new[] { firstBrep }, otherBreps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                
                if (intersectionResult?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects.Where(o => o.Geometry is Brep))
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add intersection result
                    var intId = RhinoDoc.ActiveDoc.Objects.AddBrep(intersectionResult[0]);
                    RhinoDoc.ActiveDoc.Objects.Select(intId);
                    RhinoDoc.ActiveDoc.Views.Redraw();

                    return ProcessingResult.Success($"Created boolean intersection from {breps.Length} objects");
                }

                return ProcessingResult.Error("Boolean intersection operation failed");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error in boolean intersection: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> ExplodeObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var explodedCount = 0;

                foreach (var obj in selectedObjects)
                {
                    if (obj.Geometry is Brep brep)
                    {
                        var faces = brep.Faces;
                        foreach (var face in faces)
                        {
                            var faceBrep = face.ToBrep();
                            if (faceBrep?.IsValid == true)
                            {
                                RhinoDoc.ActiveDoc.Objects.AddBrep(faceBrep);
                                explodedCount++;
                            }
                        }
                        
                        // Delete original object
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return ProcessingResult.Success($"Exploded {selectedObjects.Count()} object(s) into {explodedCount} faces");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error exploding objects: {ex.Message}");
            }
        }

        private async Task<ProcessingResult> JoinObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return ProcessingResult.Warning("Need at least 2 objects selected for joining");
                }

                var joinedBreps = Brep.JoinBreps(breps, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                
                if (joinedBreps?.Length > 0)
                {
                    // Delete original objects
                    foreach (var obj in selectedObjects.Where(o => o.Geometry is Brep))
                    {
                        RhinoDoc.ActiveDoc.Objects.Delete(obj.Id, true);
                    }

                    // Add joined results
                    foreach (var joinedBrep in joinedBreps)
                    {
                        var joinId = RhinoDoc.ActiveDoc.Objects.AddBrep(joinedBrep);
                        RhinoDoc.ActiveDoc.Objects.Select(joinId);
                    }
                    
                    RhinoDoc.ActiveDoc.Views.Redraw();
                    return ProcessingResult.Success($"Joined {breps.Length} objects into {joinedBreps.Length} object(s)");
                }

                return ProcessingResult.Error("Join operation failed");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error joining objects: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private Color? GetColorParameter(Dictionary<string, object> parameters, string key)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                if (value is Color color) return color;
                if (value is System.Drawing.Color drawingColor) return drawingColor;
            }
            return null;
        }

        private Vector3d GetVector3dParameter(Dictionary<string, object> parameters, string key, Vector3d defaultValue)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                if (value is Vector3d vector) return vector;
                if (value is double[] coords && coords.Length >= 3)
                    return new Vector3d(coords[0], coords[1], coords[2]);
            }
            return defaultValue;
        }

        private string GetColorName(Color color)
        {
            var colorNames = new Dictionary<Color, string>
            {
                { Color.Red, "red" },
                { Color.Green, "green" },
                { Color.Blue, "blue" },
                { Color.Yellow, "yellow" },
                { Color.Orange, "orange" },
                { Color.Purple, "purple" },
                { Color.Pink, "pink" },
                { Color.Cyan, "cyan" },
                { Color.Magenta, "magenta" },
                { Color.White, "white" },
                { Color.Black, "black" },
                { Color.Gray, "gray" },
                { Color.Brown, "brown" }
            };

            return colorNames.TryGetValue(color, out var name) ? name : "custom color";
        }

        private string GenerateCacheKey(string input, ConversationContext context)
        {
            var contextHash = context?.GetHashCode().ToString() ?? "0";
            return $"{input.GetHashCode()}_{contextHash}";
        }

        /// <summary>
        /// Gets processing statistics for monitoring and optimization
        /// </summary>
        public ProcessingStats GetProcessingStats()
        {
            return new ProcessingStats
            {
                CacheHitCount = _responseCache.Count,
                ContextOperationsCount = _currentContext?.RecentOperations?.Count ?? 0,
                LastProcessingTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Clears the response cache
        /// </summary>
        public void ClearCache()
        {
            _responseCache.Clear();
        }

        /// <summary>
        /// Resets the conversation context
        /// </summary>
        public void ResetContext()
        {
            _currentContext = new ConversationContext();
        }

        #endregion
    }

    /// <summary>
    /// Processing statistics for monitoring
    /// </summary>
    public class ProcessingStats
    {
        public int CacheHitCount { get; set; }
        public int ContextOperationsCount { get; set; }
        public DateTime LastProcessingTime { get; set; }
    }
} 