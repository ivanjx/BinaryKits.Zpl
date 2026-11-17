using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.DownloadedFonts;

using System;
using System.Globalization;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ~DU (unbounded TrueType, default extension <c>.FNT</c>) and ~DT (bounded TrueType, default extension
    /// <c>.DAT</c>): <c>~DUd:o.x,s,data</c>. Payloads with sfnt magic become outline fonts; other payloads (ZTools
    /// conversions) are stored as plain files and fall back when selected.
    /// </summary>
    public class DownloadUnboundedFontZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        private readonly string defaultExtension;

        /// <param name="prefix"><c>~DU</c> or <c>~DT</c></param>
        public DownloadUnboundedFontZplCommandAnalyzer(string prefix = "~DU") : base(prefix)
        {
            this.defaultExtension = prefix == "~DT" ? ".DAT" : ".FNT";
        }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] parts = zplCommand.Substring(this.PrinterCommandPrefix.Length).Split(',', 3);
            if (parts.Length < 3)
            {
                throw new FormatException($"{this.PrinterCommandPrefix} needs drive/name, byte count and data.");
            }

            if (!ZplDownloadData.TryParseObjectName(parts[0], this.defaultExtension, out PrinterObjectName name))
            {
                throw new FormatException($"Invalid font name '{parts[0].Trim()}'.");
            }

            if (!int.TryParse(parts[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int byteCount))
            {
                throw new FormatException("Byte count must be a non-negative integer.");
            }

            byte[] data = ZplDownloadData.Decode(parts[2], byteCount, 0, allowAcs: false, legacyZeros: false);

            if (ZplDownloadedOutlineFont.HasSfntMagic(data))
            {
                printerStorage.AddFont(name.Device, name.Name, new ZplDownloadedOutlineFont(data));
            }
            else
            {
                printerStorage.AddFile(name.Device, name.Name, data);
            }

            return null;
        }
    }
}
