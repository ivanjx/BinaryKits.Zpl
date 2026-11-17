using BinaryKits.Zpl.Viewer.Helpers;

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.DownloadedFonts
{
    /// <summary>
    /// Shared parts of the download commands (~DB, ~DY, ~DU, ~DT): object names and payload decoding.
    /// </summary>
    internal static class ZplDownloadData
    {
        private const string defaultObjectName = "UNKNOWN";

        private static readonly Regex wrappedDataRegex = new(@"^:(Z64|B64):([^:]+):([0-9A-Fa-f]{1,4})$", RegexOptions.Compiled);
        private static readonly Regex hexDataRegex = new("^[0-9A-Fa-f]*$", RegexOptions.Compiled);

        /// <summary>
        /// Parses <c>d:o.x</c> with <see cref="PrinterObjectName.TryParse"/>; a missing name becomes <c>UNKNOWN</c>.
        /// </summary>
        public static bool TryParseObjectName(string raw, string defaultExtension, out PrinterObjectName name)
        {
            string value = (raw ?? string.Empty).Trim();

            if (value.Length == 0)
            {
                value = defaultObjectName;
            }
            else if (value.Length == 2 && value[1] == ':')
            {
                value += defaultObjectName;
            }

            return PrinterObjectName.TryParse(value, defaultExtension, out name);
        }

        /// <summary>
        /// Decodes a download payload: whitespace is skipped; <c>:Z64:</c>/<c>:B64:</c> need a valid CRC-16; otherwise
        /// hex, with ACS when <paramref name="allowAcs"/>. <paramref name="legacyZeros"/> reads <c>O</c> as <c>0</c>
        /// when the data is hex apart from that. Throws <see cref="FormatException"/> unless exactly
        /// <paramref name="expectedBytes"/> bytes are decoded.
        /// </summary>
        public static byte[] Decode(string source, long expectedBytes, int bytesPerRow, bool allowAcs, bool legacyZeros)
        {
            string data = RemoveWhitespace(source);
            byte[] bytes;

            try
            {
                if (data.StartsWith(":Z64:") || data.StartsWith(":B64:"))
                {
                    if (!wrappedDataRegex.IsMatch(data))
                    {
                        throw new FormatException("B64/Z64 data must end with a hexadecimal CRC.");
                    }

                    bytes = ImageHelper.GetImageBytes(data, bytesPerRow, verifyCrc: true);
                }
                else
                {
                    if (legacyZeros)
                    {
                        string zeros = data.Replace('O', '0').Replace('o', '0');
                        if (hexDataRegex.IsMatch(zeros))
                        {
                            data = zeros;
                        }
                    }

                    if (!allowAcs && !hexDataRegex.IsMatch(data))
                    {
                        throw new FormatException("Data must be hexadecimal, B64 or Z64 encoded.");
                    }

                    bytes = data.Length == 0 ? [] : ImageHelper.GetImageBytes(data, bytesPerRow);
                }
            }
            catch (FormatException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new FormatException($"Data cannot be decoded: {exception.Message}", exception);
            }

            if (bytes.Length != expectedBytes)
            {
                throw new FormatException($"Expected {expectedBytes} bytes but decoded {bytes.Length}.");
            }

            return bytes;
        }

        private static string RemoveWhitespace(string value)
        {
            StringBuilder builder = new(value.Length);

            foreach (char c in value)
            {
                if (!char.IsWhiteSpace(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
