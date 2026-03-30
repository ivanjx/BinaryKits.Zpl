using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class Industrial2of5BarcodeZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public Industrial2of5BarcodeZplCommandAnalyzer() : base("^BI") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);

            int height = virtualPrinter.BarcodeInfo.Height;
            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out int tmpint))
            {
                height = tmpint;
            }

            bool printInterpretationLine = true;
            if (zplDataParts.Length > 2)
            {
                printInterpretationLine = this.ConvertBoolean(zplDataParts[2], "Y");
            }

            bool printInterpretationLineAboveCode = false;
            if (zplDataParts.Length > 3)
            {
                printInterpretationLineAboveCode = this.ConvertBoolean(zplDataParts[3]);
            }

            virtualPrinter.SetNextElementFieldData(new Industrial2of5BarcodeFieldData
            {
                FieldOrientation = fieldOrientation,
                Height = height,
                PrintInterpretationLine = printInterpretationLine,
                PrintInterpretationLineAboveCode = printInterpretationLineAboveCode
            });

            return null;
        }
    }
}
