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
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint))
            {
                fontWidth = tmpint;
            }

            // Omitted h and w are stored as 0 (unset) instead of keeping the last ^CF values; ^A/^GS then derive
            // them from the other dimension or the font matrix (Labelary: ^CF0,30 ^CF0 ^A0N is the minimum size)
            virtualPrinter.SetFontHeight(fontHeight);
            virtualPrinter.SetFontWidth(fontWidth);

            return null;
        }
    }
}
