using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class GraphicDiagonalLineCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public GraphicDiagonalLineCommandAnalyzer(VirtualPrinter virtualPrinter) : base("^GD", virtualPrinter) { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand)
        {
            int tmpint;
            int width = 3;
            int height = 3;
            int borderThickness = 1;
            var lineColor = LineColor.Black;
            bool rightLeaningDiagonal = false;

            int x = 0;
            int y = 0;

            if (this.VirtualPrinter.NextElementPosition != null)
            {
                x = this.VirtualPrinter.NextElementPosition.X;
                y = this.VirtualPrinter.NextElementPosition.Y;
            }

            var zplDataParts = this.SplitCommand(zplCommand);

            if (zplDataParts.Length > 0 && int.TryParse(zplDataParts[0], out tmpint))
            {
                width = tmpint;
            }

            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint))
            {
                height = tmpint;
            }

            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint))
            {
                borderThickness = tmpint;
            }

            if (zplDataParts.Length > 3)
            {
                string lineColorTemp = zplDataParts[3];
                lineColor = lineColorTemp == "W" ? LineColor.White : LineColor.Black;
            }

            if (zplDataParts.Length > 4)
            {
                string orientation = zplDataParts[4];
                rightLeaningDiagonal = orientation == "R";
            }

            return new ZplGraphicDiagonalLine(x, y, width, height, borderThickness, rightLeaningDiagonal, lineColor);
        }
    }
}
