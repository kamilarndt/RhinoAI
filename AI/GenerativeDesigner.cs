using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rhino.Geometry;
using RhinoAI.Core;
using RhinoAI.Models;

namespace RhinoAI.AI
{
    /// <summary>
    /// Handles generative design and AI-powered geometry creation
    /// </summary>
    public class GenerativeDesigner : IDisposable
    {
        private readonly ConfigurationManager _config;
        private readonly SimpleLogger _logger;
        private bool _disposed = false;

        public GenerativeDesigner(ConfigurationManager config, SimpleLogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generate design variants based on a prompt
        /// </summary>
        public async Task<IEnumerable<GeometryBase>> GenerateVariantsAsync(
            DesignPrompt prompt, 
            int variantCount = 5)
        {
            try
            {
                _logger?.LogInformation($"Generating {variantCount} design variants for prompt: {prompt.Text}");

                var variants = new List<GeometryBase>();

                for (int i = 0; i < variantCount; i++)
                {
                    var variant = await GenerateSingleVariantAsync(prompt, i);
                    if (variant != null)
                    {
                        variants.Add(variant);
                    }
                }

                _logger?.LogInformation($"Generated {variants.Count} design variants");
                return variants;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate design variants");
                throw;
            }
        }

        /// <summary>
        /// Generate a single design variant
        /// </summary>
        private async Task<GeometryBase?> GenerateSingleVariantAsync(DesignPrompt prompt, int index)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // This is a placeholder implementation
                    // In real implementation, would use AI to generate actual geometry
                    var type = prompt.Parameters.GetValueOrDefault("type", "sphere").ToString().ToLower();

                    switch (type)
                    {
                        case "sphere":
                            return CreateSphereVariant(prompt, index);
                        case "box":
                            return CreateBoxVariant(prompt, index);
                        case "cylinder":
                            return CreateCylinderVariant(prompt, index);
                        default:
                            return CreateDefaultVariant(prompt, index);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Failed to generate variant {index}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Create a sphere variant
        /// </summary>
        private GeometryBase CreateSphereVariant(DesignPrompt prompt, int index)
        {
            var center = prompt.ContextGeometry.OfType<Rhino.Geometry.Point>().FirstOrDefault()?.Location ?? Point3d.Origin;
            var baseRadius = Convert.ToDouble(prompt.Parameters.GetValueOrDefault("radius", 1.0));
            var variation = 1.0 + (index * 0.2); // Vary size by 20% per variant
            
            var sphere = new Sphere(center, baseRadius * variation);
            return sphere.ToBrep();
        }

        /// <summary>
        /// Create a box variant
        /// </summary>
        private GeometryBase CreateBoxVariant(DesignPrompt prompt, int index)
        {
            var center = prompt.ContextGeometry.OfType<Rhino.Geometry.Point>().FirstOrDefault()?.Location ?? Point3d.Origin;
            var baseSize = Convert.ToDouble(prompt.Parameters.GetValueOrDefault("size", 1.0));
            var variation = 1.0 + (index * 0.15);

            var size = baseSize * variation;
            var box = new Box(
                new Plane(center, Vector3d.ZAxis),
                new Interval(-size/2, size/2),
                new Interval(-size/2, size/2),
                new Interval(0, size)
            );

            return box.ToBrep();
        }

        /// <summary>
        /// Create a cylinder variant
        /// </summary>
        private GeometryBase CreateCylinderVariant(DesignPrompt prompt, int index)
        {
            var center = prompt.ContextGeometry.OfType<Rhino.Geometry.Point>().FirstOrDefault()?.Location ?? Point3d.Origin;
            var baseRadius = Convert.ToDouble(prompt.Parameters.GetValueOrDefault("radius", 1.0));
            var baseHeight = Convert.ToDouble(prompt.Parameters.GetValueOrDefault("height", 2.0));
            var variation = 1.0 + (index * 0.25);

            var cylinder = new Cylinder(
                new Circle(new Plane(center, Vector3d.ZAxis), baseRadius * variation),
                baseHeight * variation
            );

            return cylinder.ToBrep(true, true);
        }

        /// <summary>
        /// Create a default variant
        /// </summary>
        private GeometryBase CreateDefaultVariant(DesignPrompt prompt, int index)
        {
            // Default to sphere if type not recognized
            return CreateSphereVariant(prompt, index);
        }

        /// <summary>
        /// Optimize existing geometry using AI
        /// </summary>
        public async Task<GeometryBase?> OptimizeGeometryAsync(GeometryBase geometry, OptimizationGoals goals)
        {
            try
            {
                _logger?.LogInformation($"Optimizing geometry for goals: {string.Join(", ", goals.Goals)}");

                return await Task.Run(() =>
                {
                    // Placeholder optimization logic
                    // In real implementation, would use AI to optimize geometry
                    
                    if (geometry is Brep brep)
                    {
                        // Simple optimization: rebuild surfaces
                        return brep.DuplicateBrep();
                    }
                    else if (geometry is Mesh mesh)
                    {
                        // Simple optimization: reduce mesh
                        var optimized = mesh.DuplicateMesh();
                        optimized.Reduce(mesh.Faces.Count / 2, false, 10, false);
                        return optimized;
                    }

                    return geometry.Duplicate();
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Geometry optimization failed");
                return null;
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
                // Cleanup resources
                _disposed = true;
            }
        }
    }
} 