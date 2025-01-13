using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers;

public class CodeUPCABarcodeZplCommandAnalyzer : ZplCommandAnalyzerBase
{
    public CodeUPCABarcodeZplCommandAnalyzer(VirtualPrinter virtualPrinter) : base("^BU", virtualPrinter)
    {
    }

    public override ZplElementBase Analyze(string zplCommand)
    {
        var zplDataParts = this.SplitCommand(zplCommand);

        var fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0]);

        int tmpint;
        int height = this.VirtualPrinter.BarcodeInfo.Height;
        bool printInterpretationLine = true;
        bool printInterpretationLineAboveCode = false;
        bool printCheckDigit = true;

        if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint))
        {
            height = tmpint;
        }

        if (zplDataParts.Length > 2)
        {
            printInterpretationLine = this.ConvertBoolean(zplDataParts[2], "Y");
        }

        if (zplDataParts.Length > 3)
        {
            printInterpretationLineAboveCode = this.ConvertBoolean(zplDataParts[3]);
        }

        if (zplDataParts.Length > 4)
        {
            printCheckDigit = this.ConvertBoolean(zplDataParts[4]);
        }

        //The field data are processing in the FieldDataZplCommandAnalyzer
        this.VirtualPrinter.SetNextElementFieldData(new CodeUPCABarcodeFieldData
        {
            FieldOrientation = fieldOrientation,
            Height = height,
            PrintInterpretationLine = printInterpretationLine,
            PrintInterpretationLineAboveCode = printInterpretationLineAboveCode,
            PrintCheckDigit = printCheckDigit
        });

        return null;
    }
}
