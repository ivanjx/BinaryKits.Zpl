using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ^FLext,base,link Font Linking: <c>link</c> 1 links the extension font to the base font(s), 0 unlinks it, any
    /// other value is ignored. The base may be a <c>*</c>/<c>?</c> wildcard; the extension does not accept wildcards.
    /// </summary>
    public class FontLinkZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public FontLinkZplCommandAnalyzer() : base("^FL") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            if (zplDataParts.Length < 3)
            {
                return null;
            }

            string link = zplDataParts[2].Trim();
            if (link != "0" && link != "1")
            {
                return null;
            }

            if (!PrinterObjectName.TryParse(zplDataParts[0], null, out PrinterObjectName extension)
                || extension.Name.IndexOfAny(['*', '?']) >= 0)
            {
                return null;
            }

            if (!PrinterObjectName.TryParse(zplDataParts[1], null, out PrinterObjectName baseFont))
            {
                return null;
            }

            printerStorage.SetFontLink(extension.Key, baseFont.Key, link == "1");

            return null;
        }
    }
}
