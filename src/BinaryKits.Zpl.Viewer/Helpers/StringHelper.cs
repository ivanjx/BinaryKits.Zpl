using BinaryKits.Zpl.Label;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    public static class StringHelper
    {
        private static readonly Regex hexDigits = new(@"^[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

        /// <summary>
        /// ASCII positions replaced by the ^CI 0-12 national character sets.
        /// </summary>
        private const string NationalPositions = "#@[\\]^`{|}~";

        /// <summary>
        /// Characters printed at <see cref="NationalPositions"/> for ^CI 0-12, indexed by character set
        /// (Zebra ZPL II Programming Guide, ^CI International Character Sets table).
        /// </summary>
        private static readonly string[] nationalCharacterSets =
        [
            "#@[¢]^`{|}~", // 0 U.S.A. 1
            "#@⅓¢⅔^`¼½¾~", // 1 U.S.A. 2
            "£@[¢]^`{|}~", // 2 U.K.
            "ƒ§[Ĳ]^`{ĳ}~", // 3 Holland
            "#@ÆØÅ^`æøå~", // 4 Denmark/Norway
            "ÜÉÄÖÅÜéäöåü", // 5 Sweden/Finland
            "#§ÄÖÜ^`äöüß", // 6 Germany
            "£à[ç]^`é|ùè", // 7 France 1
            "#àâçêîôéùèû", // 8 France 2
            "£§[çé^ùàòèì", // 9 Italy
            "#§¡Ñ¿^`{ñç~", // 10 Spain
            "£ÉÄÖÜ^äëïöü", // 11 Miscellaneous
            "#@[¥]^`{|}~", // 12 Japan
        ];

        static StringHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Applies the character set of a resident font (A-H, 0, P-V) to decoded field text:
        /// ^CI 0-12 replace the national positions, every other ^CI except 13 prints U+005C as cent sign.
        /// Downloaded fonts and ^CI 13 keep the text unchanged.
        /// </summary>
        /// <param name="text">Decoded field text</param>
        /// <param name="fontName">Font name of the field</param>
        /// <param name="internationalFont">Active ^CI character set</param>
        /// <returns>Text with the characters the printer prints for the font.</returns>
        public static string ApplyInternationalCharacterSet(this string text, string fontName, InternationalFont internationalFont)
        {
            if (internationalFont == InternationalFont.ZCP850_13 || !IsResidentFont(fontName))
            {
                return text;
            }

            string replacements = internationalFont >= InternationalFont.ZCP850 && internationalFont <= InternationalFont.ZCP850_12 ?
                nationalCharacterSets[(int)internationalFont] :
                null;
            char[] chars = null;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char replacement;

                if (replacements != null)
                {
                    int position = NationalPositions.IndexOf(c);
                    if (position < 0)
                    {
                        continue;
                    }

                    replacement = replacements[position];
                }
                else if (c == '\\')
                {
                    replacement = '¢';
                }
                else
                {
                    continue;
                }

                if (replacement != c)
                {
                    chars ??= text.ToCharArray();
                    chars[i] = replacement;
                }
            }

            return chars == null ? text : new string(chars);
        }

        private static bool IsResidentFont(string fontName)
        {
            return fontName is "0" or "A" or "B" or "C" or "D" or "E" or "F" or "G" or "H"
                or "P" or "Q" or "R" or "S" or "T" or "U" or "V";
        }

        /// <summary>
        /// Replaces hex escapes within a text.
        /// </summary>
        /// <param name="text">Topic variable</param>
        /// <param name="hexIndicator">Character which indicates a hex escape</param>
        /// <param name="internationalFont">Byte encoding for escaped hex</param>
        /// <returns>Text with hex escapes replaced with their char equivalents.</returns>
        public static string ReplaceHexEscapes(this string text, char hexIndicator, InternationalFont internationalFont)
        {
            Encoding charset = GetCharset(internationalFont);
            List<byte> bytes = [];
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == hexIndicator && i + 2 < text.Length)
                {
                    string digits = text.Substring(i + 1, 2);
                    if (hexDigits.IsMatch(digits))
                    {
                        bytes.Add(byte.Parse(digits, NumberStyles.HexNumber));
                        i += 2;
                        continue;
                    }
                }

                bytes.AddRange(charset.GetBytes(c.ToString()));
            }

            return charset.GetString(bytes.ToArray());
        }

        private static Encoding GetCharset(InternationalFont internationalFont)
        {
            return internationalFont switch
            {
                InternationalFont.UTF8 => Encoding.UTF8,
                InternationalFont.UTF16_BE => Encoding.BigEndianUnicode,
                InternationalFont.UTF16_LE => Encoding.Unicode,
                InternationalFont.ZCP1250 => Encoding.GetEncoding(1250),
                InternationalFont.ZCP1251 => Encoding.GetEncoding(1251),
                InternationalFont.ZCP1252 => Encoding.GetEncoding(1252),
                InternationalFont.ZCP1253 => Encoding.GetEncoding(1253),
                InternationalFont.ZCP1254 => Encoding.GetEncoding(1254),
                InternationalFont.ZCP1255 => Encoding.GetEncoding(1255),
                _ => Encoding.GetEncoding(850),
            };
        }
    }
}
