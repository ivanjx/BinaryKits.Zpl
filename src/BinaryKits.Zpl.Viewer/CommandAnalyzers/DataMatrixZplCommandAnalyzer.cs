using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Models;

namespace BinaryKits.Zpl.Viewer.CommandAnalyzers
{
    public class DataMatrixZplCommandAnalyzer : ZplCommandAnalyzerBase
    {
        public DataMatrixZplCommandAnalyzer() : base("^BX") { }

        ///<inheritdoc/>
        public override ZplElementBase Analyze(string zplCommand, VirtualPrinter virtualPrinter, IPrinterStorage printerStorage)
        {
            string[] zplDataParts = this.SplitCommand(zplCommand);

            FieldOrientation fieldOrientation = this.ConvertFieldOrientation(zplDataParts[0], virtualPrinter);

            int tmpint;
            int height = virtualPrinter.BarcodeInfo.Height;
            QualityLevel qualityLevel = QualityLevel.ECC0;
            int? columns = null;
            int? rows = null;
            int format = 6;
            char escapeSequence = '~';
            int? aspectRatio = null;

            if (zplDataParts.Length > 1 && int.TryParse(zplDataParts[1], out tmpint))
            {
                height = tmpint;
            }

            if (zplDataParts.Length > 2)
            {
                qualityLevel = this.ConvertQualityLevel(zplDataParts[2]);
            }

            if (zplDataParts.Length > 3 && int.TryParse(zplDataParts[3], out tmpint))
            {
                columns = tmpint;
            }

            if (zplDataParts.Length > 4 && int.TryParse(zplDataParts[4], out tmpint))
            {
                rows = tmpint;
            }

            if (zplDataParts.Length > 5 && int.TryParse(zplDataParts[5], out tmpint))
            {
                format = tmpint;
            }

            if (zplDataParts.Length > 6 && !string.IsNullOrEmpty(zplDataParts[6]))
            {
                escapeSequence = zplDataParts[6][0];
            }

            if (zplDataParts.Length > 7 && int.TryParse(zplDataParts[7], out tmpint))
            {
                aspectRatio = tmpint;
            }

            //The field data are processing in the FieldDataZplCommandAnalyzer
            virtualPrinter.SetNextElementFieldData(new DataMatrixFieldData
            {
                FieldOrientation = fieldOrientation,
                Height = height,
                QualityLevel = qualityLevel,
                Columns = columns,
                Rows = rows,
                Format = format,
                EscapeSequence = escapeSequence,
                AspectRatio = aspectRatio
            });

            return null;
        }
    }
}
