using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using RhinoAI.Core;

namespace RhinoAI.Commands
{
    [CommandStyle(Style.ScriptRunner)]
    public class RhinoAITestCommand : Command
    {
        public override string EnglishName => "RhinoAITest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                RhinoApp.WriteLine("🚀 Starting RhinoAI Complex Geometry Tests");
                
                var plugin = RhinoAIPlugin.Instance;
                if (plugin?.AIManager == null)
                {
                    RhinoApp.WriteLine("❌ Error: RhinoAI plugin not properly initialized");
                    return Result.Failure;
                }

                // Run tests
                Task.Run(async () => await RunComplexTests(plugin.AIManager));
                
                RhinoApp.WriteLine("✅ Test execution started. Check the command line for progress.");
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error starting tests: {ex.Message}");
                return Result.Failure;
            }
        }

        private async Task RunComplexTests(AIManager aiManager)
        {
            var testCommands = new[]
            {
                "Create a sphere with radius 10 units at the origin",
                "Make a box that is 20 units wide, 15 units deep, and 8 units tall",
                "Generate a cylinder with radius 5 and height 12",
                "Create a torus with major radius 15 and minor radius 3",
                "Make a cone with base radius 8, top radius 2, and height 20",
                "Generate an ellipsoid with radii 10, 6, and 4 units",
                "Create two overlapping spheres with radius 8, then subtract the second from the first",
                "Make a box 15x15x15 and a cylinder radius 6 height 20, then intersect them",
                "Create a rectangular array of 3x2 spheres, each with radius 2, spaced 6 units apart",
                "Make a linear array of 5 boxes along the X axis, each box is 2x2x2 units"
            };

            RhinoApp.WriteLine("\n🧪 RHINOAI COMPLEX GEOMETRY TEST SUITE");
            
            foreach (var command in testCommands)
            {
                await ExecuteAndReportTest(command, aiManager);
                await Task.Delay(2000); // 2 second pause between tests
            }
            
            RhinoApp.WriteLine("\n🏁 ALL TESTS COMPLETED");
        }

        private async Task ExecuteAndReportTest(string command, AIManager aiManager)
        {
            RhinoApp.WriteLine($"\n🔄 Testing: {command}");
            
            try
            {
                var startTime = DateTime.Now;
                var result = await aiManager.ProcessNaturalLanguageAsync(command);
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;

                RhinoApp.WriteLine($"✅ SUCCESS ({duration:F0}ms): {result}");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ FAILED: {ex.Message}");
            }
        }
    }
} 