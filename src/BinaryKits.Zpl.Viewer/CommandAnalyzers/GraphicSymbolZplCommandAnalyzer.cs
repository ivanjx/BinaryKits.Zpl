using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class GraphicSymbolZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public GraphicSymbolZplCommandAnalyzer() : base("^GS") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);

            int tmpint;
            int height = virtualPrinter.FontHeight;
            int width = virtualPrinter.FontWidth;

            // An omitted or 0 dimension takes the ^CF value; when that is unset too it stays 0 and the drawer
            // derives it from the other dimension or the GS matrix
            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint) && tmpint > 0)
            {
                height = tmpint;
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint) && tmpint > 0)
            {
                width = tmpint;
            }

            //The field data are processing in the FieldDataZplCommandAnalyzer
            virtualPrinter.SetNextElementFieldData(new GraphicSymbolFieldData
            {
                FieldOrientation = fieldOrientation,
                Height = height,
                Width = width
            });

            return null;
        }
    }
}
