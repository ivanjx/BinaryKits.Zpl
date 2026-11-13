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

            // Both omitted: ^CF defaults; one given: the other stays 0 and is derived from the GS matrix
            int height = parsedHeight ?? (parsedWidth == null ? virtualPrinter.FontHeight : 0);
            int width = parsedWidth ?? (parsedHeight == null ? virtualPrinter.FontWidth : 0);

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
