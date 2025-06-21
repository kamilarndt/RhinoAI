using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace RhinoAI.Core
{
    /// <summary>
    /// Manages plugin configuration and settings
    /// </summary>
    public class ConfigurationManager : IDisposable
    {
        private readonly SimpleLogger _logger;
        private readonly string _configPath;
        private readonly string _encryptedConfigPath;
        private IConfiguration _configuration;
        private readonly Dictionary<string, object> _settings;
        private bool _disposed = false;

        public ConfigurationManager(SimpleLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var dataDirectory = GetDataDirectory();
            _configPath = Path.Combine(dataDirectory, "config.json");
            _encryptedConfigPath = Path.Combine(dataDirectory, "config.encrypted");
            _settings = new Dictionary<string, object>();
            
            InitializeConfiguration();
        }

        /// <summary>
        /// Get the plugin data directory
        /// </summary>
        private string GetDataDirectory()
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
        /// Initialize configuration
        /// </summary>
        private void InitializeConfiguration()
        {
            var builder = new ConfigurationBuilder();

            // Add default settings
            var defaultSettings = GetDefaultSettings();
            builder.AddInMemoryCollection(defaultSettings);

            // Add configuration file if it exists
            if (File.Exists(_configPath))
            {
                builder.AddJsonFile(_configPath, optional: true, reloadOnChange: false);
            }

            _configuration = builder.Build();
        }

        /// <summary>
        /// Get default configuration settings
        /// </summary>
        private Dictionary<string, string> GetDefaultSettings()
        {
            return new Dictionary<string, string>
            {
                // OpenAI Settings
                ["OpenAI:Model"] = "gpt-4",
                ["OpenAI:MaxTokens"] = "4000",
                ["OpenAI:Temperature"] = "0.7",
                
                // Claude Settings  
                ["Claude:Model"] = "claude-3-sonnet-20240229",
                ["Claude:MaxTokens"] = "4000",
                
                // MCP Settings
                ["MCP:ServerUrl"] = "http://localhost:5005/",
                ["MCP:Port"] = "5005",
                ["MCP:Enabled"] = "true",
                
                // UI Settings
                ["UI:Theme"] = "Auto",
                ["UI:ShowWelcome"] = "true",
                ["UI:AutoSaveSettings"] = "true",
                
                // Processing Settings
                ["Processing:EnableRealTime"] = "true",
                ["Processing:CacheResults"] = "true",
                ["Processing:MaxCacheSize"] = "100",
                
                // Security Settings
                ["Security:EncryptApiKeys"] = "true",
                ["Security:LogSensitiveData"] = "false"
            };
        }

        /// <summary>
        /// Load configuration from files
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                // Load encrypted settings (API keys, etc.)
                LoadEncryptedSettings();
                
                // Merge with regular configuration
                InitializeConfiguration();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - use defaults
                _logger.LogWarning("Failed to load configuration: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Save configuration to files
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                // Save regular settings to JSON
                var regularSettings = new Dictionary<string, object>();
                
                foreach (var kvp in _settings)
                {
                    if (!IsSensitiveSetting(kvp.Key))
                    {
                        regularSettings[kvp.Key] = kvp.Value;
                    }
                }

                var json = JsonSerializer.Serialize(regularSettings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(_configPath, json);

                // Save encrypted settings
                SaveEncryptedSettings();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load encrypted settings from file
        /// </summary>
        private void LoadEncryptedSettings()
        {
            if (!File.Exists(_encryptedConfigPath))
                return;

            try
            {
                var encryptedData = File.ReadAllBytes(_encryptedConfigPath);
                var decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    null,
                    DataProtectionScope.CurrentUser
                );

                var json = Encoding.UTF8.GetString(decryptedData);
                var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log warning but don't throw
                _logger.LogWarning("Failed to load encrypted settings: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Save encrypted settings to file
        /// </summary>
        private void SaveEncryptedSettings()
        {
            var sensitiveSettings = new Dictionary<string, object>();
            
            foreach (var kvp in _settings)
            {
                if (IsSensitiveSetting(kvp.Key))
                {
                    sensitiveSettings[kvp.Key] = kvp.Value;
                }
            }

            if (sensitiveSettings.Count == 0)
                return;

            var json = JsonSerializer.Serialize(sensitiveSettings);
            var dataToEncrypt = Encoding.UTF8.GetBytes(json);
            
            var encryptedData = ProtectedData.Protect(
                dataToEncrypt,
                null,
                DataProtectionScope.CurrentUser
            );

            File.WriteAllBytes(_encryptedConfigPath, encryptedData);
        }

        /// <summary>
        /// Check if a setting key contains sensitive data
        /// </summary>
        private bool IsSensitiveSetting(string key)
        {
            var sensitiveKeys = new[] { "apikey", "token", "secret", "password", "key" };
            var lowerKey = key.ToLowerInvariant();
            
            foreach (var sensitiveKey in sensitiveKeys)
            {
                if (lowerKey.Contains(sensitiveKey))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Get a setting value as string
        /// </summary>
        public string GetSetting(string key, string defaultValue = "")
        {
            // Check in-memory settings first
            if (_settings.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }

            // Check configuration
            return _configuration?[key] ?? defaultValue;
        }

        /// <summary>
        /// Get a setting value as integer
        /// </summary>
        public int GetSetting(string key, int defaultValue)
        {
            var stringValue = GetSetting(key);
            return int.TryParse(stringValue, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Get a setting value as boolean
        /// </summary>
        public bool GetSetting(string key, bool defaultValue)
        {
            var stringValue = GetSetting(key);
            return bool.TryParse(stringValue, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Get a setting value as double
        /// </summary>
        public double GetSetting(string key, double defaultValue)
        {
            var stringValue = GetSetting(key);
            return double.TryParse(stringValue, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Set a setting value
        /// </summary>
        public void SetSetting(string key, object value)
        {
            _settings[key] = value;
            
            // Auto-save if enabled
            if (GetSetting("UI:AutoSaveSettings", true))
            {
                SaveConfiguration();
            }
        }

        /// <summary>
        /// Save API key securely
        /// </summary>
        public void SaveApiKey(string provider, string apiKey)
        {
            if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(apiKey))
                return;

            var key = $"{provider}:ApiKey";
            SetSetting(key, apiKey);
            SaveConfiguration();
        }

        /// <summary>
        /// Get API key
        /// </summary>
        public string GetApiKey(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return "";

            var key = $"{provider}:ApiKey";
            return GetSetting(key);
        }

        /// <summary>
        /// Get decrypted API key for specified provider
        /// </summary>
        public string GetDecryptedApiKey(string provider)
        {
            return GetApiKey(provider);
        }

        /// <summary>
        /// Get the underlying IConfiguration instance
        /// </summary>
        public IConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Check if an API key is configured for a provider
        /// </summary>
        public bool HasApiKey(string provider)
        {
            return !string.IsNullOrEmpty(GetApiKey(provider));
        }

        /// <summary>
        /// Remove API key for a provider
        /// </summary>
        public void RemoveApiKey(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return;

            var key = $"{provider}:ApiKey";
            _settings.Remove(key);
            SaveConfiguration();
        }

        /// <summary>
        /// Get all settings (excluding sensitive data)
        /// </summary>
        public Dictionary<string, object> GetAllSettings()
        {
            var result = new Dictionary<string, object>();
            
            // Add configuration settings
            foreach (var section in _configuration.GetChildren())
            {
                foreach (var item in section.GetChildren())
                {
                    var key = $"{section.Key}:{item.Key}";
                    if (!IsSensitiveSetting(key))
                    {
                        result[key] = item.Value;
                    }
                }
            }

            // Add non-sensitive in-memory settings
            foreach (var kvp in _settings)
            {
                if (!IsSensitiveSetting(kvp.Key))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            _settings.Clear();
            
            // Keep API keys if they exist
            var providers = new[] { "OpenAI", "Claude" };
            var apiKeys = new Dictionary<string, string>();
            
            foreach (var provider in providers)
            {
                var apiKey = GetApiKey(provider);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    apiKeys[provider] = apiKey;
                }
            }

            InitializeConfiguration();

            // Restore API keys
            foreach (var kvp in apiKeys)
            {
                SaveApiKey(kvp.Key, kvp.Value);
            }
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
                SaveConfiguration();
                _disposed = true;
            }
        }
    }
} 