using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoAI.Core;
using RhinoAI.Integration;
using System.Text.Json;

namespace RhinoAI.UI.Commands
{
    /// <summary>
    /// Main AI Assistant command - primary interface for AI interactions
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class AIAssistantCommand : Command
    {
        public override string EnglishName => "AIAssistant";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Check if plugin is properly loaded
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager == null)
                {
                    RhinoApp.WriteLine("RhinoAI plugin not properly initialized. Please restart Rhino.");
                    return Result.Failure;
                }

                // Show the AI Control Panel
                plugin.ShowAIControlPanel();
                
                RhinoApp.WriteLine("AI Assistant panel opened. You can now interact with AI to help with your 3D modeling tasks.");
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error opening AI Assistant: {ex.Message}");
                return Result.Failure;
            }
        }
    }

    /// <summary>
    /// Quick AI command for immediate text-based AI assistance
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class AIQuickCommand : Command
    {
        public override string EnglishName => "AIQuick";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager == null)
                {
                    RhinoApp.WriteLine("RhinoAI plugin not properly initialized.");
                    return Result.Failure;
                }

                // Get user input
                var gs = new GetString();
                gs.SetCommandPrompt("Enter your modeling request or question");
                gs.AcceptNothing(false);
                
                var result = gs.GetLiteralString();
                if (result != GetResult.String)
                    return Result.Cancel;

                var userInput = gs.StringResult();
                if (string.IsNullOrWhiteSpace(userInput))
                    return Result.Cancel;

                // Process with AI asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        RhinoApp.WriteLine($"AI is processing: {userInput}");
                        var response = await plugin.AIManager.NlpProcessor.ProcessNaturalLanguageAsync(userInput);
                        RhinoApp.WriteLine($"AI Response: {response}");
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"AI processing failed: {ex.Message}");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error in AI Quick command: {ex.Message}");
                return Result.Failure;
            }
        }
    }

    /// <summary>
    /// Command to analyze current scene with AI
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class AIAnalyzeSceneCommand : Command
    {
        public override string EnglishName => "AIAnalyzeScene";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager == null)
                {
                    RhinoApp.WriteLine("RhinoAI plugin not properly initialized.");
                    return Result.Failure;
                }

                // Analyze current scene
                _ = Task.Run(async () =>
                {
                    try
                    {
                        RhinoApp.WriteLine("AI is analyzing the current scene...");
                        
                        // Create scene context
                        var sceneContext = CreateSceneContext(doc);
                        
                        // Use MCP server if available, otherwise use direct AI
                        if (plugin.AIManager.MCPClient?.IsConnected == true)
                        {
                            await plugin.AIManager.MCPClient.SendSceneContextAsync(sceneContext);
                            RhinoApp.WriteLine("Scene context sent to MCP server for analysis.");
                        }
                        else
                        {
                            // Fallback to direct AI analysis
                            var sceneDescription = GenerateSceneDescription(sceneContext);
                            var analysis = await plugin.AIManager.NlpProcessor.ProcessNaturalLanguageAsync(
                                $"Analyze this 3D scene: {sceneDescription}");
                            RhinoApp.WriteLine($"Scene Analysis: {analysis}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Scene analysis failed: {ex.Message}");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error in AI Analyze Scene command: {ex.Message}");
                return Result.Failure;
            }
        }

        private SceneContext CreateSceneContext(RhinoDoc doc)
        {
            var objectTypes = new System.Collections.Generic.List<string>();
            var boundingBox = new double[6] { 0, 0, 0, 0, 0, 0 };
            var activeLayers = new System.Collections.Generic.List<string>();

            try
            {
                // Collect object information
                foreach (var obj in doc.Objects)
                {
                    if (obj != null && obj.Geometry != null)
                    {
                        objectTypes.Add(obj.Geometry.ObjectType.ToString());
                    }
                }

                // Get overall bounding box
                if (doc.Objects.Count > 0)
                {
                    var bbox = Rhino.Geometry.BoundingBox.Empty;
                    foreach (var obj in doc.Objects)
                    {
                        if (obj?.Geometry != null)
                        {
                            bbox.Union(obj.Geometry.GetBoundingBox(true));
                        }
                    }

                    if (bbox.IsValid)
                    {
                        boundingBox = new double[] 
                        { 
                            bbox.Min.X, bbox.Min.Y, bbox.Min.Z,
                            bbox.Max.X, bbox.Max.Y, bbox.Max.Z 
                        };
                    }
                }

                // Get active layers
                foreach (var layer in doc.Layers)
                {
                    if (layer != null && layer.IsVisible)
                    {
                        activeLayers.Add(layer.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error creating scene context: {ex.Message}");
            }

            // Build custom properties dictionary separately to avoid initializer complexities
            var customProps = new System.Collections.Generic.Dictionary<string, object>();
            customProps["DocumentName"] = doc.Name ?? "Untitled";
            customProps["Units"] = doc.ModelUnitSystem.ToString();
            customProps["ViewCount"] = doc.Views.Count();

            return new SceneContext
            {
                ObjectCount = doc.Objects.Count,
                ObjectTypes = objectTypes.ToArray(),
                BoundingBox = boundingBox,
                ActiveLayers = activeLayers.ToArray(),
                CustomProperties = customProps
            };
        }

        private string GenerateSceneDescription(SceneContext context)
        {
            var description = $"3D Scene with {context.ObjectCount} objects. ";
            
            if (context.ObjectTypes?.Length > 0)
            {
                var uniqueTypes = new System.Collections.Generic.HashSet<string>();
                foreach (var type in context.ObjectTypes)
                {
                    uniqueTypes.Add(type);
                }
                description += $"Object types: {string.Join(", ", uniqueTypes)}. ";
            }

            if (context.BoundingBox?.Length == 6)
            {
                var width = Math.Abs(context.BoundingBox[3] - context.BoundingBox[0]);
                var height = Math.Abs(context.BoundingBox[4] - context.BoundingBox[1]);
                var depth = Math.Abs(context.BoundingBox[5] - context.BoundingBox[2]);
                description += $"Dimensions approximately {width:F1} x {height:F1} x {depth:F1} units. ";
            }

            if (context.ActiveLayers?.Length > 0)
            {
                description += $"Active layers: {string.Join(", ", context.ActiveLayers)}. ";
            }

            return description;
        }
    }

    /// <summary>
    /// Command to get AI suggestions for current selection
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class AISuggestCommand : Command
    {
        public override string EnglishName => "AISuggest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager == null)
                {
                    RhinoApp.WriteLine("RhinoAI plugin not properly initialized.");
                    return Result.Failure;
                }

                // Get selected objects
                var filter = new Rhino.DocObjects.ObjectEnumeratorSettings();
                filter.SelectedObjectsFilter = true;
                var selectedObjects = doc.Objects.FindByFilter(filter);
                if (selectedObjects == null || selectedObjects.Length == 0)
                {
                    RhinoApp.WriteLine("Please select objects first to get AI suggestions.");
                    return Result.Cancel;
                }

                // Process with AI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        RhinoApp.WriteLine($"AI is analyzing {selectedObjects.Length} selected objects...");
                        
                        var selectionDescription = GenerateSelectionDescription(selectedObjects);
                        var suggestions = await plugin.AIManager.NlpProcessor.ProcessNaturalLanguageAsync(
                            $"Suggest modeling operations for these selected objects: {selectionDescription}");

                        RhinoApp.WriteLine($"AI Suggestions: {suggestions}");
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"AI suggestion failed: {ex.Message}");
                    }
                });

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error in AI Suggest command: {ex.Message}");
                return Result.Failure;
            }
        }

        private string GenerateSelectionDescription(Rhino.DocObjects.RhinoObject[] objects)
        {
            var description = $"{objects.Length} selected objects: ";
            var types = new System.Collections.Generic.List<string>();

            foreach (var obj in objects)
            {
                if (obj?.Geometry != null)
                {
                    types.Add(obj.Geometry.ObjectType.ToString());
                }
            }

            var uniqueTypes = new System.Collections.Generic.HashSet<string>(types);
            description += string.Join(", ", uniqueTypes);

            return description;
        }
    }
} 