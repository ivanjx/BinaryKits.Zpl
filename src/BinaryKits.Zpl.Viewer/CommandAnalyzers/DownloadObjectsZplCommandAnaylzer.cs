using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.DownloadedFonts;

using System;
using System.Globalization;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ~DY: download objects, <c>~DYd:f,b,x,t,w,data</c>. Formats <c>A</c> (hex, <c>:B64:</c>, <c>:Z64:</c>) and
    /// <c>P</c>; format <c>B</c> (raw binary) cannot survive text transport and is rejected. Extension code <c>T</c>
    /// with sfnt data becomes an outline font stored as <c>NAME.TTF</c>; other objects are stored as files.
    /// </summary>
    public class DownloadObjectsZplCommandAnaylzer : ZplCommandAnalyzerBase
    {
        public DownloadObjectsZplCommandAnaylzer() : base("~DY") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] parts = zplCommand.Substring(this.PrinterCommandPrefix.Length).Split(',', 6);
            if (parts.Length < 6)
            {
                throw new FormatException("~DY needs drive/name, format, extension, byte count, bytes per row and data.");
            }

            string format = parts[1].Trim().ToUpperInvariant();
            if (format != "A" && format != "P")
            {
                throw new FormatException($"~DY download format '{format}' is not supported; use A or P.");
            }

            string extensionCode = parts[2].Trim().ToUpperInvariant();
            if (extensionCode.Length == 0)
            {
                extensionCode = format == "P" ? "P" : "G";
            }

            if (!ZplDownloadData.TryParseObjectName(parts[0], null, out PrinterObjectName name))
            {
                throw new FormatException($"Invalid object name '{parts[0].Trim()}'.");
            }

            if (!int.TryParse(parts[3].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int byteCount))
            {
                throw new FormatException("Byte count must be a non-negative integer.");
            }

            _ = int.TryParse(parts[4].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int bytesPerRow);

            // ACS only exists for graphics (GRF rows)
            byte[] data = ZplDownloadData.Decode(parts[5], byteCount, bytesPerRow, allowAcs: extensionCode == "G" && format == "A", legacyZeros: false);

            if (extensionCode == "T")
            {
                // The stored extension comes from the extension code, not from the name
                string fileName = $"{StripExtension(name.Name)}.TTF";

                if (ZplDownloadedOutlineFont.HasSfntMagic(data))
                {
                    printerStorage.AddFont(name.Device, fileName, new ZplDownloadedOutlineFont(data));
                }
                else
                {
                    printerStorage.AddFile(name.Device, fileName, data);
                }

                return null;
            }

            // TODO: Handle case when .GRF data is downloaded using the ~DY command
            printerStorage.AddFile(name.Device, name.Name, data);

            return null;
        }

        private static string StripExtension(string fileName)
        {
            int index = fileName.LastIndexOf('.');
            return index > 0 ? fileName.Substring(0, index) : fileName;
        }
    }
}
