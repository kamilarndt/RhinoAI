using System.Drawing;
using System.Reflection;

namespace RhinoAI.Properties
{
    /// <summary>
    /// Resource accessor for embedded resources
    /// </summary>
    internal static class Resources
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        /// <summary>
        /// Default panel icon
        /// </summary>
        public static Icon panel_icon
        {
            get
            {
                try
                {
                    // Try to load embedded icon resource
                    using var stream = _assembly.GetManifestResourceStream("RhinoAI.Properties.panel_icon.ico");
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
                catch
                {
                    // Fall back to default icon if loading fails
                }

                // Return default system icon as fallback
                return SystemIcons.Application;
            }
        }

        /// <summary>
        /// Plugin icon
        /// </summary>
        public static Icon plugin_icon
        {
            get
            {
                try
                {
                    using var stream = _assembly.GetManifestResourceStream("RhinoAI.Properties.plugin_icon.ico");
                    if (stream != null)
                    {
                        return new Icon(stream);
                    }
                }
                catch
                {
                    // Fall back to default icon if loading fails
                }

                return SystemIcons.Application;
            }
        }

        /// <summary>
        /// Command icon
        /// </summary>
        public static Bitmap command_icon
        {
            get
            {
                try
                {
                    using var stream = _assembly.GetManifestResourceStream("RhinoAI.Properties.command_icon.png");
                    if (stream != null)
                    {
                        return new Bitmap(stream);
                    }
                }
                catch
                {
                    // Fall back to default bitmap if loading fails
                }

                // Create simple default bitmap
                var bitmap = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.FillRectangle(Brushes.Blue, 0, 0, 16, 16);
                    g.DrawString("AI", SystemFonts.DefaultFont, Brushes.White, 2, 2);
                }
                return bitmap;
            }
        }
    }
} 