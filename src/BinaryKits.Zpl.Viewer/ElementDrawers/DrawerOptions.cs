using BinaryKits.Zpl.Viewer.BitmapFonts;

using SkiaSharp;

using System;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class DrawerOptions
    {
        [Obsolete("Use FontManager.FontLoader instead.")]
        public Func<string, SKTypeface> FontLoader
        {
            get => this.FontManager.FontLoader;
            set => this.FontManager.FontLoader = value;
        }

        /// <summary>
        /// Gets or sets the image format used when rendering output.
        /// </summary>
        public SKEncodedImageFormat RenderFormat { get; set; } = SKEncodedImageFormat.Png;

        /// <summary>
        /// Gets or sets the quality level used when rendering images in formats that support lossy compression.
        /// </summary>
        public int RenderQuality { get; set; } = 80;

        /// <summary>
        /// Applies label over a white background after rendering all elements
        /// </summary>
        public bool OpaqueBackground { get; set; } = false;

        /// <summary>
        /// Gets or sets the solid background color used when <see cref="OpaqueBackground"/> is enabled.
        /// </summary>
        public SKColor BackgroundColor { get; set; } = SKColors.White;

        public bool ReplaceDashWithEnDash { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether underscores in text should be replaced with en space.
        /// </summary>
        public bool ReplaceUnderscoreWithEnSpace { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether antialiasing is enabled.
        /// </summary>
        public bool Antialias { get; set; } = true;

        /// <summary>
        /// Gets or sets the text rendering path. Outline rendering is the default for compatibility.
        /// </summary>
        public ZplTextRenderingMode TextRenderingMode { get; set; } = ZplTextRenderingMode.Outline;

        /// <summary>
        /// Gets or sets the externally supplied bitmap font provider used by strict bitmap text rendering.
        /// </summary>
        public IZplBitmapFontProvider BitmapFontProvider { get; set; } = EmptyZplBitmapFontProvider.Instance;

        public FontManager FontManager { get; private set; }

        public DrawerOptions() : this(new FontManager()) { }

        public DrawerOptions(FontManager fontManager)
        {
            this.FontManager = fontManager;
        }
    }
}
