using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using RhinoAI.Core;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Single command to process natural language input
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class RhinoAISingleCommand : Command
    {
        public override string EnglishName => "RhinoAISingle";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager == null)
                {
                    RhinoApp.WriteLine("❌ Error: RhinoAI plugin not properly initialized");
                    return Result.Failure;
                }

                RhinoApp.WriteLine("🎯 RhinoAI Single Command Mode");
                RhinoApp.WriteLine("Enter a complete natural language command:");
                RhinoApp.WriteLine("Examples:");
                RhinoApp.WriteLine("  - \"Create a sphere with radius 5\"");
                RhinoApp.WriteLine("  - \"Make a box 10x10x10 and move it up 5 units\"");
                RhinoApp.WriteLine("  - \"Generate 5 cylinders in a row\"");
                RhinoApp.WriteLine("  - \"Create a torus with major radius 8 and minor radius 2\"");
                RhinoApp.WriteLine("  - \"Make an array of 3x3 spheres with radius 1\"");

                var getString = new GetString();
                getString.SetCommandPrompt("Enter complete natural language command");
                getString.AcceptNothing(false);
                
                // Use GetLiteralString() to allow spaces in the input
                var result = getString.GetLiteralString();
                if (result != Rhino.Input.GetResult.String)
                {
                    RhinoApp.WriteLine("❌ Command cancelled");
                    return Result.Cancel;
                }

                var command = getString.StringResult();
                if (string.IsNullOrWhiteSpace(command))
                {
                    RhinoApp.WriteLine("❌ Empty command");
                    return Result.Nothing;
                }

                // Execute the command synchronously for better user experience
                ExecuteCommand(command, plugin.AIManager);

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error: {ex.Message}");
                return Result.Failure;
            }
        }

        private void ExecuteCommand(string command, AIManager aiManager)
        {
            try
            {
                RhinoApp.WriteLine($"\n🔄 Processing: \"{command}\"");
                var startTime = DateTime.Now;
                
                // Use synchronous processing to avoid threading issues
                var task = aiManager.ProcessNaturalLanguageAsync(command);
                task.Wait(); // Wait for completion
                var commandResult = task.Result;
                
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;
                
                RhinoApp.WriteLine($"✅ Result ({duration:F0}ms): {commandResult}");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error processing command: {ex.Message}");
            }
        }
    }
} 