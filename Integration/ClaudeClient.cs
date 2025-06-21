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
    /// Anthropic Claude API client for AI text processing and reasoning
    /// </summary>
    public class ClaudeClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private bool _disposed = false;

        public ClaudeClient(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            
            InitializeClient();
        }

        /// <summary>
        /// Indicates if the Claude client is properly configured
        /// </summary>
        public bool IsConfigured { get; private set; }

        /// <summary>
        /// Initialize the Claude client
        /// </summary>
        private void InitializeClient()
        {
            try
            {
                var apiKey = _configManager.GetDecryptedApiKey("Claude");

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Claude API key not configured");
                    return;
                }

                _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "RhinoAI/1.0");

                IsConfigured = true;
                _logger.LogInformation("Claude client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Claude client");
                IsConfigured = false;
            }
        }

        /// <summary>
        /// Process text input using Claude models
        /// </summary>
        public async Task<string> ProcessTextAsync(string systemPrompt, string userInput, string model = "claude-3-sonnet-20240229")
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Claude client not configured");

            try
            {
                var request = new ClaudeRequest
                {
                    Model = model,
                    MaxTokens = 1000,
                    System = systemPrompt,
                    Messages = new[]
                    {
                        new ClaudeMessage { Role = "user", Content = userInput }
                    }
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("messages", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseJson);

                var result = claudeResponse?.Content?[0]?.Text ?? "No response received";
                _logger.LogInformation("Claude text processing completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Claude text processing failed");
                throw;
            }
        }

        /// <summary>
        /// Analyze and provide detailed reasoning for 3D modeling tasks
        /// </summary>
        public async Task<string> AnalyzeModelingTaskAsync(string userRequest)
        {
            var systemPrompt = @"You are Claude, an AI assistant specialized in 3D modeling and CAD design for Rhino 3D.
Your role is to provide detailed analysis and step-by-step reasoning for 3D modeling tasks.

When analyzing a modeling request:
1. Break down the problem into logical steps
2. Consider geometric constraints and relationships
3. Suggest appropriate Rhino tools and commands
4. Explain the reasoning behind each step
5. Anticipate potential challenges and solutions

Be thorough, precise, and educational in your explanations.";

            return await ProcessTextAsync(systemPrompt, userRequest, "claude-3-opus-20240229");
        }

        /// <summary>
        /// Generate design variations and alternatives
        /// </summary>
        public async Task<string> GenerateDesignVariationsAsync(string baseDesign, string requirements = "")
        {
            var systemPrompt = @"You are a creative design assistant for Rhino 3D.
Generate multiple design variations and alternatives based on the provided base design.

For each variation:
1. Describe the key differences from the base design
2. Explain the design rationale
3. Suggest specific Rhino modeling techniques
4. Consider functional and aesthetic aspects
5. Provide implementation guidance

Be creative while maintaining practical feasibility.";

            var fullPrompt = $"Base Design: {baseDesign}\nRequirements: {requirements}";
            return await ProcessTextAsync(systemPrompt, fullPrompt);
        }

        /// <summary>
        /// Provide technical design review and optimization suggestions
        /// </summary>
        public async Task<string> ReviewDesignAsync(string designDescription, string criteria = "")
        {
            var systemPrompt = @"You are a technical design reviewer for 3D models in Rhino.
Analyze the provided design and offer constructive feedback and optimization suggestions.

Review areas:
1. Geometric accuracy and precision
2. Manufacturing feasibility
3. Structural integrity considerations
4. Material efficiency
5. Design optimization opportunities
6. Potential modeling improvements

Provide specific, actionable recommendations with Rhino-specific guidance.";

            var fullPrompt = $"Design: {designDescription}\nReview Criteria: {criteria}";
            return await ProcessTextAsync(systemPrompt, fullPrompt, "claude-3-opus-20240229");
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

    #region Claude Data Models

    public class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("messages")]
        public ClaudeMessage[] Messages { get; set; }
    }

    public class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ClaudeResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public ClaudeContent[] Content { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage Usage { get; set; }
    }

    public class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    #endregion
} 