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
                "CreateTorus" => await CreateTorusAsync(parameters),
                "CreateCone" => await CreateConeAsync(parameters),
                "CreateLine" => await CreateLineAsync(parameters),
                "CreateCircle" => await CreateCircleAsync(parameters),
                "CreateArc" => await CreateArcAsync(parameters),
                "CreatePolyline" => await CreatePolylineAsync(parameters),
                "CreateSphereArray" => await CreateSphereArrayAsync(parameters),
                "CreateBoxArray" => await CreateBoxArrayAsync(parameters),
                "CreateCircularArray" => await CreateCircularArrayAsync(parameters),
                "MoveObjects" => await MoveObjectsAsync(parameters),
                "ScaleObjects" => await ScaleObjectsAsync(parameters),
                "RotateObjects" => await RotateObjectsAsync(parameters),
                "MirrorObjects" => await MirrorObjectsAsync(parameters),
                "CopyObjects" => await CopyObjectsAsync(parameters),
                "BooleanUnion" => await BooleanUnionAsync(parameters),
                "BooleanDifference" => await BooleanDifferenceAsync(parameters),
                "BooleanIntersection" => await BooleanIntersectionAsync(parameters),
                "ExplodeObjects" => await ExplodeObjectsAsync(parameters),
                "JoinObjects" => await JoinObjectsAsync(parameters),
                "FilletEdges" => await FilletEdgesAsync(parameters),
                "ChamferEdges" => await ChamferEdgesAsync(parameters),
                _ => ProcessingResult.Error($"Command '{template.CommandName}' is not implemented")
            };
        }

        #region Command Implementations

        private Task<ProcessingResult> CreateSphereArrayAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
                var rows = _parameterExtractor.GetInt(parameters, "rows", 3);
                var columns = _parameterExtractor.GetInt(parameters, "columns", 3);
                var spacing = _parameterExtractor.GetDouble(parameters, "spacing", radius * 3);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var createdCount = 0;
                var createdIds = new List<Guid>();

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        var sphereCenter = new Point3d(
                            center.X + col * spacing,
                            center.Y + row * spacing,
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
                                attributes.Name = $"{name}_sphere_{row}_{col}";
                            }

                            var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                            
                            if (id != Guid.Empty)
                            {
                                createdIds.Add(id);
                                createdCount++;
                                _currentContext.AddCreatedObject(id, "Sphere", parameters);
                            }
                        }
                    }
                }

                if (createdCount > 0)
                {
                    // Select all created spheres
                    foreach (var id in createdIds)
                    {
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                    }
                    RhinoDoc.ActiveDoc.Views.Redraw();

                    var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                    return Task.FromResult(ProcessingResult.Success($"Created {createdCount} spheres in {colorName} in a {rows}x{columns} array with radius {radius:F2} and spacing {spacing:F2}"));
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create sphere array"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating sphere array: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateBoxArrayAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var dimensions = GetVector3dParameter(parameters, "dimensions", new Vector3d(1, 1, 1));
                var rows = _parameterExtractor.GetInt(parameters, "rows", 3);
                var columns = _parameterExtractor.GetInt(parameters, "columns", 3);
                var spacing = _parameterExtractor.GetDouble(parameters, "spacing", Math.Max(dimensions.X, dimensions.Y) * 2);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var createdCount = 0;
                var createdIds = new List<Guid>();

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        var boxCenter = new Point3d(
                            center.X + col * spacing,
                            center.Y + row * spacing,
                            center.Z
                        );

                        var box = new Box(Plane.WorldXY, new Interval(-dimensions.X/2, dimensions.X/2),
                                         new Interval(-dimensions.Y/2, dimensions.Y/2),
                                         new Interval(-dimensions.Z/2, dimensions.Z/2));
                        
                        // Translate to position
                        var transform = Transform.Translation(boxCenter - Point3d.Origin);
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
                                attributes.Name = $"{name}_box_{row}_{col}";
                            }

                            var id = RhinoDoc.ActiveDoc.Objects.AddBrep(brep, attributes);
                            
                            if (id != Guid.Empty)
                            {
                                createdIds.Add(id);
                                createdCount++;
                                _currentContext.AddCreatedObject(id, "Box", parameters);
                            }
                        }
                    }
                }

                if (createdCount > 0)
                {
                    // Select all created boxes
                    foreach (var id in createdIds)
                    {
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                    }
                    RhinoDoc.ActiveDoc.Views.Redraw();

                    var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                    return Task.FromResult(ProcessingResult.Success($"Created {createdCount} boxes in {colorName} in a {rows}x{columns} array with dimensions {dimensions.X:F1}x{dimensions.Y:F1}x{dimensions.Z:F1} and spacing {spacing:F2}"));
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create box array"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating box array: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateTorusAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var majorRadius = _parameterExtractor.GetDouble(parameters, "major_radius", 5.0);
                var minorRadius = _parameterExtractor.GetDouble(parameters, "minor_radius", 2.0);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var torus = new Torus(Plane.WorldXY, majorRadius, minorRadius);
                
                // Move to center by creating at the right position
                var plane = new Plane(center, Vector3d.ZAxis);
                torus = new Torus(plane, majorRadius, minorRadius);
                
                var brep = torus.ToBrep();

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
                        _currentContext.AddCreatedObject(id, "Torus", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return Task.FromResult(ProcessingResult.Success($"Created torus in {colorName}{nameInfo} at {center} with major radius {majorRadius:F2} and minor radius {minorRadius:F2}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create torus"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating torus: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateConeAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
                var height = _parameterExtractor.GetDouble(parameters, "height", 3.0);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var basePlane = new Plane(center, Vector3d.ZAxis);
                var cone = new Cone(basePlane, height, radius);
                var brep = cone.ToBrep(true); // true = cap the base

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
                        _currentContext.AddCreatedObject(id, "Cone", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return Task.FromResult(ProcessingResult.Success($"Created cone in {colorName}{nameInfo} at {center} with radius {radius:F2} and height {height:F2}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create cone"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating cone: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateLineAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var start = _parameterExtractor.GetPoint3d(parameters, "start", Point3d.Origin);
                var end = _parameterExtractor.GetPoint3d(parameters, "end", new Point3d(10, 0, 0));
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var line = new Line(start, end);
                var curve = line.ToNurbsCurve();

                if (curve?.IsValid == true)
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

                    var id = RhinoDoc.ActiveDoc.Objects.AddCurve(curve, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        _currentContext.AddCreatedObject(id, "Line", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return Task.FromResult(ProcessingResult.Success($"Created line in {colorName}{nameInfo} from {start} to {end}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create line"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating line: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateCircleAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var plane = new Plane(center, Vector3d.ZAxis);
                var circle = new Circle(plane, radius);
                var curve = circle.ToNurbsCurve();

                if (curve?.IsValid == true)
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

                    var id = RhinoDoc.ActiveDoc.Objects.AddCurve(curve, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        _currentContext.AddCreatedObject(id, "Circle", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return Task.FromResult(ProcessingResult.Success($"Created circle in {colorName}{nameInfo} at {center} with radius {radius:F2}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create circle"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating circle: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateArcAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 1.0);
                var angle = _parameterExtractor.GetDouble(parameters, "angle", 90.0); // degrees
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var plane = new Plane(center, Vector3d.ZAxis);
                var angleRadians = RhinoMath.ToRadians(angle);
                var arc = new Arc(plane, radius, angleRadians);
                var curve = arc.ToNurbsCurve();

                if (curve?.IsValid == true)
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

                    var id = RhinoDoc.ActiveDoc.Objects.AddCurve(curve, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        _currentContext.AddCreatedObject(id, "Arc", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        
                        return Task.FromResult(ProcessingResult.Success($"Created arc in {colorName}{nameInfo} at {center} with radius {radius:F2} and angle {angle:F1}°"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create arc"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating arc: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreatePolylineAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Extract points from parameters - this would need more sophisticated parsing
                var points = new List<Point3d> { Point3d.Origin, new Point3d(5, 0, 0), new Point3d(5, 5, 0), new Point3d(0, 5, 0) };
                var closed = _parameterExtractor.GetString(parameters, "closed", "false").ToLower() == "true";
                var name = _parameterExtractor.GetString(parameters, "name", "");
                var color = GetColorParameter(parameters, "color");

                var polyline = new Polyline(points);
                if (closed) polyline.Add(points[0]); // Close the polyline
                
                var curve = polyline.ToNurbsCurve();

                if (curve?.IsValid == true)
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

                    var id = RhinoDoc.ActiveDoc.Objects.AddCurve(curve, attributes);
                    
                    if (id != Guid.Empty)
                    {
                        _currentContext.AddCreatedObject(id, "Polyline", parameters);
                        RhinoDoc.ActiveDoc.Objects.Select(id);
                        RhinoDoc.ActiveDoc.Views.Redraw();

                        var colorName = color.HasValue ? GetColorName(color.Value) : "default";
                        var nameInfo = !string.IsNullOrEmpty(name) ? $" named '{name}'" : "";
                        var closedText = closed ? "closed" : "open";
                        
                        return Task.FromResult(ProcessingResult.Success($"Created {closedText} polyline in {colorName}{nameInfo} with {points.Count} points"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create polyline"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating polyline: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateSphereAsync(Dictionary<string, object> parameters)
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
                        
                        return Task.FromResult(ProcessingResult.Success($"Created sphere in {colorName}{nameInfo} at {center} with radius {radius}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create sphere"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating sphere: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateBoxAsync(Dictionary<string, object> parameters)
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
                        
                        return Task.FromResult(ProcessingResult.Success($"Created box in {colorName}{nameInfo} at {center} with dimensions {dimensions.X}x{dimensions.Y}x{dimensions.Z}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create box"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating box: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateCylinderAsync(Dictionary<string, object> parameters)
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
                        
                        return Task.FromResult(ProcessingResult.Success($"Created cylinder in {colorName}{nameInfo} at {center} with radius {radius} and height {height}"));
                    }
                }

                return Task.FromResult(ProcessingResult.Error("Failed to create cylinder"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating cylinder: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> MoveObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var translation = GetVector3dParameter(parameters, "translation", Vector3d.Zero);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected to move"));
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
                return Task.FromResult(ProcessingResult.Success($"Moved {movedCount} object(s) by {translation}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error moving objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> ScaleObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var scale = _parameterExtractor.GetDouble(parameters, "scale", 1.0);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected to scale"));
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
                return Task.FromResult(ProcessingResult.Success($"Scaled {scaledCount} object(s) by factor {scale:F2}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error scaling objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> BooleanUnionAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return Task.FromResult(ProcessingResult.Warning("Need at least 2 objects selected for boolean union"));
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

                    return Task.FromResult(ProcessingResult.Success($"Created boolean union from {breps.Length} objects"));
                }

                return Task.FromResult(ProcessingResult.Error("Boolean union operation failed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error in boolean union: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> BooleanDifferenceAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return Task.FromResult(ProcessingResult.Warning("Need at least 2 objects selected for boolean difference"));
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

                    return Task.FromResult(ProcessingResult.Success($"Created boolean difference from {breps.Length} objects"));
                }

                return Task.FromResult(ProcessingResult.Error("Boolean difference operation failed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error in boolean difference: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> BooleanIntersectionAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return Task.FromResult(ProcessingResult.Warning("Need at least 2 objects selected for boolean intersection"));
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

                    return Task.FromResult(ProcessingResult.Success($"Created boolean intersection from {breps.Length} objects"));
                }

                return Task.FromResult(ProcessingResult.Error("Boolean intersection operation failed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error in boolean intersection: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> ExplodeObjectsAsync(Dictionary<string, object> parameters)
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
                return Task.FromResult(ProcessingResult.Success($"Exploded {selectedObjects.Count()} object(s) into {explodedCount} faces"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error exploding objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> JoinObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);
                var breps = selectedObjects.Where(o => o.Geometry is Brep).Select(o => o.Geometry as Brep).ToArray();

                if (breps.Length < 2)
                {
                    return Task.FromResult(ProcessingResult.Warning("Need at least 2 objects selected for joining"));
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
                    return Task.FromResult(ProcessingResult.Success($"Joined {breps.Length} objects into {joinedBreps.Length} object(s)"));
                }

                return Task.FromResult(ProcessingResult.Error("Join operation failed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error joining objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CreateCircularArrayAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var count = _parameterExtractor.GetInt(parameters, "count", 6);
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 5.0);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected for circular array"));
                }

                var createdCount = 0;
                var angleStep = 2 * Math.PI / count;

                for (int i = 1; i < count; i++) // Start from 1 since original is at 0
                {
                    var angle = i * angleStep;
                    var x = center.X + radius * Math.Cos(angle);
                    var y = center.Y + radius * Math.Sin(angle);
                    var arrayPoint = new Point3d(x, y, center.Z);
                    
                    var translation = arrayPoint - center;
                    var transform = Transform.Translation(translation);

                    foreach (var obj in selectedObjects)
                    {
                        var duplicate = RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, false);
                        if (duplicate != Guid.Empty)
                        {
                            createdCount++;
                        }
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return Task.FromResult(ProcessingResult.Success($"Created circular array with {createdCount} objects around {center} with radius {radius:F2}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error creating circular array: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> RotateObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var angle = _parameterExtractor.GetDouble(parameters, "angle", 90.0); // degrees
                var axis = GetVector3dParameter(parameters, "axis", Vector3d.ZAxis);
                var center = _parameterExtractor.GetPoint3d(parameters, "center", Point3d.Origin);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected to rotate"));
                }

                var angleRadians = RhinoMath.ToRadians(angle);
                var transform = Transform.Rotation(angleRadians, axis, center);
                var rotatedCount = 0;

                foreach (var obj in selectedObjects)
                {
                    if (RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, true) != Guid.Empty)
                    {
                        rotatedCount++;
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return Task.FromResult(ProcessingResult.Success($"Rotated {rotatedCount} object(s) by {angle:F1}° around {axis} at {center}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error rotating objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> MirrorObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var planeOrigin = _parameterExtractor.GetPoint3d(parameters, "plane_origin", Point3d.Origin);
                var planeNormal = GetVector3dParameter(parameters, "plane_normal", Vector3d.XAxis);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected to mirror"));
                }

                var mirrorPlane = new Plane(planeOrigin, planeNormal);
                var transform = Transform.Mirror(mirrorPlane);
                var mirroredCount = 0;

                foreach (var obj in selectedObjects)
                {
                    var mirrored = RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, false);
                    if (mirrored != Guid.Empty)
                    {
                        mirroredCount++;
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return Task.FromResult(ProcessingResult.Success($"Mirrored {mirroredCount} object(s) across plane at {planeOrigin} with normal {planeNormal}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error mirroring objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> CopyObjectsAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var translation = GetVector3dParameter(parameters, "translation", new Vector3d(5, 0, 0));
                var copies = _parameterExtractor.GetInt(parameters, "copies", 1);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected to copy"));
                }

                var copiedCount = 0;

                for (int i = 1; i <= copies; i++)
                {
                    var transform = Transform.Translation(translation * i);
                    
                    foreach (var obj in selectedObjects)
                    {
                        var copy = RhinoDoc.ActiveDoc.Objects.Transform(obj.Id, transform, false);
                        if (copy != Guid.Empty)
                        {
                            copiedCount++;
                        }
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return Task.FromResult(ProcessingResult.Success($"Created {copiedCount} copies with translation {translation}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error copying objects: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> FilletEdgesAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var radius = _parameterExtractor.GetDouble(parameters, "radius", 0.5);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected for filleting"));
                }

                var filletedCount = 0;

                foreach (var obj in selectedObjects)
                {
                    if (obj.Geometry is Brep brep)
                    {
                        // Get all edges that are not smooth manifold edges
                        var edges = new List<BrepEdge>();
                        foreach (var edge in brep.Edges)
                        {
                            if (!edge.IsSmoothManifoldEdge)
                            {
                                edges.Add(edge);
                            }
                        }
                        
                        if (edges.Count > 0)
                        {
                            var filletBreps = Brep.CreateFilletEdges(brep, edges.Select(e => e.EdgeIndex), 
                                                                    Enumerable.Repeat(radius, edges.Count), 
                                                                    Enumerable.Repeat(radius, edges.Count), 
                                                                    BlendType.Fillet, RailType.RollingBall, 
                                                                    RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                            
                            if (filletBreps?.Length > 0 && filletBreps[0]?.IsValid == true)
                            {
                                RhinoDoc.ActiveDoc.Objects.Replace(obj.Id, filletBreps[0]);
                                filletedCount++;
                            }
                        }
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return Task.FromResult(ProcessingResult.Success($"Applied fillet with radius {radius:F2} to {filletedCount} object(s)"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error filleting edges: {ex.Message}"));
            }
        }

        private Task<ProcessingResult> ChamferEdgesAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var distance = _parameterExtractor.GetDouble(parameters, "distance", 0.5);
                var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false);

                if (selectedObjects?.Count() == 0)
                {
                    return Task.FromResult(ProcessingResult.Warning("No objects selected for chamfering"));
                }

                var chamferedCount = 0;

                foreach (var obj in selectedObjects)
                {
                    if (obj.Geometry is Brep brep)
                    {
                        // Get all edges that are not smooth manifold edges
                        var edges = new List<BrepEdge>();
                        foreach (var edge in brep.Edges)
                        {
                            if (!edge.IsSmoothManifoldEdge)
                            {
                                edges.Add(edge);
                            }
                        }
                        
                        if (edges.Count > 0)
                        {
                            var chamferBreps = Brep.CreateFilletEdges(brep, edges.Select(e => e.EdgeIndex), 
                                                                     Enumerable.Repeat(distance, edges.Count), 
                                                                     Enumerable.Repeat(distance, edges.Count), 
                                                                     BlendType.Chamfer, RailType.RollingBall, 
                                                                     RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                            
                            if (chamferBreps?.Length > 0 && chamferBreps[0]?.IsValid == true)
                            {
                                RhinoDoc.ActiveDoc.Objects.Replace(obj.Id, chamferBreps[0]);
                                chamferedCount++;
                            }
                        }
                    }
                }

                RhinoDoc.ActiveDoc.Views.Redraw();
                return Task.FromResult(ProcessingResult.Success($"Applied chamfer with distance {distance:F2} to {chamferedCount} object(s)"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult.Error($"Error chamfering edges: {ex.Message}"));
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