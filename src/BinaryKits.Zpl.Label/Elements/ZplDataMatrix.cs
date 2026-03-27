using BinaryKits.Zpl.Label.Helpers;
using System.Collections.Generic;
using System.Text;

namespace BinaryKits.Zpl.Label.Elements
{
    /// <summary>
    /// Data Matrix Bar Code, ^BXo,h,s,c,r,f,g,a
    /// </summary>
    public class ZplDataMatrix : ZplFieldDataElementBase
    {
        private const int DefaultFormat = 6;
        private const char DefaultEscapeSequence = '~';

        public int Height { get; protected set; }

        public QualityLevel QualityLevel { get; protected set; }

        public int? Columns { get; protected set; }

        public int? Rows { get; protected set; }

        public int Format { get; protected set; }

        public char EscapeSequence { get; protected set; }

        public int? AspectRatio { get; protected set; }

        /// <summary>
        /// Data Matrix Bar Code
        /// </summary>
        /// <param name="content"></param>
        /// <param name="positionX"></param>
        /// <param name="positionY"></param>
        /// <param name="height"></param>
        /// <param name="qualityLevel"></param>
        /// <param name="fieldOrientation"></param>
        /// <param name="hexadecimalIndicator"></param>
        /// <param name="bottomToTop"></param>
        /// <param name="useDefaultPosition"></param>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        /// <param name="format"></param>
        /// <param name="escapeSequence"></param>
        /// <param name="aspectRatio"></param>
        public ZplDataMatrix(
            string content,
            int positionX,
            int positionY,
            int height = 100,
            QualityLevel qualityLevel = QualityLevel.ECC0,
            FieldOrientation fieldOrientation = FieldOrientation.Normal,
            char? hexadecimalIndicator = null,
            bool bottomToTop = false,
            bool useDefaultPosition = false,
            int? columns = null,
            int? rows = null,
            int format = DefaultFormat,
            char escapeSequence = DefaultEscapeSequence,
            int? aspectRatio = null)
            : base(content, positionX, positionY, fieldOrientation, hexadecimalIndicator, bottomToTop, useDefaultPosition)
        {
            Height = height;
            QualityLevel = qualityLevel;
            Columns = columns;
            Rows = rows;
            Format = format;
            EscapeSequence = escapeSequence;
            AspectRatio = aspectRatio;
        }

        protected string RenderQualityLevel()
        {
            return RenderQualityLevel(QualityLevel);
        }

        ///<inheritdoc/>
        public override IEnumerable<string> Render(ZplRenderOptions context)
        {
            //^FO100,100
            //^BXN,10,200
            //^FDZEBRA TECHNOLOGIES CORPORATION ^ FS
            List<string> result = new();
            result.AddRange(RenderPosition(context));
            List<string> parameters = new()
            {
                context.Scale(Height).ToString(),
                RenderQualityLevel()
            };

            bool hasExtendedParameters = Columns.HasValue || Rows.HasValue || Format != DefaultFormat || EscapeSequence != DefaultEscapeSequence || AspectRatio.HasValue;

            if (hasExtendedParameters)
            {
                parameters.Add(Columns?.ToString() ?? string.Empty);
                parameters.Add(Rows?.ToString() ?? string.Empty);
            }

            if (Format != DefaultFormat || EscapeSequence != DefaultEscapeSequence || AspectRatio.HasValue)
            {
                parameters.Add(Format != DefaultFormat ? Format.ToString() : string.Empty);
            }

            if (EscapeSequence != DefaultEscapeSequence || AspectRatio.HasValue)
            {
                parameters.Add(EscapeSequence.ToString());
            }

            if (AspectRatio.HasValue)
            {
                parameters.Add(AspectRatio.Value.ToString());
            }

            result.Add($"^BX{RenderFieldOrientation()},{string.Join(",", parameters)}");
            result.Add(RenderFieldDataSection());

            return result;
        }

    }
}
