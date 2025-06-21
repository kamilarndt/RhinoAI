using System;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Commands;
using RhinoAI.Core;
using RhinoAI.Development;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Command to display basic status information about the RhinoAI plugin
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class RhinoAIStatusCommand : Command
    {
        public override string EnglishName => "RhinoAIStatus";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var status = GenerateBasicStatusReport();
                
                // Display in Rhino command line
                foreach (var line in status.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        RhinoApp.WriteLine(line);
                    }
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error generating status report: {ex.Message}");
                return Result.Failure;
            }
        }

        private string GenerateBasicStatusReport()
        {
            var report = new StringBuilder();
            
            report.AppendLine("🤖 ═══════════════════════════════════════════════");
            report.AppendLine("🤖 RhinoAI Plugin Status Report");
            report.AppendLine("🤖 ═══════════════════════════════════════════════");
            report.AppendLine();
            
            // Plugin Information
            report.AppendLine("📋 Plugin Information:");
            report.AppendLine("   • Plugin Name: RhinoAI");
            report.AppendLine("   • Version: 1.0.0");
            report.AppendLine("   • Status: Active");
            report.AppendLine($"   • Rhino Version: {RhinoApp.Version}");
            report.AppendLine();
            
            // System Information
            report.AppendLine("💻 System Information:");
            report.AppendLine($"   • Operating System: {Environment.OSVersion}");
            report.AppendLine($"   • .NET Version: {Environment.Version}");
            report.AppendLine($"   • Process ID: {Environment.ProcessId}");
            report.AppendLine($"   • Working Set: {Environment.WorkingSet / 1024 / 1024:F1} MB");
            report.AppendLine();
            
            // Document Information  
            var currentDoc = RhinoDoc.ActiveDoc;
            if (currentDoc != null)
            {
                report.AppendLine("📄 Current Document:");
                report.AppendLine($"   • Document Name: {(string.IsNullOrEmpty(currentDoc.Name) ? "Untitled" : currentDoc.Name)}");
                report.AppendLine($"   • Objects Count: {currentDoc.Objects.Count}");
                report.AppendLine($"   • Layers Count: {currentDoc.Layers.Count}");
                report.AppendLine($"   • Views Count: {currentDoc.Views.Count}");
                report.AppendLine();
            }
            
            // Available Commands
            report.AppendLine("⚡ Available Commands:");
            report.AppendLine("   • RhinoAI - Main AI assistant command");
            report.AppendLine("   • RhinoAITest - Run plugin tests");
            report.AppendLine("   • RhinoAIStatus - Display this status");
            report.AppendLine("   • RhinoAIDiagnostics - Run diagnostics");
            report.AppendLine();
            
            // Quick Health Check
            report.AppendLine("🏥 Quick Health Check:");
            var healthStatus = PerformQuickHealthCheck();
            foreach (var status in healthStatus)
            {
                report.AppendLine($"   • {status}");
            }
            
            report.AppendLine();
            report.AppendLine("🤖 ═══════════════════════════════════════════════");
            
            return report.ToString();
        }

        private string[] PerformQuickHealthCheck()
        {
            var results = new System.Collections.Generic.List<string>();
            
            try
            {
                // Check if plugin is loaded
                results.Add("✅ Plugin loaded successfully");
                
                // Check Rhino integration
                if (RhinoApp.Version != null)
                {
                    results.Add("✅ Rhino integration working");
                }
                else
                {
                    results.Add("❌ Rhino integration issue");
                }
                
                // Check file system access
                var tempPath = System.IO.Path.GetTempPath();
                if (System.IO.Directory.Exists(tempPath))
                {
                    results.Add("✅ File system access working");
                }
                else
                {
                    results.Add("❌ File system access issue");
                }
                
                // Check memory usage
                var memoryMB = Environment.WorkingSet / 1024 / 1024;
                if (memoryMB < 500)
                {
                    results.Add($"✅ Memory usage normal ({memoryMB:F1} MB)");
                }
                else
                {
                    results.Add($"⚠️ High memory usage ({memoryMB:F1} MB)");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ Health check error: {ex.Message}");
            }
            
            return results.ToArray();
        }
    }
} 