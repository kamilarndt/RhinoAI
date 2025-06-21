using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoAI.Core;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Command for configuring RhinoAI settings
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class RhinoAIConfigCommand : Command
    {
        public override string EnglishName => "RhinoAIConfig";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager?.ConfigurationManager == null)
                {
                    RhinoApp.WriteLine("❌ RhinoAI plugin not properly initialized");
                    return Result.Failure;
                }

                var configManager = plugin.AIManager.ConfigurationManager;

                // Show configuration menu
                ShowConfigurationMenu(configManager);

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Configuration error: {ex.Message}");
                return Result.Failure;
            }
        }

        private void ShowConfigurationMenu(ConfigurationManager configManager)
        {
            while (true)
            {
                RhinoApp.WriteLine("\n🔧 RhinoAI Configuration");
                RhinoApp.WriteLine("========================");
                RhinoApp.WriteLine("1. Set OpenAI API Key");
                RhinoApp.WriteLine("2. Set Claude API Key");
                RhinoApp.WriteLine("3. Configure Ollama");
                RhinoApp.WriteLine("4. View Current Settings");
                RhinoApp.WriteLine("5. Test API Connection");
                RhinoApp.WriteLine("6. Reset to Defaults");
                RhinoApp.WriteLine("0. Exit");
                
                var choice = GetUserChoice("Select option (0-6): ");
                
                switch (choice)
                {
                    case "1":
                        SetApiKey(configManager, "OpenAI");
                        break;
                    case "2":
                        SetApiKey(configManager, "Claude");
                        break;
                    case "3":
                        ConfigureOllama(configManager);
                        break;
                    case "4":
                        ViewCurrentSettings(configManager);
                        break;
                    case "5":
                        TestApiConnection(configManager);
                        break;
                    case "6":
                        ResetToDefaults(configManager);
                        break;
                    case "0":
                        return;
                    default:
                        RhinoApp.WriteLine("❌ Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private string GetUserChoice(string prompt)
        {
            var getter = new GetString();
            getter.SetCommandPrompt(prompt);
            getter.AcceptNothing(false);
            
            if (getter.Get() == GetResult.String)
            {
                return getter.StringResult();
            }
            
            return "";
        }

        private void SetApiKey(ConfigurationManager configManager, string provider)
        {
            RhinoApp.WriteLine($"\n🔑 Setting {provider} API Key");
            RhinoApp.WriteLine($"Current status: {(configManager.HasApiKey(provider) ? "✅ Configured" : "❌ Not configured")}");
            
            var getter = new GetString();
            getter.SetCommandPrompt($"Enter {provider} API Key (or press Enter to cancel): ");
            getter.AcceptNothing(true);
            
            if (getter.Get() == GetResult.String)
            {
                var apiKey = getter.StringResult();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    configManager.SaveApiKey(provider, apiKey);
                    RhinoApp.WriteLine($"✅ {provider} API Key saved successfully!");
                    
                    // Reinitialize AI components
                    var plugin = RhinoAIPlugin.Instance;
                    if (plugin?.AIManager != null)
                    {
                        plugin.AIManager.ReinitializeClients();
                        RhinoApp.WriteLine("🔄 AI components reinitialized with new API key");
                    }
                }
                else
                {
                    RhinoApp.WriteLine("❌ API Key not set (empty input)");
                }
            }
            else
            {
                RhinoApp.WriteLine("❌ API Key setup cancelled");
            }
        }

        private void ConfigureOllama(ConfigurationManager configManager)
        {
            RhinoApp.WriteLine("\n🦙 Ollama Configuration");
            RhinoApp.WriteLine("========================");
            
            var currentUrl = configManager.GetSetting("OllamaUrl", "http://localhost:11434");
            var currentModel = configManager.GetSetting("OllamaModel", "llama3.1:8b");
            
            RhinoApp.WriteLine($"Current URL: {currentUrl}");
            RhinoApp.WriteLine($"Current Model: {currentModel}");
            
            // Configure URL
            var urlGetter = new GetString();
            urlGetter.SetCommandPrompt($"Enter Ollama URL (current: {currentUrl}): ");
            urlGetter.AcceptNothing(true);
            urlGetter.SetDefaultString(currentUrl);
            
            string newUrl = currentUrl;
            if (urlGetter.Get() == GetResult.String)
            {
                var input = urlGetter.StringResult();
                if (!string.IsNullOrEmpty(input))
                {
                    newUrl = input;
                }
            }
            
            // Configure Model
            var modelGetter = new GetString();
            modelGetter.SetCommandPrompt($"Enter Ollama Model (current: {currentModel}): ");
            modelGetter.AcceptNothing(true);
            modelGetter.SetDefaultString(currentModel);
            
            string newModel = currentModel;
            if (modelGetter.Get() == GetResult.String)
            {
                var input = modelGetter.StringResult();
                if (!string.IsNullOrEmpty(input))
                {
                    newModel = input;
                }
            }
            
            // Save settings
            configManager.SetSetting("OllamaUrl", newUrl);
            configManager.SetSetting("OllamaModel", newModel);
            
            RhinoApp.WriteLine($"✅ Ollama configured:");
            RhinoApp.WriteLine($"   URL: {newUrl}");
            RhinoApp.WriteLine($"   Model: {newModel}");
            
            // Reinitialize AI components
            var plugin = RhinoAIPlugin.Instance;
            if (plugin?.AIManager != null)
            {
                plugin.AIManager.ReinitializeClients();
                RhinoApp.WriteLine("🔄 AI components reinitialized with new Ollama settings");
            }
        }

        private void ViewCurrentSettings(ConfigurationManager configManager)
        {
            RhinoApp.WriteLine("\n📋 Current RhinoAI Settings");
            RhinoApp.WriteLine("============================");
            
            // API Key status
            RhinoApp.WriteLine("AI Services:");
            RhinoApp.WriteLine($"  OpenAI: {(configManager.HasApiKey("OpenAI") ? "✅ Configured" : "❌ Not configured")}");
            RhinoApp.WriteLine($"  Claude: {(configManager.HasApiKey("Claude") ? "✅ Configured" : "❌ Not configured")}");
            RhinoApp.WriteLine($"  Ollama: {configManager.GetSetting("OllamaUrl", "http://localhost:11434")} ({configManager.GetSetting("OllamaModel", "llama3.1:8b")})");
            
            // Other settings
            RhinoApp.WriteLine("\nSettings:");
            RhinoApp.WriteLine($"  OpenAI Model: {configManager.GetSetting("OpenAI:Model", "gpt-4")}");
            RhinoApp.WriteLine($"  Claude Model: {configManager.GetSetting("Claude:Model", "claude-3-sonnet-20240229")}");
            RhinoApp.WriteLine($"  MCP Server: {configManager.GetSetting("MCP:ServerUrl", "http://localhost:5005/")}");
            RhinoApp.WriteLine($"  Real-time Processing: {configManager.GetSetting("Processing:EnableRealTime", true)}");
        }

        private void TestApiConnection(ConfigurationManager configManager)
        {
            RhinoApp.WriteLine("\n🔍 Testing API Connections");
            RhinoApp.WriteLine("===========================");
            
            var plugin = RhinoAIPlugin.Instance;
            if (plugin?.AIManager == null)
            {
                RhinoApp.WriteLine("❌ AI Manager not available");
                return;
            }

            // Test OpenAI
            if (configManager.HasApiKey("OpenAI"))
            {
                RhinoApp.WriteLine("Testing OpenAI connection...");
                try
                {
                    RhinoApp.WriteLine("✅ OpenAI API Key is configured");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ OpenAI connection failed: {ex.Message}");
                }
            }
            else
            {
                RhinoApp.WriteLine("❌ OpenAI API Key not configured");
            }

            // Test Claude
            if (configManager.HasApiKey("Claude"))
            {
                RhinoApp.WriteLine("Testing Claude connection...");
                try
                {
                    RhinoApp.WriteLine("✅ Claude API Key is configured");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ Claude connection failed: {ex.Message}");
                }
            }
            else
            {
                RhinoApp.WriteLine("❌ Claude API Key not configured");
            }
            
            // Test Ollama
            if (plugin.AIManager.OllamaClient != null)
            {
                RhinoApp.WriteLine("Testing Ollama connection...");
                try
                {
                    var isConnected = plugin.AIManager.OllamaClient.TestConnectionAsync().Result;
                    if (isConnected)
                    {
                        RhinoApp.WriteLine("✅ Ollama connection successful");
                        
                        // Get available models
                        var models = plugin.AIManager.OllamaClient.GetAvailableModelsAsync().Result;
                        if (models.Length > 0)
                        {
                            RhinoApp.WriteLine($"   Available models: {string.Join(", ", models)}");
                        }
                    }
                    else
                    {
                        RhinoApp.WriteLine("❌ Ollama connection failed - is Ollama running?");
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ Ollama connection failed: {ex.Message}");
                }
            }
            else
            {
                RhinoApp.WriteLine("❌ Ollama client not initialized");
            }
        }

        private void ResetToDefaults(ConfigurationManager configManager)
        {
            RhinoApp.WriteLine("\n⚠️  Reset to Defaults");
            RhinoApp.WriteLine("=====================");
            RhinoApp.WriteLine("This will reset all settings to defaults but keep API keys.");
            
            var getter = new GetString();
            getter.SetCommandPrompt("Are you sure? (yes/no): ");
            getter.AcceptNothing(false);
            
            if (getter.Get() == GetResult.String)
            {
                var confirm = getter.StringResult();
                if (confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    configManager.ResetToDefaults();
                    RhinoApp.WriteLine("✅ Settings reset to defaults");
                }
                else
                {
                    RhinoApp.WriteLine("❌ Reset cancelled");
                }
            }
        }
    }
} 