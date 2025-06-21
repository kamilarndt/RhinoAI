using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.Commands;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Basic diagnostics command for troubleshooting RhinoAI plugin issues
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class RhinoAIDiagnosticsCommand : Command
    {
        public override string EnglishName => "RhinoAIDiagnostics";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                RhinoApp.WriteLine("🔍 Starting RhinoAI Diagnostics...");
                RhinoApp.WriteLine();

                var diagnostics = RunBasicDiagnostics();
                DisplayResults(diagnostics);

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error running diagnostics: {ex.Message}");
                return Result.Failure;
            }
        }

        private List<DiagnosticResult> RunBasicDiagnostics()
        {
            var results = new List<DiagnosticResult>();

            // Basic plugin checks
            results.Add(CheckPluginLoaded());
            results.Add(CheckRhinoVersion());
            results.Add(CheckFileSystemAccess());
            results.Add(CheckMemoryUsage());
            results.Add(CheckNetworkAccess());
            results.Add(CheckDependencies());

            return results;
        }

        private DiagnosticResult CheckPluginLoaded()
        {
            try
            {
                return new DiagnosticResult
                {
                    Name = "Plugin Loading",
                    Status = DiagnosticStatus.Pass,
                    Message = "Plugin loaded successfully",
                    Details = new List<string>
                    {
                        $"Assembly Location: {System.Reflection.Assembly.GetExecutingAssembly().Location}",
                        $"Plugin ID: {RhinoAIPlugin.Instance?.Id ?? Guid.Empty}"
                    }
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Name = "Plugin Loading",
                    Status = DiagnosticStatus.Fail,
                    Message = $"Plugin loading failed: {ex.Message}",
                    Recommendations = new List<string> { "Reinstall the plugin", "Check plugin dependencies" }
                };
            }
        }

        private DiagnosticResult CheckRhinoVersion()
        {
            try
            {
                var version = RhinoApp.Version;
                
                if (version.Major >= 8)
                {
                    return new DiagnosticResult
                    {
                        Name = "Rhino Version",
                        Status = DiagnosticStatus.Pass,
                        Message = $"Rhino version is compatible ({version})",
                        Details = new List<string>
                        {
                            $"Version: {version}",
                            $"Build Date: {RhinoApp.BuildDate}",
                            $"Service Release: {version.Build}"
                        }
                    };
                }
                else
                {
                    return new DiagnosticResult
                    {
                        Name = "Rhino Version",
                        Status = DiagnosticStatus.Fail,
                        Message = $"Unsupported Rhino version ({version})",
                        Recommendations = new List<string> { "Upgrade to Rhino 8 or later" }
                    };
                }
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Name = "Rhino Version",
                    Status = DiagnosticStatus.Fail,
                    Message = $"Version check failed: {ex.Message}"
                };
            }
        }

        private DiagnosticResult CheckFileSystemAccess()
        {
            var testPaths = new[]
            {
                Path.GetTempPath(),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            };

            var accessiblePaths = new List<string>();
            var issues = new List<string>();

            foreach (var path in testPaths)
            {
                try
                {
                    var testFile = Path.Combine(path, $"rhinoai_test_{Guid.NewGuid()}.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    accessiblePaths.Add(path);
                }
                catch (Exception ex)
                {
                    issues.Add($"{path}: {ex.Message}");
                }
            }

            if (issues.Count == 0)
            {
                return new DiagnosticResult
                {
                    Name = "File System Access",
                    Status = DiagnosticStatus.Pass,
                    Message = "All file system access tests passed",
                    Details = accessiblePaths.ConvertAll(p => $"✓ {p}")
                };
            }
            else
            {
                return new DiagnosticResult
                {
                    Name = "File System Access",
                    Status = issues.Count == testPaths.Length ? DiagnosticStatus.Fail : DiagnosticStatus.Warning,
                    Message = $"File system access issues detected ({issues.Count}/{testPaths.Length} failed)",
                    Details = issues.ConvertAll(i => $"❌ {i}"),
                    Recommendations = new List<string> { "Run Rhino as administrator", "Check folder permissions" }
                };
            }
        }

        private DiagnosticResult CheckMemoryUsage()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var totalMemory = GC.GetTotalMemory(false);
                var workingSet = Environment.WorkingSet;
                var memoryMB = totalMemory / 1024 / 1024;
                var workingSetMB = workingSet / 1024 / 1024;

                var details = new List<string>
                {
                    $"Managed Memory: {memoryMB:N0} MB",
                    $"Working Set: {workingSetMB:N0} MB",
                    $"GC Gen 0: {GC.CollectionCount(0)} collections",
                    $"GC Gen 1: {GC.CollectionCount(1)} collections",
                    $"GC Gen 2: {GC.CollectionCount(2)} collections"
                };

                if (memoryMB > 1000)
                {
                    return new DiagnosticResult
                    {
                        Name = "Memory Usage",
                        Status = DiagnosticStatus.Fail,
                        Message = $"Critical memory usage ({memoryMB:N0} MB)",
                        Details = details,
                        Recommendations = new List<string> { "Restart Rhino immediately", "Check for memory leaks" }
                    };
                }
                else if (memoryMB > 500)
                {
                    return new DiagnosticResult
                    {
                        Name = "Memory Usage",
                        Status = DiagnosticStatus.Warning,
                        Message = $"High memory usage ({memoryMB:N0} MB)",
                        Details = details,
                        Recommendations = new List<string> { "Monitor memory usage", "Consider restarting Rhino" }
                    };
                }
                else
                {
                    return new DiagnosticResult
                    {
                        Name = "Memory Usage",
                        Status = DiagnosticStatus.Pass,
                        Message = $"Memory usage is normal ({memoryMB:N0} MB)",
                        Details = details
                    };
                }
            }
            catch (Exception ex)
            {
                return new DiagnosticResult
                {
                    Name = "Memory Usage",
                    Status = DiagnosticStatus.Fail,
                    Message = $"Memory check failed: {ex.Message}"
                };
            }
        }

        private DiagnosticResult CheckNetworkAccess()
        {
            var testUrls = new[]
            {
                "https://www.google.com",
                "https://api.openai.com",
                "https://api.anthropic.com"
            };

            var reachable = 0;
            var details = new List<string>();

            foreach (var url in testUrls)
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = client.GetAsync(url).Result;
                    reachable++;
                    details.Add($"✓ {url} - Reachable");
                }
                catch
                {
                    details.Add($"❌ {url} - Not reachable");
                }
            }

            if (reachable == 0)
            {
                return new DiagnosticResult
                {
                    Name = "Network Connectivity",
                    Status = DiagnosticStatus.Fail,
                    Message = "No network connectivity detected",
                    Details = details,
                    Recommendations = new List<string> { "Check internet connection", "Check firewall settings" }
                };
            }
            else if (reachable < testUrls.Length)
            {
                return new DiagnosticResult
                {
                    Name = "Network Connectivity",
                    Status = DiagnosticStatus.Warning,
                    Message = $"Limited connectivity ({reachable}/{testUrls.Length} services reachable)",
                    Details = details,
                    Recommendations = new List<string> { "Check specific service endpoints", "Verify API credentials" }
                };
            }
            else
            {
                return new DiagnosticResult
                {
                    Name = "Network Connectivity",
                    Status = DiagnosticStatus.Pass,
                    Message = "Network connectivity is good",
                    Details = details
                };
            }
        }

        private DiagnosticResult CheckDependencies()
        {
            var requiredAssemblies = new[]
            {
                "RhinoCommon",
                "System.Text.Json",
                "Microsoft.Extensions.Logging"
            };

            var loadedAssemblies = new HashSet<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                loadedAssemblies.Add(assembly.GetName().Name);
            }

            var missing = new List<string>();
            var found = new List<string>();

            foreach (var dependency in requiredAssemblies)
            {
                if (loadedAssemblies.Any(a => a.StartsWith(dependency)))
                {
                    found.Add(dependency);
                }
                else
                {
                    missing.Add(dependency);
                }
            }

            if (missing.Count == 0)
            {
                return new DiagnosticResult
                {
                    Name = "Dependencies",
                    Status = DiagnosticStatus.Pass,
                    Message = "All required dependencies are loaded",
                    Details = found.ConvertAll(dep => $"✓ {dep}")
                };
            }
            else
            {
                return new DiagnosticResult
                {
                    Name = "Dependencies",
                    Status = DiagnosticStatus.Fail,
                    Message = $"Missing dependencies: {string.Join(", ", missing)}",
                    Details = missing.ConvertAll(dep => $"❌ {dep}"),
                    Recommendations = new List<string> { "Reinstall the plugin", "Check NuGet package references" }
                };
            }
        }

        private void DisplayResults(List<DiagnosticResult> results)
        {
            RhinoApp.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            RhinoApp.WriteLine("║                   DIAGNOSTIC RESULTS                        ║");
            RhinoApp.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            RhinoApp.WriteLine();

            var passCount = 0;
            var warnCount = 0;
            var failCount = 0;

            foreach (var result in results)
            {
                var statusIcon = result.Status switch
                {
                    DiagnosticStatus.Pass => "✅",
                    DiagnosticStatus.Warning => "⚠️",
                    DiagnosticStatus.Fail => "❌",
                    _ => "❓"
                };

                switch (result.Status)
                {
                    case DiagnosticStatus.Pass: passCount++; break;
                    case DiagnosticStatus.Warning: warnCount++; break;
                    case DiagnosticStatus.Fail: failCount++; break;
                }

                RhinoApp.WriteLine($"{statusIcon} {result.Name}: {result.Message}");
                
                if (result.Details?.Count > 0)
                {
                    foreach (var detail in result.Details)
                    {
                        RhinoApp.WriteLine($"    {detail}");
                    }
                }
                
                if (result.Recommendations?.Count > 0)
                {
                    RhinoApp.WriteLine("    Recommendations:");
                    foreach (var recommendation in result.Recommendations)
                    {
                        RhinoApp.WriteLine($"    • {recommendation}");
                    }
                }
                
                RhinoApp.WriteLine();
            }

            // Summary
            var overallHealth = (double)passCount / results.Count * 100;
            var healthIcon = overallHealth >= 90 ? "💚" : overallHealth >= 70 ? "💛" : "❤️";
            
            RhinoApp.WriteLine($"{healthIcon} Overall Health: {overallHealth:F1}%");
            RhinoApp.WriteLine($"Summary: {passCount} passed, {warnCount} warnings, {failCount} failed");
            RhinoApp.WriteLine($"Diagnostic completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
    }

    public class DiagnosticResult
    {
        public string Name { get; set; } = string.Empty;
        public DiagnosticStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public enum DiagnosticStatus
    {
        Pass,
        Warning,
        Fail
    }
} 