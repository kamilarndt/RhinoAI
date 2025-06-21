using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using RhinoAI.Core;

namespace RhinoAI.Commands
{
    /// <summary>
    /// Interactive command to test custom natural language input
    /// </summary>
    [CommandStyle(Style.ScriptRunner)]
    public class RhinoAIInteractiveCommand : Command
    {
        public override string EnglishName => "RhinoAIInteractive";

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

                RhinoApp.WriteLine("🎮 RhinoAI Interactive Mode");
                RhinoApp.WriteLine("Enter natural language commands to create geometry.");
                RhinoApp.WriteLine("Examples:");
                RhinoApp.WriteLine("  - 'Create a sphere with radius 5'");
                RhinoApp.WriteLine("  - 'Make a box 10x10x10 and move it up 5 units'");
                RhinoApp.WriteLine("  - 'Generate 5 cylinders in a row'");
                RhinoApp.WriteLine("  - 'Create a torus with major radius 8 and minor radius 2'");
                RhinoApp.WriteLine("  - 'Make an array of 3x3 spheres with radius 1'");

                while (true)
                {
                    // Use StringBox to completely bypass Rhino's command interpretation
                    string command = "";
                    bool result = Rhino.UI.Dialogs.ShowEditBox(
                        "RhinoAI Interactive", 
                        "Enter natural language command (or 'exit' to quit):", 
                        "", 
                        false,
                        out command);
                    
                    if (!result || string.IsNullOrWhiteSpace(command) || command.ToLower() == "exit")
                        break;

                    // Execute the command
                    ExecuteCommandAsync(command, plugin.AIManager);
                }

                RhinoApp.WriteLine("🏁 Interactive mode ended");
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error: {ex.Message}");
                return Result.Failure;
            }
        }

        private void ExecuteCommandAsync(string command, AIManager aiManager)
        {
            Task.Run(async () =>
            {
                try
                {
                    RhinoApp.WriteLine($"\n🔄 Processing: {command}");
                    var startTime = DateTime.Now;
                    
                    var commandResult = await aiManager.ProcessNaturalLanguageAsync(command);
                    
                    var endTime = DateTime.Now;
                    var duration = (endTime - startTime).TotalMilliseconds;
                    
                    RhinoApp.WriteLine($"✅ Result ({duration:F0}ms): {commandResult}");
                    RhinoApp.WriteLine("Ready for next command...\n");
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ Error: {ex.Message}");
                    RhinoApp.WriteLine("Ready for next command...\n");
                }
            });
        }
    }
} 