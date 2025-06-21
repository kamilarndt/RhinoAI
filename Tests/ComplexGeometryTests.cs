using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using RhinoAI.Core;
using RhinoAI.AI;

namespace RhinoAI.Tests
{
    /// <summary>
    /// Complex geometry test commands for RhinoAI
    /// </summary>
    public static class ComplexGeometryTests
    {
        /// <summary>
        /// Test commands for complex object creation
        /// </summary>
        public static readonly List<string> TestCommands = new List<string>
        {
            // Basic geometry with parameters
            "Create a sphere with radius 10 units at the origin",
            "Make a box that is 20 units wide, 15 units deep, and 8 units tall",
            "Generate a cylinder with radius 5 and height 12",
            
            // Complex single objects
            "Create a torus with major radius 15 and minor radius 3",
            "Make a cone with base radius 8, top radius 2, and height 20",
            "Generate an ellipsoid with radii 10, 6, and 4 units",
            
            // Multi-step operations
            "Create a sphere with radius 5, then move it 10 units in the X direction",
            "Make a box 10x10x10, then rotate it 45 degrees around the Z axis",
            "Generate a cylinder radius 3 height 8, then copy it 5 times along the Y axis with 12 unit spacing",
            
            // Boolean operations
            "Create two overlapping spheres with radius 8, then subtract the second from the first",
            "Make a box 15x15x15 and a cylinder radius 6 height 20, then intersect them",
            "Generate a large sphere radius 12 and three smaller spheres radius 3, then subtract all small spheres from the large one",
            
            // Arrays and patterns
            "Create a rectangular array of 5x3 spheres, each with radius 2, spaced 6 units apart",
            "Make a circular array of 8 cylinders around the origin, each cylinder has radius 1 and height 5",
            "Generate a linear array of 10 boxes along the X axis, each box is 2x2x2 units",
            
            // Complex surfaces
            "Create a NURBS surface from 4 corner points: (0,0,0), (20,0,5), (20,15,8), (0,15,3)",
            "Make a lofted surface between a circle at Z=0 and a square at Z=10",
            "Generate a revolved surface by rotating a curve around the Z axis",
            
            // Architectural elements
            "Create a spiral staircase with 20 steps, radius 5, and total height 30",
            "Make a dome structure with radius 25 and height 15",
            "Generate a parametric tower with 10 floors, each floor 3 units high and slightly smaller than the one below",
            
            // Organic shapes
            "Create a tree-like structure with a trunk and 5 main branches",
            "Make a shell-like surface that spirals outward",
            "Generate a wave pattern surface with amplitude 3 and wavelength 8",
            
            // Mechanical parts
            "Create a gear with 24 teeth, outer radius 15, and inner radius 8",
            "Make a spring coil with 10 turns, radius 4, and pitch 2",
            "Generate a threaded bolt with length 30, diameter 8, and thread pitch 1.5",
            
            // Complex assemblies
            "Create a chair with four legs, a seat, and a backrest",
            "Make a table with a rectangular top 120x80 units and four cylindrical legs",
            "Generate a simple house structure with walls, roof, windows, and a door",
            
            // Mathematical objects
            "Create a Klein bottle approximation",
            "Make a Möbius strip with width 4 and radius 8",
            "Generate a fractal tree structure with 4 levels of branching",
            
            // Precision operations
            "Create a sphere exactly at coordinates (25.5, -12.3, 8.7) with radius 3.14159",
            "Make a box from point (0,0,0) to point (15.75, 23.25, 11.5)",
            "Generate a cylinder centered at (10, 20, 5) with precise radius 4.321 and height 9.876"
        };

        /// <summary>
        /// Execute a test command and return the result
        /// </summary>
        public static async Task<string> ExecuteTestCommand(string command, AIManager aiManager)
        {
            try
            {
                var result = await aiManager.ProcessNaturalLanguageAsync(command);
                return $"✅ SUCCESS: {command}\n   Result: {result}";
            }
            catch (Exception ex)
            {
                return $"❌ FAILED: {command}\n   Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Run all test commands
        /// </summary>
        public static async Task<List<string>> RunAllTests(AIManager aiManager)
        {
            var results = new List<string>();
            
            RhinoApp.WriteLine("🧪 Starting RhinoAI Complex Geometry Tests...");
            RhinoApp.WriteLine($"📋 Total test commands: {TestCommands.Count}");
            
            for (int i = 0; i < TestCommands.Count; i++)
            {
                var command = TestCommands[i];
                RhinoApp.WriteLine($"🔄 Test {i + 1}/{TestCommands.Count}: {command}");
                
                var result = await ExecuteTestCommand(command, aiManager);
                results.Add(result);
                
                // Brief pause between tests
                await Task.Delay(500);
            }
            
            // Summary
            var successCount = results.Count(r => r.StartsWith("✅"));
            var failCount = results.Count(r => r.StartsWith("❌"));
            
            RhinoApp.WriteLine($"\n📊 Test Results Summary:");
            RhinoApp.WriteLine($"   ✅ Successful: {successCount}");
            RhinoApp.WriteLine($"   ❌ Failed: {failCount}");
            RhinoApp.WriteLine($"   📈 Success Rate: {(double)successCount / TestCommands.Count * 100:F1}%");
            
            return results;
        }

        /// <summary>
        /// Run a specific category of tests
        /// </summary>
        public static async Task<List<string>> RunCategoryTests(string category, AIManager aiManager)
        {
            var categoryCommands = GetCommandsByCategory(category);
            var results = new List<string>();
            
            RhinoApp.WriteLine($"🧪 Running {category} tests...");
            
            foreach (var command in categoryCommands)
            {
                var result = await ExecuteTestCommand(command, aiManager);
                results.Add(result);
                RhinoApp.WriteLine(result);
                await Task.Delay(300);
            }
            
            return results;
        }

        /// <summary>
        /// Get commands by category
        /// </summary>
        private static List<string> GetCommandsByCategory(string category)
        {
            return category.ToLower() switch
            {
                "basic" => TestCommands.Take(6).ToList(),
                "boolean" => TestCommands.Skip(9).Take(3).ToList(),
                "arrays" => TestCommands.Skip(12).Take(3).ToList(),
                "surfaces" => TestCommands.Skip(15).Take(3).ToList(),
                "architectural" => TestCommands.Skip(18).Take(3).ToList(),
                "organic" => TestCommands.Skip(21).Take(3).ToList(),
                "mechanical" => TestCommands.Skip(24).Take(3).ToList(),
                "assemblies" => TestCommands.Skip(27).Take(3).ToList(),
                "mathematical" => TestCommands.Skip(30).Take(3).ToList(),
                "precision" => TestCommands.Skip(33).Take(3).ToList(),
                _ => TestCommands.Take(5).ToList()
            };
        }

        /// <summary>
        /// Interactive test mode - user can input custom commands
        /// </summary>
        public static async Task RunInteractiveTest(AIManager aiManager)
        {
            RhinoApp.WriteLine("🎮 Interactive Test Mode Started");
            RhinoApp.WriteLine("Type natural language commands to create geometry.");
            RhinoApp.WriteLine("Type 'exit' to stop, 'help' for examples.");
            
            while (true)
            {
                RhinoApp.WriteLine("\n💬 Enter command:");
                
                // Note: In a real implementation, you'd need to get user input
                // This is a placeholder for the interactive functionality
                break;
            }
        }
    }
} 