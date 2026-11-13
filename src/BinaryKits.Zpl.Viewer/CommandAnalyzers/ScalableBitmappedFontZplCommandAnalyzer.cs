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
            int fontHeight = virtualPrinter.FontHeight;
            int fontWidth = virtualPrinter.FontWidth;

            // An omitted or 0 dimension takes the ^CF value; when that is unset too it stays 0 and the drawers
            // derive it from the other dimension or the font matrix
            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint) && tmpint > 0)
            {
                fontHeight = tmpint;
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint) && tmpint > 0)
            {
                fontWidth = tmpint;
            }

            virtualPrinter.SetNextFont(fontName, fieldOrientation, fontWidth, fontHeight);

            return null;
        }
    }
}
