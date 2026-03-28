using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers;

public class Code11BarcodeZplCommandAnalyzer : ZplCommandAnalyzerBase
{
    public Code11BarcodeZplCommandAnalyzer() : base("^B1")
    {
    }

    public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
    {
        string[] zplDataParts = this.SplitCommand(zplCommand);

        FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);
        Code11CheckDigitCount checkDigitCount = Code11CheckDigitCount.Two;

        if (zplDataParts.Length > 1)
        {
            checkDigitCount = this.ConvertCode11CheckDigitCount(zplDataParts[1]);
        }

        int height = virtualPrinter.BarcodeInfo.Height;

        if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out int tmpint))
        {
            height = tmpint;
        }

        bool printInterpretationLine = true;

        if (zplDataParts.Length > 3)
        {
            printInterpretationLine = this.ConvertBoolean(zplDataParts[3], "Y");
        }

        bool printInterpretationLineAboveCode = false;

        if (zplDataParts.Length > 4)
        {
            printInterpretationLineAboveCode = this.ConvertBoolean(zplDataParts[4]);
        }

        virtualPrinter.SetNextElementFieldData(
            new Code11BarcodeFieldData
            {
                FieldOrientation = fieldOrientation,
                CheckDigitCount = checkDigitCount,
                Height = height,
                PrintInterpretationLine = printInterpretationLine,
                PrintInterpretationLineAboveCode = printInterpretationLineAboveCode
            });

        return null;
    }
}
