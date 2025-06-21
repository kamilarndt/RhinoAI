using System;
using System.Threading.Tasks;

using Rhino.Geometry;
using RhinoAI.AI;
using RhinoAI.Integration;
using System.Collections.Generic;
using RhinoAI.Models;

namespace RhinoAI.Core
{
    /// <summary>
    /// Central manager for all AI operations and integrations
    /// </summary>
    public class AIManager : IDisposable
    {
        private readonly ConfigurationManager _configManager;
        private readonly SimpleLogger _logger;
        private bool _disposed = false;

        // AI Components
        public NLPProcessor NlpProcessor { get; private set; }
        public VisionProcessor VisionProcessor { get; private set; }
        public GenerativeDesigner GenerativeDesigner { get; private set; }
        public RealTimeAssistant RealTimeAssistant { get; private set; }
        public MCPServer MCPServer { get; private set; }
        public OpenAIClient OpenAIClient { get; private set; }
        public ClaudeClient ClaudeClient { get; private set; }
        public MCPClient MCPClient { get; private set; }
        
        // Core Components
        private ContextManager _contextManager;
        private SuggestionEngine _suggestionEngine;

        public AIManager(ConfigurationManager configManager, SimpleLogger logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes all AI components and returns true if successful
        /// </summary>
        public bool InitializeAll()
        {
            try
            {
                _logger?.LogInformation("Initializing AI components...");

                // Check for API keys
                if (!_configManager.HasApiKey("OpenAI"))
                {
                    _logger?.LogWarning("OpenAI API key not configured");
                }
                if (!_configManager.HasApiKey("Claude"))
                {
                    _logger?.LogWarning("Claude API key not configured");
                }

                // Initialize clients
                OpenAIClient = new OpenAIClient(_configManager, _logger);
                ClaudeClient = new ClaudeClient(_configManager, _logger);
                
                // Initialize AI processors
                NlpProcessor = new NLPProcessor(_configManager, _logger);
                VisionProcessor = new VisionProcessor(_configManager, _logger, OpenAIClient);
                GenerativeDesigner = new GenerativeDesigner(_configManager, _logger);
                
                // Initialize MCP Client
                MCPClient = new MCPClient(_configManager, _logger);

                RealTimeAssistant = new RealTimeAssistant(_configManager, _logger, MCPClient);
                _logger?.LogInformation("AI processors initialized");

                // Initialize Core Components
                _contextManager = new ContextManager(_logger);
                _suggestionEngine = new SuggestionEngine(_logger, OpenAIClient, ClaudeClient);

                // Initialize MCP server
                MCPServer = new MCPServer(_configManager, _logger, _contextManager, _suggestionEngine, ProcessNaturalLanguageAsync);
                if (!MCPServer.Start())
                {
                    _logger?.LogWarning("MCP Server failed to start. Some features may be unavailable.");
                    // We don't return false here, as MCP might be optional
                }
                else
                {
                    _logger?.LogInformation("MCP Server started successfully.");
                }

                _logger?.LogInformation("AI components initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "A critical error occurred during AI Manager initialization.");
                return false;
            }
        }

        /// <summary>
        /// Process natural language input
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>AI response</returns>
        public async Task<string> ProcessNaturalLanguageAsync(string input)
        {
            try
            {
                if (NlpProcessor == null)
                {
                    throw new InvalidOperationException("NLP Processor not initialized");
                }

                return await NlpProcessor.ProcessNaturalLanguageAsync(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing natural language input: {input}");
                throw;
            }
        }

        /// <summary>
        /// Generate design variants
        /// </summary>
        /// <param name="prompt">Design prompt</param>
        /// <param name="variantCount">Number of variants to generate</param>
        /// <returns>Generated variants</returns>
        public async Task<IEnumerable<GeometryBase>> GenerateDesignVariantsAsync(string prompt, int variantCount = 3)
        {
            try
            {
                if (GenerativeDesigner == null)
                {
                    throw new InvalidOperationException("Generative Designer not initialized");
                }

                var designPrompt = new Models.DesignPrompt { Text = prompt };
                return await GenerativeDesigner.GenerateVariantsAsync(designPrompt, variantCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating design variants for prompt: {prompt}");
                throw;
            }
        }

        /// <summary>
        /// Start real-time assistance
        /// </summary>
        public void StartRealTimeAssistance()
        {
            try
            {
                RealTimeAssistant?.StartMonitoring();
                _logger.LogInformation("Real-time assistance started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start real-time assistance");
                throw;
            }
        }

        /// <summary>
        /// Stop real-time assistance
        /// </summary>
        public void StopRealTimeAssistance()
        {
            try
            {
                RealTimeAssistant?.StopMonitoring();
                _logger.LogInformation("Real-time assistance stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping real-time assistance");
            }
        }

        /// <summary>
        /// Check if AI services are available
        /// </summary>
        /// <returns>True if at least one AI service is available</returns>
        public bool IsAIServiceAvailable()
        {
            return OpenAIClient?.IsConfigured == true || ClaudeClient?.IsConfigured == true;
        }

        /// <summary>
        /// Get AI service status
        /// </summary>
        /// <returns>Status information</returns>
        public AIServiceStatus GetServiceStatus()
        {
            return new AIServiceStatus
            {
                OpenAIAvailable = OpenAIClient?.IsConfigured == true,
                ClaudeAvailable = ClaudeClient?.IsConfigured == true,
                MCPServerRunning = MCPServer?.IsRunning == true,
                RealTimeAssistanceActive = RealTimeAssistant?.IsMonitoring == true
            };
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
                _logger.LogInformation("Disposing AI Manager...");

                RealTimeAssistant?.StopMonitoring();
                MCPServer?.Stop();
                
                OpenAIClient?.Dispose();
                ClaudeClient?.Dispose();
                MCPServer?.Dispose();
                MCPClient?.Dispose();
                RealTimeAssistant?.Dispose();

                _disposed = true;
                _logger.LogInformation("AI Manager disposed");
            }
        }
    }

    /// <summary>
    /// AI service status information
    /// </summary>
    public class AIServiceStatus
    {
        public bool OpenAIAvailable { get; set; }
        public bool ClaudeAvailable { get; set; }
        public bool MCPServerRunning { get; set; }
        public bool RealTimeAssistanceActive { get; set; }
        
        public bool AnyServiceAvailable => OpenAIAvailable || ClaudeAvailable;
    }
} 