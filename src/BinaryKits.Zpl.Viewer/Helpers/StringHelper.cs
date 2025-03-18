using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Replaces hex escapes within a text.
        /// </summary>
        /// <param name="text">Topic variable</param>
        /// <param name="replaceChar"></param>
        /// <returns>Text with hex escapes replaced with their char equivalents.</returns>
        public static string ReplaceHexEscapes(this string text, char replaceChar)
        {
            // Build a regex pattern using the provided replaceChar.
            // This pattern matches one or more consecutive occurrences of the escape indicator followed by two hex digits.
            string pattern = $"((?:{Regex.Escape(replaceChar.ToString())}[0-9A-Fa-f]{{2}})+)";

            return Regex.Replace(text, pattern, match =>
            {
                // Remove the escape indicator characters to get a continuous hex string.
                string hexSequence = match.Value.Replace(replaceChar.ToString(), "");
                byte[] bytes = new byte[hexSequence.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = byte.Parse(hexSequence.Substring(i * 2, 2), NumberStyles.HexNumber);
                }
                // Decode the byte array using UTF-8.
                return Encoding.UTF8.GetString(bytes);
            });
        }
    }
}
