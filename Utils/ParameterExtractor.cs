using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Drawing;
using Rhino.Geometry;

namespace RhinoAI.Utils
{
    public static class ParameterExtractor
    {
        public static Dictionary<string, object> Extract(string input, List<string> expectedParams)
        {
            var parameters = new Dictionary<string, object>();
            var numbers = ExtractNumbers(input);
            var colors = ExtractColors(input);
            var names = ExtractNames(input);

            // This is a simplified logic and can be expanded
            if (expectedParams.Contains("center") && numbers.Count >= 3)
            {
                parameters["center"] = new Point3d(numbers[0], numbers[1], numbers[2]);
                numbers.RemoveRange(0, 3);
            }
            
            if (expectedParams.Contains("radius") && numbers.Any())
            {
                parameters["radius"] = numbers[0];
                numbers.RemoveAt(0);
            }
            
            if (expectedParams.Contains("height") && numbers.Any())
            {
                parameters["height"] = numbers[0];
                numbers.RemoveAt(0);
            }
            
            if (expectedParams.Contains("size") && numbers.Any())
            {
                if (numbers.Count >= 3)
                {
                    parameters["size"] = new Vector3d(numbers[0], numbers[1], numbers[2]);
                    numbers.RemoveRange(0, 3);
                }
                else
                {
                    var size = numbers[0];
                    parameters["size"] = new Vector3d(size, size, size);
                    numbers.RemoveAt(0);
                }
            }

            if (expectedParams.Contains("color") && colors.Any())
            {
                parameters["color"] = colors.First();
            }

            if (expectedParams.Contains("name") && names.Any())
            {
                parameters["name"] = names.First();
            }

            // For move operations
            if (expectedParams.Contains("translation") && numbers.Count >= 3)
            {
                parameters["translation"] = new Vector3d(numbers[0], numbers[1], numbers[2]);
                numbers.RemoveRange(0, 3);
            }

            // For scale operations
            if (expectedParams.Contains("scale") && numbers.Any())
            {
                if (numbers.Count >= 3)
                {
                    parameters["scale"] = new Vector3d(numbers[0], numbers[1], numbers[2]);
                    numbers.RemoveRange(0, 3);
                }
                else
                {
                    var scale = numbers[0];
                    parameters["scale"] = new Vector3d(scale, scale, scale);
                    numbers.RemoveAt(0);
                }
            }

            return parameters;
        }

        private static List<double> ExtractNumbers(string input)
        {
            var numbers = new List<double>();
            var matches = Regex.Matches(input, @"-?\d+(?:\.\d+)?");
            
            foreach (Match match in matches)
            {
                if (double.TryParse(match.Value, out double value))
                {
                    numbers.Add(value);
                }
            }
            
            return numbers;
        }

        private static List<Color> ExtractColors(string input)
        {
            var colors = new List<Color>();
            var lowerInput = input.ToLowerInvariant();
            
            var colorMap = new Dictionary<string, Color>
            {
                {"red", Color.Red},
                {"green", Color.Green},
                {"blue", Color.Blue},
                {"yellow", Color.Yellow},
                {"orange", Color.Orange},
                {"purple", Color.Purple},
                {"pink", Color.Pink},
                {"cyan", Color.Cyan},
                {"magenta", Color.Magenta},
                {"white", Color.White},
                {"black", Color.Black},
                {"gray", Color.Gray},
                {"grey", Color.Gray},
                {"brown", Color.Brown}
            };
            
            foreach (var colorName in colorMap.Keys)
            {
                if (lowerInput.Contains(colorName))
                {
                    colors.Add(colorMap[colorName]);
                }
            }
            
            return colors;
        }

        private static List<string> ExtractNames(string input)
        {
            var names = new List<string>();
            
            // Look for patterns like "named X" or "call it X" or "name it X"
            var namePatterns = new[]
            {
                @"named?\s+([a-zA-Z][a-zA-Z0-9_]*)",
                @"call\s+it\s+([a-zA-Z][a-zA-Z0-9_]*)",
                @"name\s+it\s+([a-zA-Z][a-zA-Z0-9_]*)",
                @"with\s+name\s+([a-zA-Z][a-zA-Z0-9_]*)"
            };

            foreach (var pattern in namePatterns)
            {
                var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        names.Add(match.Groups[1].Value);
                    }
                }
            }

            return names;
        }
    }
} 