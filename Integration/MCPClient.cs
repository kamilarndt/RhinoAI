using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using RhinoAI.Core;

namespace RhinoAI.Integration
{
    /// <summary>
    /// Client for communicating with the MCP Server
    /// </summary>
    public class MCPClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private bool _disposed = false;

        public MCPClient(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            
            InitializeClient();
        }

        public bool IsConnected { get; private set; }

        private void InitializeClient()
        {
            try
            {
                var serverUrl = _configManager.GetSetting("MCP:ServerUrl", "http://localhost:5005/");
                _httpClient.BaseAddress = new Uri(serverUrl);
                IsConnected = true;
                _logger.LogInformation("MCP Client configured for: {0}", serverUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP Client");
                IsConnected = false;
            }
        }

        /// <summary>
        /// Send scene context to MCP server for AI processing
        /// </summary>
        public async Task SendSceneContextAsync(SceneContext context)
        {
            if (!IsConnected)
            {
                _logger.LogWarning("MCP Client not connected, cannot send scene context.");
                return;
            }

            try
            {
                var request = new MCPRequest
                {
                    Type = "scene_analysis",
                    Context = context,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/context", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Scene context sent to MCP server successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send scene context to MCP server");
            }
        }

        /// <summary>
        /// Get suggestions from the MCP server
        /// </summary>
        public async Task<System.Collections.Generic.List<string>> GetSuggestionsAsync()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("MCP Client not connected, cannot get suggestions.");
                return new System.Collections.Generic.List<string>();
            }

            try
            {
                var response = await _httpClient.GetAsync("/api/suggestions");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var suggestions = JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(json);

                return suggestions ?? new System.Collections.Generic.List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get suggestions from MCP server");
                return new System.Collections.Generic.List<string>();
            }
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
} 