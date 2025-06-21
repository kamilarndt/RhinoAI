using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

using Rhino;
using Rhino.DocObjects;
using RhinoAI.Core;
using RhinoAI.Integration;
using Timer = System.Timers.Timer;

namespace RhinoAI.AI
{
    /// <summary>
    /// Provides real-time assistance and monitoring during modeling
    /// </summary>
    public class RealTimeAssistant : IDisposable
    {
        private readonly ConfigurationManager _config;
        private readonly SimpleLogger _logger;
        private readonly MCPClient _mcpClient;
        private readonly Timer _monitoringTimer;
        private bool _disposed = false;
        private bool _isMonitoring = false;

        public bool IsMonitoring => _isMonitoring;

        public RealTimeAssistant(ConfigurationManager config, SimpleLogger logger, MCPClient mcpClient)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
            
            _monitoringTimer = new Timer(5000); // Check every 5 seconds
            _monitoringTimer.Elapsed += OnMonitoringTimerElapsed;
        }

        /// <summary>
        /// Start real-time monitoring
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            try
            {
                _logger?.LogInformation("Starting real-time assistance monitoring...");

                // Subscribe to Rhino document events
                RhinoDoc.NewDocument += OnNewDocument;
                RhinoDoc.BeginOpenDocument += OnBeginOpenDocument;
                RhinoDoc.EndOpenDocument += OnEndOpenDocument;

                // Subscribe to object events if there's an active document
                var doc = RhinoDoc.ActiveDoc;
                if (doc != null)
                {
                    SubscribeToDocumentEvents(doc);
                }

                _monitoringTimer.Start();
                _isMonitoring = true;

                _logger?.LogInformation("Real-time assistance monitoring started");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start real-time monitoring");
                throw;
            }
        }

        /// <summary>
        /// Stop real-time monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            try
            {
                _logger?.LogInformation("Stopping real-time assistance monitoring...");

                _monitoringTimer.Stop();

                // Unsubscribe from Rhino document events
                RhinoDoc.NewDocument -= OnNewDocument;
                RhinoDoc.BeginOpenDocument -= OnBeginOpenDocument;
                RhinoDoc.EndOpenDocument -= OnEndOpenDocument;

                // Unsubscribe from object events
                var doc = RhinoDoc.ActiveDoc;
                if (doc != null)
                {
                    UnsubscribeFromDocumentEvents(doc);
                }

                _isMonitoring = false;

                _logger?.LogInformation("Real-time assistance monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping real-time monitoring");
            }
        }

        /// <summary>
        /// Subscribe to document-specific events
        /// </summary>
        private void SubscribeToDocumentEvents(RhinoDoc doc)
        {
            RhinoDoc.AddRhinoObject += OnObjectAdded;
            RhinoDoc.DeleteRhinoObject += OnObjectDeleted;
            RhinoDoc.ReplaceRhinoObject += OnObjectReplaced;
            RhinoDoc.UndeleteRhinoObject += OnObjectUndeleted;
        }

        /// <summary>
        /// Unsubscribe from document-specific events
        /// </summary>
        private void UnsubscribeFromDocumentEvents(RhinoDoc doc)
        {
            RhinoDoc.AddRhinoObject -= OnObjectAdded;
            RhinoDoc.DeleteRhinoObject -= OnObjectDeleted;
            RhinoDoc.ReplaceRhinoObject -= OnObjectReplaced;
            RhinoDoc.UndeleteRhinoObject -= OnObjectUndeleted;
        }

        /// <summary>
        /// Handle new document event
        /// </summary>
        private void OnNewDocument(object? sender, DocumentEventArgs e)
        {
            _logger?.LogInformation("New document created - setting up monitoring");
            if (e.Document != null)
            {
                SubscribeToDocumentEvents(e.Document);
            }
        }

        /// <summary>
        /// Handle begin open document event
        /// </summary>
        private void OnBeginOpenDocument(object? sender, DocumentOpenEventArgs e)
        {
            _logger?.LogDebug($"Opening document: {e.FileName}");
        }

        /// <summary>
        /// Handle end open document event
        /// </summary>
        private void OnEndOpenDocument(object? sender, DocumentOpenEventArgs e)
        {
            _logger?.LogInformation($"Document opened: {e.FileName} - setting up monitoring");
            if (e.Document != null)
            {
                SubscribeToDocumentEvents(e.Document);
            }
        }

        /// <summary>
        /// Handle object added event
        /// </summary>
        private async void OnObjectAdded(object? sender, RhinoObjectEventArgs e)
        {
            try
            {
                _logger?.LogDebug($"Object added: {e.TheObject.Id}");
                
                await UpdateSceneContextAsync();

                var analysis = await AnalyzeObjectAsync(e.TheObject);
                if (analysis.HasIssues)
                {
                    ShowSuggestions(analysis.Suggestions);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing added object");
            }
        }

        /// <summary>
        /// Handle object deleted event
        /// </summary>
        private void OnObjectDeleted(object? sender, RhinoObjectEventArgs e)
        {
            _logger?.LogDebug($"Object deleted: {e.TheObject.Id}");
        }

        /// <summary>
        /// Handle object replaced event
        /// </summary>
        private async void OnObjectReplaced(object? sender, RhinoReplaceObjectEventArgs e)
        {
            try
            {
                _logger?.LogDebug($"Object replaced: {e.NewRhinoObject.Id}");

                await UpdateSceneContextAsync();
                
                var analysis = await AnalyzeObjectAsync(e.NewRhinoObject);
                if (analysis.HasIssues)
                {
                    ShowSuggestions(analysis.Suggestions);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error analyzing replaced object");
            }
        }

        /// <summary>
        /// Handle object undeleted event
        /// </summary>
        private void OnObjectUndeleted(object? sender, RhinoObjectEventArgs e)
        {
            _logger?.LogDebug($"Object undeleted: {e.TheObject.Id}");
        }

        /// <summary>
        /// Handle monitoring timer elapsed
        /// </summary>
        private async void OnMonitoringTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await PerformPeriodicAnalysisAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during periodic analysis");
            }
        }

        /// <summary>
        /// Perform periodic analysis of the scene
        /// </summary>
        private async Task PerformPeriodicAnalysisAsync()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc?.Objects == null) return;

            await UpdateSceneContextAsync();

            await Task.Run(() =>
            {
                try
                {
                    var objectCount = doc.Objects.Count;
                    
                    // Check for performance issues
                    if (objectCount > 10000)
                    {
                        ShowSuggestion("Large number of objects detected. Consider using layers or blocks to organize your model.");
                    }

                    // Check for memory usage (placeholder)
                    var memoryUsage = GC.GetTotalMemory(false);
                    if (memoryUsage > 1024 * 1024 * 1024) // 1GB
                    {
                        ShowSuggestion("High memory usage detected. Consider simplifying your geometry or closing other applications.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during periodic analysis task.");
                }
            });
        }

        private async Task UpdateSceneContextAsync()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return;

            var context = new SceneContext
            {
                ObjectCount = doc.Objects.Count,
                // In a real implementation, we would populate these fields with more detail
                ObjectTypes = new string[0],
                BoundingBox = new double[0],
                ActiveLayers = new string[0],
                CustomProperties = new Dictionary<string, object>()
            };

            await _mcpClient.SendSceneContextAsync(context);
        }

        /// <summary>
        /// Analyze a single Rhino object for potential issues
        /// </summary>
        private async Task<ObjectAnalysis> AnalyzeObjectAsync(RhinoObject rhinoObject)
        {
            return await Task.Run(() =>
            {
                var analysis = new ObjectAnalysis { ObjectId = rhinoObject.Id };
                var issues = new List<string>();

                if (rhinoObject.Geometry == null || !rhinoObject.Geometry.IsValid)
                {
                    issues.Add("Object has invalid geometry");
                }

                if (string.IsNullOrEmpty(rhinoObject.Name))
                {
                    issues.Add("Object is unnamed");
                }

                if (rhinoObject.Attributes.LayerIndex == -1)
                {
                    issues.Add("Object is not on a valid layer");
                }

                analysis.HasIssues = issues.Count > 0;
                analysis.Suggestions = issues;

                return analysis;
            });
        }

        /// <summary>
        /// Show multiple suggestions to the user
        /// </summary>
        private void ShowSuggestions(List<string> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                ShowSuggestion(suggestion);
            }
        }

        /// <summary>
        /// Show a single suggestion to the user
        /// </summary>
        private void ShowSuggestion(string suggestion)
        {
            RhinoApp.WriteLine($"[AI Assistant] {suggestion}");
            _logger?.LogInformation($"Suggestion: {suggestion}");
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
                StopMonitoring();
                _monitoringTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Results of object analysis
    /// </summary>
    public class ObjectAnalysis
    {
        public Guid ObjectId { get; set; }
        public bool HasIssues { get; set; }
        public List<string> Suggestions { get; set; } = new();
    }
}
