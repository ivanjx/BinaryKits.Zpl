using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers;

public class MsiBarcodeZplCommandAnalyzer : ZplCommandAnalyzerBase
{
    public MsiBarcodeZplCommandAnalyzer(VirtualPrinter virtualPrinter) : base("^BM", virtualPrinter)
    {
    }

    public override ZplElementBase Analyze(string zplCommand)
    {
        var zplDataParts = this.SplitCommand(zplCommand);

        // ^BMN,B,100,Y,N,N
        FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0]);
        MsiBarcodeCheckDigitMode checkDigitSelection = default;

        if (zplDataParts.Length > 1)
        {
            checkDigitSelection = this.ConvertCheckDigitSelection(zplDataParts[1]);
        }

        int height = this.VirtualPrinter.BarcodeInfo.Height;

        if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out int tmpint))
        {
            height = tmpint;
        }

        bool printInterpretationLine = true;

        if (zplDataParts.Length > 3)
        {
            printInterpretationLine = this.ConvertBoolean(zplDataParts[3], "Y");
        }

        bool printInterpretationLineAbove = false;

        if (zplDataParts.Length > 4)
        {
            printInterpretationLineAbove = this.ConvertBoolean(zplDataParts[4], "N");
        }

        bool printCheckDigit = false;

        if (zplDataParts.Length > 5)
        {
            printCheckDigit = this.ConvertBoolean(zplDataParts[5], "N");
        }

        this.VirtualPrinter.SetNextElementFieldData(
            new CodeMsiBarcodeFieldData
            {
                FieldOrientation = fieldOrientation,
                CheckDigitSelection = checkDigitSelection,
                Height = height,
                PrintInterpretationLine = printInterpretationLine,
                PrintInterpretationLineAboveCode = printInterpretationLineAbove,
                PrintInterpretationLineWithCheckDigit = printCheckDigit
            });

        return null;
    }
}
