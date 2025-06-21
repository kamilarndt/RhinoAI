using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.DocObjects;

using RhinoAI.Core;
using RhinoAI.Integration;

namespace RhinoAI.AI
{
    /// <summary>
    /// Handles computer vision processing for 3D scenes
    /// </summary>
    public class VisionProcessor : IDisposable
    {
        private readonly ConfigurationManager _config;
        private readonly SimpleLogger _logger;
        private readonly OpenAIClient _openAIClient;
        private bool _disposed = false;

        public VisionProcessor(ConfigurationManager config, SimpleLogger logger, OpenAIClient openAIClient)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        }

        /// <summary>
        /// Analyze a 3D scene using computer vision
        /// </summary>
        public async Task<SceneAnalysis> AnalyzeSceneAsync(ViewportInfo viewport)
        {
            try
            {
                _logger?.LogInformation("Starting AI scene analysis...");

                if (!_openAIClient.IsConfigured)
                {
                    _logger.LogWarning("OpenAI client not configured. Cannot perform vision analysis.");
                    return new SceneAnalysis { Suggestions = new List<string> { "Error: OpenAI client is not configured." } };
                }

                var screenshot = await CaptureViewportAsync(viewport);
                if (screenshot == null)
                {
                    _logger.LogWarning("Failed to capture viewport for analysis.");
                    return new SceneAnalysis { Suggestions = new List<string> { "Error: Failed to capture viewport." } };
                }

                var prompt = CreateVisionPrompt();
                var aiResponse = await _openAIClient.AnalyzeImageAsync(screenshot, prompt);

                var analysis = ParseAIResponse(aiResponse);
                analysis.Timestamp = DateTime.UtcNow;
                analysis.ViewportInfo = viewport;
                analysis.Screenshot = screenshot;

                _logger?.LogInformation("AI scene analysis complete.");
                return analysis;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI scene analysis failed");
                return new SceneAnalysis { Suggestions = new List<string> { $"Error: {ex.Message}" } };
            }
        }
        
        /// <summary>
        /// Creates the system prompt for the vision analysis request.
        /// </summary>
        private string CreateVisionPrompt()
        {
            return @"You are a 3D design analysis expert specializing in Rhino.
Analyze the provided image of a Rhino 3D scene.

Provide your analysis in a JSON format with the following structure:
{
  ""detectedObjects"": [
    {
      ""name"": ""(a descriptive name of the object)"",
      ""type"": ""(e.g., 'Surface', 'Polysurface', 'Curve')"",
      ""confidence"": (a value from 0.0 to 1.0)
    }
  ],
  ""qualityScore"": (a value from 0.0 to 1.0 assessing the overall design quality),
  ""suggestions"": [
    ""(a list of actionable suggestions for improvement)""
  ]
}

Focus on identifying key geometric elements, assessing the design's quality, clarity, and potential issues. Provide concise, actionable suggestions for improvement.";
        }

        /// <summary>
        /// Parses the JSON response from the AI into a SceneAnalysis object.
        /// </summary>
        private SceneAnalysis ParseAIResponse(string jsonResponse)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    _logger.LogWarning("AI vision response was empty.");
                    return new SceneAnalysis { Suggestions = new List<string> { "AI returned an empty response." }};
                }

                // Clean the response to ensure it's valid JSON
                var cleanedJson = jsonResponse.Trim().Trim('`');
                if (cleanedJson.StartsWith("json"))
                {
                    cleanedJson = cleanedJson.Substring(4).Trim();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                var analysis = JsonSerializer.Deserialize<SceneAnalysis>(cleanedJson, options);
                
                // The deserializer should handle populating the lists.
                // If they are null, initialize them.
                analysis.DetectedObjects ??= new List<DetectedObject>();
                analysis.Suggestions ??= new List<string>();

                return analysis;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI vision response JSON.");
                // Return the raw response as a suggestion for debugging
                return new SceneAnalysis { Suggestions = new List<string> { "Failed to parse AI response.", jsonResponse } };
            }
             catch (Exception ex)
            {
                _logger?.LogError(ex, "An unexpected error occurred while parsing the AI response.");
                return new SceneAnalysis { Suggestions = new List<string> { "An unexpected error occurred during parsing." } };
            }
        }


        /// <summary>
        /// Capture screenshot of viewport
        /// </summary>
        private async Task<byte[]?> CaptureViewportAsync(ViewportInfo viewport)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var view = RhinoDoc.ActiveDoc?.Views?.ActiveView;
                    if (view == null)
                    {
                        _logger?.LogWarning("No active view found to capture.");
                        return null;
                    }

                    using (var bitmap = view.CaptureToBitmap())
                    {
                        if (bitmap == null)
                        {
                            _logger?.LogWarning("Failed to capture view to bitmap.");
                            return null;
                        }

                        using (var ms = new System.IO.MemoryStream())
                        {
                            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            return ms.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to capture viewport");
                    return null;
                }
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Cleanup resources
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Results of scene analysis
    /// </summary>
    public class SceneAnalysis
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonIgnore]
        public ViewportInfo? ViewportInfo { get; set; }

        [JsonIgnore]
        public byte[]? Screenshot { get; set; }
        
        [JsonPropertyName("detectedObjects")]
        public List<DetectedObject> DetectedObjects { get; set; } = new();
        
        [JsonPropertyName("qualityScore")]
        public double QualityScore { get; set; }
        
        [JsonPropertyName("suggestions")]
        public List<string> Suggestions { get; set; } = new();
    }

    /// <summary>
    /// Detected object information
    /// </summary>
    public class DetectedObject
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonIgnore]
        public BoundingBox BoundingBox { get; set; }
        
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
} 