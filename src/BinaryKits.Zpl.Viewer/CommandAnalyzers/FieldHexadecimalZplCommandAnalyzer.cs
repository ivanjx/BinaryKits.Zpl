using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class FieldHexadecimalZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public FieldHexadecimalZplCommandAnalyzer(VirtualPrinter virtualPrinter) : base("^FH", virtualPrinter) { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand)
        {
            var zplDataParts = this.SplitCommand(zplCommand);

            char indicator = '_';
            
            if ((zplDataParts.Length > 0) && (zplDataParts[0].Length > 0))
            {
                indicator = zplDataParts[0][0];
            }

            this.VirtualPrinter.SetNextElementFieldUseHexadecimalIndicator(indicator);
            return null;
        }
    }
}
