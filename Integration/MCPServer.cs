using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using RhinoAI.Core;

namespace RhinoAI.Integration
{
    /// <summary>
    /// In-process MCP Server for AI context management and suggestions
    /// </summary>
    public class MCPServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly SimpleLogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly ContextManager _contextManager;
        private readonly SuggestionEngine _suggestionEngine;
        private readonly Func<string, Task<string>> _commandProcessor;
        private Thread _serverThread;
        private bool _isRunning = false;
        private bool _disposed = false;

        public MCPServer(
            ConfigurationManager configManager, 
            SimpleLogger logger, 
            ContextManager contextManager, 
            SuggestionEngine suggestionEngine,
            Func<string, Task<string>> commandProcessor)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            _suggestionEngine = suggestionEngine ?? throw new ArgumentNullException(nameof(suggestionEngine));
            _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
            
            _listener = new HttpListener();
        }

        public bool IsRunning => _isRunning;

        /// <summary>
        /// Start the MCP server
        /// </summary>
        public bool Start()
        {
            if (_isRunning)
            {
                _logger.LogWarning("MCP Server is already running.");
                return true;
            }

            try
            {
                var serverUrl = _configManager.GetSetting("MCP:ServerUrl", "http://localhost:5005/");
                _listener.Prefixes.Add(serverUrl);
                _listener.Start();
                _isRunning = true;

                _serverThread = new Thread(HandleRequests);
                _serverThread.Start();

                _logger.LogInformation("MCP Server started and listening on {0}", serverUrl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start MCP Server");
                _isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Stop the MCP server
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;
                _listener.Stop();
                _serverThread.Join();
                _logger.LogInformation("MCP Server stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping MCP Server.");
            }
        }

        private void HandleRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    Task.Run(() => ProcessRequest(context));
                }
                catch (HttpListenerException ex)
                {
                    // This exception is expected when the listener is stopped.
                    if (ex.ErrorCode == 995) 
                    {
                        _logger.LogInformation("HttpListener was gracefully stopped.");
                    }
                    else
                    {
                        _logger.LogError(ex, "HttpListenerException in request handler loop.");
                    }
                    
                    _isRunning = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in request handler loop.");
                    _isRunning = false;
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                _logger.LogDebug("Received request: {0} {1}", request.HttpMethod, request.Url.AbsolutePath);

                if (request.HttpMethod == "POST")
                {
                    switch (request.Url.AbsolutePath)
                    {
                        case "/api/context":
                            await HandleContextRequest(request, response);
                            break;
                        case "/api/suggestions":
                            await HandleSuggestionsRequest(request, response);
                            break;
                        case "/api/model":
                            await HandleModelRequest(request, response);
                            break;
                        default:
                            SendResponse(response, HttpStatusCode.NotFound, new { message = "Endpoint not found" });
                            break;
                    }
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/api/command")
                {
                    await HandleCommandRequestAsync(request, response);
                }
                else
                {
                    SendResponse(response, HttpStatusCode.MethodNotAllowed, new { message = "Method not allowed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request.");
                SendResponse(response, HttpStatusCode.InternalServerError, new { message = "An internal server error occurred." });
            }
            finally
            {
                response.Close();
            }
        }
        
        private async Task HandleCommandRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var commandText = request.QueryString["text"];

            if (string.IsNullOrWhiteSpace(commandText))
            {
                SendResponse(response, HttpStatusCode.BadRequest, new { message = "Command text cannot be empty." });
                return;
            }

            try
            {
                var result = await _commandProcessor(commandText);
                SendResponse(response, HttpStatusCode.OK, new { success = true, message = "Command processed.", result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing command: {commandText}");
                SendResponse(response, HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
            }
        }

        private async Task HandleContextRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var requestBody = await new StreamReader(request.InputStream).ReadToEndAsync();
            var mcpRequest = JsonSerializer.Deserialize<MCPRequest>(requestBody);

            _contextManager.UpdateContext(mcpRequest.Context);
            
            var mcpResponse = new MCPResponse { Success = true, Message = "Context updated successfully." };
            SendResponse(response, HttpStatusCode.OK, mcpResponse);
        }
        
        private async Task HandleSuggestionsRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var requestBody = await new StreamReader(request.InputStream).ReadToEndAsync();
            var mcpRequest = JsonSerializer.Deserialize<MCPRequest>(requestBody);

            var suggestions = await _suggestionEngine.GetSuggestionsAsync(mcpRequest.UserInput, mcpRequest.Context);
            var suggestionsResponse = new SuggestionsResponse { Success = true, Suggestions = suggestions };
            
            SendResponse(response, HttpStatusCode.OK, suggestionsResponse);
        }

        private async Task HandleModelRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Placeholder for model update logic
            await Task.CompletedTask;
            var mcpResponse = new MCPResponse { Success = true, Message = "Model update processed." };
            SendResponse(response, HttpStatusCode.OK, mcpResponse);
        }

        private void SendResponse(HttpListenerResponse response, HttpStatusCode statusCode, object responseObject)
        {
            response.StatusCode = (int)statusCode;
            response.ContentType = "application/json";
            
            var json = JsonSerializer.Serialize(responseObject);
            var buffer = Encoding.UTF8.GetBytes(json);
            
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }

    #region Data Models

    public class MCPRequest
    {
        public string Type { get; set; }
        public string UserInput { get; set; }
        public SceneContext Context { get; set; }
        public ModelContext ModelContext { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MCPResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    public class SuggestionsResponse
    {
        public bool Success { get; set; }
        public AISuggestion[] Suggestions { get; set; }
    }

    public class AISuggestion
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Command { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public double Confidence { get; set; }
    }

    public class SceneContext
    {
        public int ObjectCount { get; set; }
        public string[] ObjectTypes { get; set; }
        public double[] BoundingBox { get; set; }
        public string[] ActiveLayers { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }

    public class ModelContext
    {
        public string ModelId { get; set; }
        public string ModelType { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public DateTime LastModified { get; set; }
    }

    #endregion
} 