using System;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// A font stored in printer memory: <see cref="ZplDownloadedBitmapFont"/> (~DB, .FNT) or
    /// <see cref="ZplDownloadedOutlineFont"/> (~DY/~DU/~DT TrueType or OpenType).
    /// </summary>
    public abstract class ZplDownloadedFont : IDisposable
    {
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
