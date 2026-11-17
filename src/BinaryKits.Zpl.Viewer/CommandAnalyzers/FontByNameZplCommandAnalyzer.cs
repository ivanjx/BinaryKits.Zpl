using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ^A@o,h,w,d:f.x Use Font Name to Call Font. The name carries over to later ^A@ commands of the same label
    /// without a name; without a name the ^CF font is used. The ^CF font letter is the fallback of a named font.
    /// </summary>
    public class FontByNameZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public FontByNameZplCommandAnalyzer() : base("^A@") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);

            int tmpint;
            int fontHeight = virtualPrinter.FontHeight;
            int fontWidth = virtualPrinter.FontWidth;

            // Same as ^A: an omitted or 0 dimension takes the ^CF value, else it stays 0 for the drawers to derive
            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint) && tmpint > 0)
            {
                fontHeight = tmpint;
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint) && tmpint > 0)
            {
                fontWidth = tmpint;
            }

            string fontFileName;
            string rawName = zplDataParts.Length > 3 ? zplDataParts[3] : null;

            if (string.IsNullOrWhiteSpace(rawName))
            {
                fontFileName = virtualPrinter.FontByNameFileName ?? virtualPrinter.FontFileName;
            }
            else if (PrinterObjectName.TryParse(rawName, null, out PrinterObjectName name))
            {
                fontFileName = name.Key;
                virtualPrinter.SetFontByNameFileName(fontFileName);
            }
            else
            {
                // An invalid name prints with the ^CF font
                fontFileName = virtualPrinter.FontFileName;
            }

            virtualPrinter.SetNextFont(virtualPrinter.FontName, fieldOrientation, fontWidth, fontHeight, fontFileName);

            return null;
        }
    }
}
