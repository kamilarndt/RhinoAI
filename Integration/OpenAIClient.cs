using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using RhinoAI.Core;
// No need to bring in the whole namespace, models are now in this file.

namespace RhinoAI.Integration
{
    /// <summary>
    /// OpenAI API client for AI text processing and image analysis
    /// </summary>
    public class OpenAIClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private bool _disposed = false;

        public OpenAIClient(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            
            InitializeClient();
        }

        /// <summary>
        /// Indicates if the OpenAI client is properly configured
        /// </summary>
        public bool IsConfigured { get; private set; }

        /// <summary>
        /// Initialize the OpenAI client
        /// </summary>
        private void InitializeClient()
        {
            try
            {
                var apiKey = _configManager.GetDecryptedApiKey("OpenAI");

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("OpenAI API key not configured");
                    IsConfigured = false;
                    return;
                }

                _httpClient.BaseAddress = new Uri(_configManager.GetSetting("OpenAI:ApiUrl", "https://api.openai.com/v1/"));
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "RhinoAI/1.0");

                IsConfigured = true;
                _logger.LogInformation("OpenAI client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client");
                IsConfigured = false;
            }
        }

        /// <summary>
        /// Process text input using OpenAI GPT models
        /// </summary>
        public async Task<string> ProcessTextAsync(string systemPrompt, string userInput, string model = "gpt-3.5-turbo")
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI client not configured");

            try
            {
                var request = new OpenAIChatRequest
                {
                    Model = model,
                    Messages = new List<OpenAIMessage>
                    {
                        new OpenAIMessage { Role = "system", Content = systemPrompt },
                        new OpenAIMessage { Role = "user", Content = userInput }
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var openAIResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);

                var result = openAIResponse?.Choices?[0]?.Message?.Content ?? "No response received";
                _logger.LogInformation("OpenAI text processing completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI text processing failed");
                throw;
            }
        }

        /// <summary>
        /// Analyze image using OpenAI Vision models
        /// </summary>
        public async Task<string> AnalyzeImageAsync(byte[] imageData, string prompt = "Analyze this 3D model image")
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI client not configured");

            try
            {
                var base64Image = Convert.ToBase64String(imageData);
                var imageUrl = $"data:image/png;base64,{base64Image}";

                var request = new OpenAIVisionRequest
                {
                    Model = "gpt-4-vision-preview",
                    Messages = new List<OpenAIVisionMessage>
                    {
                        new OpenAIVisionMessage
                        {
                            Role = "user",
                            Content = new List<OpenAIVisionContent>
                            {
                                new OpenAIVisionContent { Type = "text", Text = prompt },
                                new OpenAIVisionContent { Type = "image_url", ImageUrl = new OpenAIVisionImageUrl { Url = imageUrl } }
                            }
                        }
                    },
                    MaxTokens = 1500
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var visionResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);

                var result = visionResponse?.Choices?[0]?.Message?.Content ?? "No analysis received";
                _logger.LogInformation("OpenAI image analysis completed successfully");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI image analysis failed");
                throw;
            }
        }

        /// <summary>
        /// Generate 3D model description from text
        /// </summary>
        public async Task<string> GenerateModelDescriptionAsync(string userRequest)
        {
            var systemPrompt = @"You are an expert 3D modeling assistant for Rhino 3D. 
Given a user's description, provide detailed step-by-step instructions for creating the 3D model.
Include specific Rhino commands, dimensions, and modeling techniques.
Be precise and professional in your response.";

            return await ProcessTextAsync(systemPrompt, userRequest, "gpt-4");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
} 