using System;
using System.IO;

using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using RhinoAI.Core;
using RhinoAI.UI.Panels;
using RhinoAI.Properties;

[assembly: Rhino.PlugIns.PlugInDescription(DescriptionType.Address, "RhinoAI")]
//[assembly: Rhino.PlugIns.PlugInDescription(DescriptionType.Assembly, "1.0.0")]

namespace RhinoAI
{
    /// <summary>
    /// Main plugin class for RhinoAI - AI-powered assistant for Rhino 3D
    /// </summary>
    public class RhinoAIPlugin : PlugIn
    {
        public static RhinoAIPlugin Instance { get; private set; }

        public AIManager AIManager { get; private set; }
        public ConfigurationManager ConfigManager { get; private set; }
        public SimpleLogger Logger { get; private set; }

        public RhinoAIPlugin()
        {
            Instance = this;
        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                // Initialize logger first
                Logger = new SimpleLogger(nameof(RhinoAIPlugin));
                Logger.LogInformation("Initializing RhinoAI Plugin...");

                // Initialize Configuration Manager
                ConfigManager = new ConfigurationManager(Logger);

                // Initialize AI Manager
                AIManager = new AIManager(ConfigManager, Logger);
                if (!AIManager.InitializeAll())
                {
                    errorMessage = "Failed to initialize AI components. Check logs for details.";
                    Logger.LogCritical(errorMessage);
                    return LoadReturnCode.ErrorShowDialog;
                }

                // Initialize UI components
                InitializeUI();
                Logger.LogInformation("UI components initialized successfully");

                Logger.LogInformation("RhinoAI Plugin initialization complete");
                RhinoApp.WriteLine("RhinoAI Plugin loaded successfully!");
                
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = $"A critical error occurred during RhinoAI Plugin startup: {ex.Message}";
                Logger?.LogError(ex, "RhinoAIPlugin.OnLoad failed");
                return LoadReturnCode.ErrorShowDialog;
            }
        }

        protected override void OnShutdown()
        {
            try
            {
                CleanupPlugin();
                RhinoApp.WriteLine("RhinoAI Plugin shutdown complete.");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during plugin shutdown: {ex.Message}");
                Logger?.LogError(ex, "Plugin shutdown error");
            }
            
            base.OnShutdown();
        }

        /// <summary>
        /// Initialize UI components
        /// </summary>
        private void InitializeUI()
        {
            try
            {
                // Register the panel
                Panels.RegisterPanel(this, typeof(UI.Panels.AIControlPanel), "RhinoAI Assistant", Resources.panel_icon);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to initialize UI components");
                throw;
            }
        }

        /// <summary>
        /// Cleanup plugin resources
        /// </summary>
        private void CleanupPlugin()
        {
            Logger?.LogInformation("Cleaning up RhinoAI Plugin...");

            AIManager?.Dispose();
        }

        /// <summary>
        /// Get the plugin's data directory
        /// </summary>
        public string GetDataDirectory()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RhinoAI"
            );

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Show the AI Control Panel
        /// </summary>
        public void ShowAIControlPanel()
        {
            var panelId = typeof(UI.Panels.AIControlPanel).GUID;
            Panels.OpenPanel(panelId);
        }
    }
} 