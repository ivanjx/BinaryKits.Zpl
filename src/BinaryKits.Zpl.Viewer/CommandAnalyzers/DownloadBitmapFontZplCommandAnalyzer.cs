using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.DownloadedFonts;

using System;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ~DB: download bitmap font (<c>.FNT</c>).
    /// </summary>
    public class DownloadBitmapFontZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public DownloadBitmapFontZplCommandAnalyzer() : base("~DB") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            if (!ZplBitmapFontParser.TryParse(zplCommand, out PrinterObjectName name, out ZplDownloadedBitmapFont font, out string error))
            {
                throw new FormatException($"Invalid ~DB font: {error}");
            }

            printerStorage.AddFont(name.Device, name.Name, font);

            return null;
        }
    }
}
