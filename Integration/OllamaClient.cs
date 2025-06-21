using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RhinoAI.Core;

namespace RhinoAI.Integration
{
    /// <summary>
    /// Client for interacting with local Ollama AI models
    /// </summary>
    public class OllamaClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private bool _disposed = false;

        public OllamaClient(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            
            InitializeClient();
        }

        public bool IsConfigured { get; private set; }
        public string BaseUrl { get; private set; } = "http://localhost:11434";
        public string DefaultModel { get; private set; } = "llama3.1:8b";

        /// <summary>
        /// Initialize the Ollama client
        /// </summary>
        private void InitializeClient()
        {
            try
            {
                // Get Ollama configuration
                var ollamaUrl = _configManager.GetSetting("OllamaUrl", "http://localhost:11434");
                var ollamaModel = _configManager.GetSetting("OllamaModel", "llama3.1:8b");

                BaseUrl = ollamaUrl;
                DefaultModel = ollamaModel;

                _httpClient.BaseAddress = new Uri(BaseUrl);
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "RhinoAI/1.0");
                _httpClient.Timeout = TimeSpan.FromMinutes(5); // Longer timeout for local models

                IsConfigured = true;
                _logger.LogInformation($"Ollama client initialized - URL: {BaseUrl}, Model: {DefaultModel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Ollama client");
                IsConfigured = false;
            }
        }

        /// <summary>
        /// Test if Ollama is running and accessible
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Ollama");
                return false;
            }
        }

        /// <summary>
        /// Get list of available models
        /// </summary>
        public async Task<string[]> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags");
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson);

                var modelNames = new string[modelsResponse?.Models?.Length ?? 0];
                for (int i = 0; i < modelNames.Length; i++)
                {
                    modelNames[i] = modelsResponse.Models[i].Name;
                }

                return modelNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available models");
                return new string[0];
            }
        }

        /// <summary>
        /// Process text input using Ollama models
        /// </summary>
        public async Task<string> ProcessTextAsync(string systemPrompt, string userInput, string model = null)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Ollama client not configured");

            try
            {
                var selectedModel = model ?? DefaultModel;
                
                var request = new OllamaRequest
                {
                    Model = selectedModel,
                    Prompt = $"System: {systemPrompt}\n\nUser: {userInput}\n\nAssistant:",
                    Stream = false,
                    Options = new OllamaOptions
                    {
                        Temperature = 0.7f,
                        TopP = 0.9f,
                        TopK = 40
                    }
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/generate", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

                var result = ollamaResponse?.Response ?? "No response received";
                _logger.LogInformation("Ollama text processing completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama text processing failed");
                throw;
            }
        }

        /// <summary>
        /// Analyze and provide detailed reasoning for 3D modeling tasks
        /// </summary>
        public async Task<string> AnalyzeModelingTaskAsync(string userRequest, string model = null)
        {
            var systemPrompt = @"You are an AI assistant specialized in 3D modeling and CAD design for Rhino 3D.
Your role is to provide detailed analysis and step-by-step reasoning for 3D modeling tasks.

When analyzing a modeling request:
1. Break down the problem into logical steps
2. Consider geometric constraints and relationships
3. Suggest appropriate Rhino tools and commands
4. Explain the reasoning behind each step
5. Anticipate potential challenges and solutions

Be thorough, precise, and educational in your explanations.
Respond in a clear, structured format.";

            return await ProcessTextAsync(systemPrompt, userRequest, model);
        }

        /// <summary>
        /// Generate design variations and alternatives
        /// </summary>
        public async Task<string> GenerateDesignVariationsAsync(string baseDesign, string requirements = "", string model = null)
        {
            var systemPrompt = @"You are a creative design assistant for Rhino 3D.
Generate multiple design variations and alternatives based on the provided base design.

For each variation:
1. Describe the key differences from the base design
2. Explain the design rationale
3. Suggest specific Rhino modeling techniques
4. Consider functional and aesthetic aspects
5. Provide implementation guidance

Be creative while maintaining practical feasibility.
Provide 3-5 distinct variations.";

            var fullPrompt = $"Base Design: {baseDesign}\nRequirements: {requirements}";
            return await ProcessTextAsync(systemPrompt, fullPrompt, model);
        }

        /// <summary>
        /// Extract parameters from natural language for 3D modeling
        /// </summary>
        public async Task<string> ExtractParametersAsync(string naturalLanguageInput, string model = null)
        {
            var systemPrompt = @"You are a parameter extraction AI for 3D modeling commands in Rhino.
Extract geometric parameters from natural language descriptions.

Extract these parameters when present:
- Object type (sphere, box, cylinder, etc.)
- Dimensions (radius, width, height, length)
- Position coordinates (x, y, z)
- Array information (count, spacing, grid dimensions)
- Material properties
- Colors

Return the extracted parameters in a structured JSON format.
If parameters are missing, use reasonable defaults.

Example output:
{
  ""object_type"": ""sphere"",
  ""radius"": 5,
  ""position"": [0, 0, 0],
  ""array"": {""enabled"": true, ""rows"": 3, ""columns"": 3, ""spacing"": 10}
}";

            return await ProcessTextAsync(systemPrompt, naturalLanguageInput, model);
        }

        /// <summary>
        /// Configure Ollama settings
        /// </summary>
        public void Configure(string baseUrl, string defaultModel)
        {
            BaseUrl = baseUrl ?? "http://localhost:11434";
            DefaultModel = defaultModel ?? "llama3.1:8b";

            _configManager.SetSetting("OllamaUrl", BaseUrl);
            _configManager.SetSetting("OllamaModel", DefaultModel);

            InitializeClient();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }

    #region Ollama Data Models

    public class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaOptions Options { get; set; }
    }

    public class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public float TopP { get; set; }

        [JsonPropertyName("top_k")]
        public int TopK { get; set; }
    }

    public class OllamaResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("response")]
        public string Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("context")]
        public int[] Context { get; set; }

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
    }

    public class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public OllamaModel[] Models { get; set; }
    }

    public class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("modified_at")]
        public string ModifiedAt { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("digest")]
        public string Digest { get; set; }

        [JsonPropertyName("details")]
        public OllamaModelDetails Details { get; set; }
    }

    public class OllamaModelDetails
    {
        [JsonPropertyName("parent_model")]
        public string ParentModel { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("family")]
        public string Family { get; set; }

        [JsonPropertyName("families")]
        public string[] Families { get; set; }

        [JsonPropertyName("parameter_size")]
        public string ParameterSize { get; set; }

        [JsonPropertyName("quantization_level")]
        public string QuantizationLevel { get; set; }
    }

    #endregion
} 