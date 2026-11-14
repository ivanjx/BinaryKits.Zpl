using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;

using System;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ^FPd,g Field Parameter: character direction (H, V, R) and additional inter-character gap (0–9999 dots) of the
    /// next field. An invalid direction is H and an invalid gap is 0 (Labelary).
    /// </summary>
    public class FieldParameterZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        private const int maxCharacterGap = 9999;

        public FieldParameterZplCommandAnalyzer() : base("^FP") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            FieldDirection fieldDirection = FieldDirection.Horizontal;
            if (zplDataParts.Length > 0)
            {
                switch (zplDataParts[0])
                {
                    case "V":
                        fieldDirection = FieldDirection.Vertical;
                        break;
                    case "R":
                        fieldDirection = FieldDirection.Reverse;
                        break;
                }
            }

            int characterGap = 0;
            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out int tmpint))
            {
                characterGap = Math.Clamp(tmpint, 0, maxCharacterGap);
            }

            virtualPrinter.SetNextElementFieldParameter(fieldDirection, characterGap);

            return null;
        }
    }
}
