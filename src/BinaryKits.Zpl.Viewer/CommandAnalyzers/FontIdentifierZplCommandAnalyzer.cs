using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ^CWa,d:o.x Font Identifier: assigns the letter or digit <c>a</c> to a downloaded font. The alias applies to ^A
    /// and ^CF and lasts until the printer is reset, so it survives ^XA/^XZ; the last assignment wins.
    /// </summary>
    public class FontIdentifierZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public FontIdentifierZplCommandAnalyzer() : base("^CW") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            if (zplDataParts.Length < 2)
            {
                return null;
            }

            string identifier = zplDataParts[0].Trim().ToUpperInvariant();
            if (identifier.Length != 1 || !IsFontIdentifier(identifier[0]))
            {
                return null;
            }

            if (!PrinterObjectName.TryParse(zplDataParts[1], null, out PrinterObjectName name))
            {
                return null;
            }

            virtualPrinter.SetFontAlias(identifier[0], name.Key);

            return null;
        }

        private static bool IsFontIdentifier(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }
    }
}
