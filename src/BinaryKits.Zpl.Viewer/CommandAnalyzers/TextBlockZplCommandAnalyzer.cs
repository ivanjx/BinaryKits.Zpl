using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

using System;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    /// <summary>
    /// ^TBa,w,h Text Block: field orientation (default: the current ^A / ^FW orientation) and block size in dots
    /// (1–32000, default 1). The block replaces a preceding ^FB of the same field.
    /// </summary>
    public class TextBlockZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        private const int minBlockSize = 1;
        private const int maxBlockSize = 32000;

        public TextBlockZplCommandAnalyzer() : base("^TB") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            if (zplDataParts.Length > 0 && zplDataParts[0].Length > 0)
            {
                FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);

                if (virtualPrinter.NextFont != null)
                {
                    virtualPrinter.SetNextFont(virtualPrinter.NextFont.FontName, fieldOrientation, virtualPrinter.NextFont.FontWidth, virtualPrinter.NextFont.FontHeight);
                }
                else
                {
                    virtualPrinter.SetNextFont(virtualPrinter.FontName, fieldOrientation, virtualPrinter.FontWidth, virtualPrinter.FontHeight);
                }
            }

            int width = minBlockSize;
            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out int tmpint))
            {
                width = Math.Clamp(tmpint, minBlockSize, maxBlockSize);
            }

            int height = minBlockSize;
            if (zplDataParts.Length > 2 && int.TryParse(zplDataParts[2], out tmpint))
            {
                height = Math.Clamp(tmpint, minBlockSize, maxBlockSize);
            }

            virtualPrinter.ClearNextElementFieldBlock();
            virtualPrinter.SetNextElementTextBlock(new TextBlock
            {
                Width = width,
                Height = height
            });

            return null;
        }
    }
}
