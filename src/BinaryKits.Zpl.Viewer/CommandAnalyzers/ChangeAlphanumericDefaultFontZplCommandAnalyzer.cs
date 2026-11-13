using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class ChangeAlphanumericDefaultFontZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public ChangeAlphanumericDefaultFontZplCommandAnalyzer() : base("^CF") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            int tmpint;
            int fontHeight = 0;
            int fontWidth = 0;
            bool hasDimension = false;

            if (zplDataParts.Length > 0)
            {
                string newFont = zplDataParts[0];
                if (!string.IsNullOrEmpty(newFont))
                {
                    virtualPrinter.SetFontName(newFont);
                }
            }

            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint))
            {
                fontHeight = tmpint;
                hasDimension = true;
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint))
            {
                fontWidth = tmpint;
                hasDimension = true;
            }

            // Omitted h and w keep the last accepted values; a single given dimension leaves the other at 0
            // so the drawers derive it proportionally from the font matrix.
            if (hasDimension)
            {
                virtualPrinter.SetFontHeight(fontHeight);
                virtualPrinter.SetFontWidth(fontWidth);
            }

            return null;
        }
    }
}
