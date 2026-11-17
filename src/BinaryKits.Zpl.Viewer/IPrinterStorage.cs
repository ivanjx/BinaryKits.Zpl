using BinaryKits.Zpl.Viewer.DownloadedFonts;

using System.Collections.Generic;

namespace BinaryKits.Zpl.Viewer
{
    /// <summary>
    /// Printer memory. Names are matched case-insensitively on device and exact <c>NAME.EXT</c>; a file and a font
    /// never share a key, writing one removes the other.
    /// </summary>
    public interface IPrinterStorage
    {
        /// <summary>
        /// Stores or replaces a file.
        /// </summary>
        void AddFile(char storageDevice, string fileName, byte[] data);

        byte[] GetFile(char storageDevice, string fileName);

        /// <summary>
        /// Stores or replaces a downloaded font. With <paramref name="ownsFont"/> the storage disposes
        /// <paramref name="font"/> when it is replaced or the storage is disposed; without it the caller keeps
        /// ownership, so one font can be shared by several storages (for example from a cache).
        /// </summary>
        void AddFont(char storageDevice, string fileName, ZplDownloadedFont font, bool ownsFont = true);

        bool TryGetFont(char storageDevice, string fileName, out ZplDownloadedFont font);

        /// <summary>
        /// ^FL: links or unlinks <paramref name="extensionFont"/> to <paramref name="baseFont"/>. Both are
        /// <c>D:NAME.EXT</c> keys; <paramref name="baseFont"/> may contain <c>*</c>/<c>?</c> wildcards. The last 5
        /// links per base are kept.
        /// </summary>
        void SetFontLink(string extensionFont, string baseFont, bool enabled);

        /// <summary>
        /// Extension font keys linked to <paramref name="baseFont"/> in lookup order: links of the exact base first,
        /// then links of matching wildcard bases, each in link order. Keys are not checked against stored fonts.
        /// </summary>
        IReadOnlyList<string> GetFontLinks(string baseFont);
    }
}
