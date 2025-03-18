using BinaryKits.Zpl.Label.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryKits.Zpl.Label.Elements
{
    //^FD – Field Data
    public class ZplTextField : ZplPositionedElementBase, IFormatElement
    {
        //^A
        public ZplFont Font { get; protected set; }
        //^FH
        public char HexadecimalIndicator { get; protected set; }
        //^FR
        public bool ReversePrint { get; protected set; }

        public NewLineConversionMethod NewLineConversion { get; protected set; }
        //^FD
        public string Text { get; protected set; }

        /// <summary>
        /// Construct a ^FD (Field Data) element, together with the ^FO, ^A and ^FH.
        /// Control character will be handled (Convert to Hex or replace with ' ')
        /// </summary>
        /// <param name="text">Original text content</param>
        /// <param name="positionX"></param>
        /// <param name="positionY"></param>
        /// <param name="font"></param>
        /// <param name="newLineConversion"></param>
        /// <param name="hexadecimalIndicator"></param>
        /// <param name="reversePrint"></param>
        /// <param name="bottomToTop"></param>
        /// <param name="fieldJustification"></param>
        public ZplTextField(
            string text,
            int positionX,
            int positionY,
            ZplFont font,
            NewLineConversionMethod newLineConversion = NewLineConversionMethod.ToSpace,
            char hexadecimalIndicator = default,
            bool reversePrint = false,
            bool bottomToTop = false,
            FieldJustification fieldJustification = FieldJustification.None)
            : base(positionX, positionY, bottomToTop, fieldJustification)
        {
            Text = text;
            Font = font;
            this.HexadecimalIndicator = hexadecimalIndicator;
            NewLineConversion = newLineConversion;
            ReversePrint = reversePrint;
        }

        ///<inheritdoc/>
        public override IEnumerable<string> Render(ZplRenderOptions context)
        {
            var result = new List<string>();
            result.AddRange(Font.Render(context));
            result.AddRange(RenderPosition(context));
            result.Add(
                Text.RenderFieldDataSection(
                    HexadecimalIndicator,
                    ReversePrint,
                    NewLineConversion));

            return result;
        }

        /// <inheritdoc />
        public void SetTemplateContent(string content)
        {
            Text = content;
        }
    }
}
