using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class ScalableBitmappedFontZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public ScalableBitmappedFontZplCommandAnalyzer() : base("^A") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string fontName = zplCommand[this.PrinterCommandPrefix.Length].ToString();

            string[] zplDataParts = this.SplitCommand(zplCommand, 1);

            FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);

            int tmpint;
            int? parsedHeight = null;
            int? parsedWidth = null;

            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint))
            {
                parsedHeight = tmpint;
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint))
            {
                parsedWidth = tmpint;
            }

            // Both omitted: ^CF defaults; one given: the other stays 0 and is derived from the font matrix
            int fontHeight = parsedHeight ?? (parsedWidth == null ? virtualPrinter.FontHeight : 0);
            int fontWidth = parsedWidth ?? (parsedHeight == null ? virtualPrinter.FontWidth : 0);

            virtualPrinter.SetNextFont(fontName, fieldOrientation, fontWidth, fontHeight);

            return null;
        }
    }
}
