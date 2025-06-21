using System;
using System.IO;
using Rhino;

namespace RhinoAI.Core
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical,
        None
    }

    /// <summary>
    /// Simple logger implementation for Rhino compatibility
    /// </summary>
    public class SimpleLogger
    {
        private readonly string _name;
        private readonly string _logFilePath;
        private readonly LogLevel _minimumLevel;
        private static readonly object _lock = new object();

        public SimpleLogger(string name, LogLevel minimumLevel = LogLevel.Information)
        {
            _name = name;
            _minimumLevel = minimumLevel;
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var logDir = Path.Combine(appDataPath, "RhinoAI", "Logs");
            Directory.CreateDirectory(logDir);
            _logFilePath = Path.Combine(logDir, $"rhinoai_{DateTime.Now:yyyyMMdd}.log");
        }

        private void Log(LogLevel level, string message)
        {
            if (level < _minimumLevel) return;

            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{_name}] {message}";

            // Write to Rhino command line for immediate feedback
            RhinoApp.WriteLine($"RhinoAI: {message}");

            // Write to log file
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback to debug output if file writing fails
                    System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }

#if DEBUG
            // Write to debug output as well in debug builds
            System.Diagnostics.Debug.WriteLine(logMessage);
#endif
        }

        private string FormatMessage(string template, params object[] args)
        {
            // Avoid formatting if no args are provided
            if (args == null || args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException ex)
            {
                var argsString = string.Join(", ", args);
                // Fallback to a safe format if the template is invalid
                return $"[FORMATTING ERROR] Template: '{template}', Args: '[{argsString}]', Error: {ex.Message}";
            }
        }

        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        
        public void LogDebug(string template, params object[] args) => Log(LogLevel.Debug, FormatMessage(template, args));
        
        public void LogInformation(string template, params object[] args) => Log(LogLevel.Information, FormatMessage(template, args));
        
        public void LogWarning(string template, params object[] args) => Log(LogLevel.Warning, FormatMessage(template, args));
        
        public void LogError(string message) => Log(LogLevel.Error, message);
        
        public void LogError(Exception ex, string message)
        {
            Log(LogLevel.Error, $"{message}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        
        public void LogCritical(string message) => Log(LogLevel.Critical, message);

        public void LogCritical(Exception ex, string message)
        {
            Log(LogLevel.Critical, $"{message}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }
} 