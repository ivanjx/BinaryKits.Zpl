using SkiaSharp;

using System;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// TrueType or OpenType (CFF) font downloaded with ~DY/~DU/~DT, or seeded from a font file.
    /// </summary>
    public sealed class ZplDownloadedOutlineFont : ZplDownloadedFont
    {
        private readonly object sync = new();
        private SKTypeface typeface;
        private bool typefaceLoaded;
        private bool disposed;

        public ZplDownloadedOutlineFont(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (!HasSfntMagic(data))
            {
                throw new ArgumentException("Data is not a TrueType or OpenType font.", nameof(data));
            }

            this.Data = data;
        }

        /// <summary>
        /// The raw sfnt bytes.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Typeface created on first use with <see cref="SKTypeface.FromData(SKData, int)"/>; <c>null</c> when Skia
        /// cannot load the data. Owned by this font, never registered in <see cref="FontManager"/>.
        /// </summary>
        public SKTypeface Typeface
        {
            get
            {
                lock (this.sync)
                {
                    ObjectDisposedException.ThrowIf(this.disposed, this);

                    return this.LoadTypeface();
                }
            }
        }

        /// <summary>
        /// Gets <see cref="Typeface"/>; <c>false</c> when this font is disposed or Skia cannot load the data.
        /// </summary>
        public bool TryGetTypeface(out SKTypeface typeface)
        {
            lock (this.sync)
            {
                typeface = this.disposed ? null : this.LoadTypeface();
                return typeface != null;
            }
        }

        private SKTypeface LoadTypeface()
        {
            if (!this.typefaceLoaded)
            {
                using SKData skData = SKData.CreateCopy(this.Data);
                this.typeface = SKTypeface.FromData(skData);
                this.typefaceLoaded = true;
            }

            return this.typeface;
        }

        /// <summary>
        /// sfnt magic: <c>00 01 00 00</c> or <c>true</c> (TrueType outlines), <c>OTTO</c> (CFF).
        /// </summary>
        public static bool HasSfntMagic(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
            {
                return false;
            }

            ReadOnlySpan<byte> magic = data[..4];
            return magic.SequenceEqual((ReadOnlySpan<byte>)[0x00, 0x01, 0x00, 0x00])
                || magic.SequenceEqual("true"u8)
                || magic.SequenceEqual("OTTO"u8);
        }

        public override void Dispose()
        {
            lock (this.sync)
            {
                if (this.disposed)
                {
                    return;
                }

                this.disposed = true;
                this.typeface?.Dispose();
                this.typeface = null;
            }

            base.Dispose();
        }
    }
}
