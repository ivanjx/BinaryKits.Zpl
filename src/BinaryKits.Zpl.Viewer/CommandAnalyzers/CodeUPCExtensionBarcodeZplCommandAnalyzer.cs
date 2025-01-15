using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers;

public class CodeUPCExtensionBarcodeZplCommandAnalyzer : ZplCommandAnalyzerBase
{
    public CodeUPCExtensionBarcodeZplCommandAnalyzer(VirtualPrinter virtualPrinter) : base("^BS", virtualPrinter)
    {
    }

    public override ZplElementBase Analyze(string zplCommand)
    {
        var zplDataParts = this.SplitCommand(zplCommand);

        var fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0]);

        int tmpint;
        int height = this.VirtualPrinter.BarcodeInfo.Height;
        bool printInterpretationLine = true;
        bool printInterpretationLineAboveCode = true;

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

        //The field data are processing in the FieldDataZplCommandAnalyzer
        this.VirtualPrinter.SetNextElementFieldData(new CodeUPCExtensionBarcodeFieldData
        {
            FieldOrientation = fieldOrientation,
            Height = height,
            PrintInterpretationLine = printInterpretationLine,
            PrintInterpretationLineAboveCode = printInterpretationLineAboveCode
        });

        return null;
    }
}
