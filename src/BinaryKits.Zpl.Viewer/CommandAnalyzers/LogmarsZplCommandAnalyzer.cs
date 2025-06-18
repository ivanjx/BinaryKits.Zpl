using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers;

public class LogmarsZplCommandAnalyzer : ZplCommandAnalyzerBase
{
    public LogmarsZplCommandAnalyzer(VirtualPrinter virtualPrinter) : base("^BL", virtualPrinter)
    {
    }

    public override ZplElementBase Analyze(string zplCommand)
    {
        var zplDataParts = this.SplitCommand(zplCommand);

        // ^BLN,100,N
        FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0]);
        int height = this.VirtualPrinter.BarcodeInfo.Height;

        if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out int tmpint))
        {
            height = tmpint;
        }

        bool printInterpretationLineAbove = false;

        if (zplDataParts.Length > 2)
        {
            printInterpretationLineAbove = this.ConvertBoolean(zplDataParts[2], "Y");
        }

        this.VirtualPrinter.SetNextElementFieldData(
            new CodeLogmarsBarcodeFieldData
            {
                FieldOrientation = fieldOrientation,
                Height = height,
                PrintInterpretationLineAboveCode = printInterpretationLineAbove
            });

        return null;
    }
}
