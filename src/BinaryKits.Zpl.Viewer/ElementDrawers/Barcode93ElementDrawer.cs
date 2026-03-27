using BinaryKits.Zpl.Label;
using BinaryKits.Zpl.Label.Elements;
using BinaryKits.Zpl.Viewer.Helpers;

using SkiaSharp;

using System;
using System.Text;
using ZXing.OneD;

namespace BinaryKits.Zpl.Viewer.ElementDrawers
{
    public class Barcode93ElementDrawer : BarcodeDrawerBase
    {
        ///<inheritdoc/>
        public override bool CanDraw(ZplElementBase element)
        {
            return element is ZplBarcode93;
        }

        ///<inheritdoc/>
        public override SKPoint Draw(ZplElementBase element, DrawerOptions options, SKPoint currentPosition, InternationalFont internationalFont, int printDensityDpmm)
        {
            if (element is ZplBarcode93 barcode)
            {
                float x = barcode.PositionX;
                float y = barcode.PositionY;

                if (barcode.UseDefaultPosition)
                {
                    x = currentPosition.X;
                    y = currentPosition.Y;
                }

                string content = barcode.Content;
                if (barcode.HexadecimalIndicator is char hexIndicator)
                {
                    content = content.ReplaceHexEscapes(hexIndicator, internationalFont);
                }

                Code93Writer writer = new();
                bool[] result = writer.encode(content);
                using SKBitmap resizedImage = BoolArrayToSKBitmap(result, barcode.Height, barcode.ModuleWidth);
                byte[] png = resizedImage.Encode(SKEncodedImageFormat.Png, 100).ToArray();
                this.DrawBarcode(png, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation);

                if (barcode.PrintInterpretationLine)
                {
                    float labelFontSize = FontScale.GetBitmappedFontSize("A", Math.Min(barcode.ModuleWidth, 10), printDensityDpmm).Value;
                    SKTypeface labelTypeFace = options.FontManager.FontLoader("A");
                    SKFont labelFont = new(labelTypeFace, labelFontSize);
                    string interpretation = BuildCode93Interpretation(content, barcode.CheckDigit);
                    this.DrawInterpretationLine(interpretation, labelFont, x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, barcode.PrintInterpretationLineAboveCode, options);
                }

                return this.CalculateNextDefaultPosition(x, y, resizedImage.Width, resizedImage.Height, barcode.FieldOrigin != null, barcode.FieldOrientation, currentPosition);
            }

            return currentPosition;


        }

        private const string Code93Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%abcd";
        private const string Code93DisplayAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%!#&@";
        public const string Code93StartStopSymbol = "▯";

        public static string BuildCode93Interpretation(string content, bool includeCheckDigits)
        {
            string interpretation = Code93StartStopSymbol + content;

            if (!includeCheckDigits)
            {
                return interpretation + Code93StartStopSymbol;
            }

            string extended = ConvertToExtended(content);
            int checkC = ComputeChecksumIndex(extended, 20);
            extended += Code93Alphabet[checkC];
            int checkK = ComputeChecksumIndex(extended, 15);

            return interpretation +
                MapDisplayChar(Code93Alphabet[checkC]) +
                MapDisplayChar(Code93Alphabet[checkK]) +
                Code93StartStopSymbol;
        }

        private static int ComputeChecksumIndex(string content, int maxWeight)
        {
            int weight = 1;
            int total = 0;

            for (int i = content.Length - 1; i >= 0; i--)
            {
                int index = Code93Alphabet.IndexOf(content[i]);

                if (index < 0)
                {
                    throw new ArgumentException("Invalid Code 93 content.", nameof(content));
                }

                total += index * weight;

                if (++weight > maxWeight)
                {
                    weight = 1;
                }
            }

            return total % 47;
        }

        private static char MapDisplayChar(char character)
        {
            int index = Code93Alphabet.IndexOf(character);

            if (index < 0)
            {
                throw new ArgumentException("Invalid Code 93 character.", nameof(character));
            }

            return Code93DisplayAlphabet[index];
        }

        private static string ConvertToExtended(string content)
        {
            StringBuilder extendedContent = new(content.Length * 2);

            foreach (char character in content)
            {
                if (character == 0)
                {
                    extendedContent.Append("bU");
                }
                else if (character <= 26)
                {
                    extendedContent.Append('a');
                    extendedContent.Append((char)('A' + character - 1));
                }
                else if (character <= 31)
                {
                    extendedContent.Append('b');
                    extendedContent.Append((char)('A' + character - 27));
                }
                else if (character is ' ' or '$' or '%' or '+')
                {
                    extendedContent.Append(character);
                }
                else if (character <= ',')
                {
                    extendedContent.Append('c');
                    extendedContent.Append((char)('A' + character - '!'));
                }
                else if (character <= '9')
                {
                    extendedContent.Append(character);
                }
                else if (character == ':')
                {
                    extendedContent.Append("cZ");
                }
                else if (character <= '?')
                {
                    extendedContent.Append('b');
                    extendedContent.Append((char)('F' + character - ';'));
                }
                else if (character == '@')
                {
                    extendedContent.Append("bV");
                }
                else if (character <= 'Z')
                {
                    extendedContent.Append(character);
                }
                else if (character <= '_')
                {
                    extendedContent.Append('b');
                    extendedContent.Append((char)('K' + character - '['));
                }
                else if (character == '`')
                {
                    extendedContent.Append("bW");
                }
                else if (character <= 'z')
                {
                    extendedContent.Append('d');
                    extendedContent.Append((char)('A' + character - 'a'));
                }
                else if (character <= 127)
                {
                    extendedContent.Append('b');
                    extendedContent.Append((char)('P' + character - '{'));
                }
                else
                {
                    throw new ArgumentException($"Requested content contains a non-encodable character: '{character}'", nameof(content));
                }
            }

            return extendedContent.ToString();
        }
    }
}
