using System;
using System.Collections.Generic;

using RhinoAI.Integration;

namespace RhinoAI.Core
{
    /// <summary>
    /// Manages the scene context for AI processing
    /// </summary>
    public class ContextManager
    {
        private readonly SimpleLogger _logger;
        private readonly Dictionary<string, SceneContext> _contextHistory;
        private SceneContext _currentContext;

        public ContextManager(SimpleLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contextHistory = new Dictionary<string, SceneContext>();
        }

        /// <summary>
        /// Update the current scene context
        /// </summary>
        public void UpdateContext(SceneContext newContext)
        {
            if (newContext == null)
            {
                _logger.LogWarning("Attempted to update with a null context.");
                return;
            }

            _currentContext = newContext;
            var contextId = Guid.NewGuid().ToString();
            _contextHistory[contextId] = newContext;

            _logger.LogInformation("Scene context updated. New context ID: {0}", contextId);
        }

        /// <summary>
        /// Get the current scene context
        /// </summary>
        public SceneContext GetCurrentContext()
        {
            return _currentContext;
        }

        /// <summary>
        /// Get a specific version of the scene context
        /// </summary>
        public SceneContext GetContextById(string contextId)
        {
            if (_contextHistory.TryGetValue(contextId, out var context))
            {
                return context;
            }

            _logger.LogWarning("Context ID not found: {0}", contextId);
            return null;
        }
    }
} 