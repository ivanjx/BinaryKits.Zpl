using BinaryKits.Zpl.Label.Helpers;

using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.Helpers
{
    internal static class ImageHelper
    {
        private static readonly Regex hexDataRegex = new("^[0-9A-Fa-f]+$", RegexOptions.Compiled);
        private static readonly Regex z64DataRegex = new(":(Z64):(\\S+):([0-9a-fA-F]+)", RegexOptions.Compiled);
        private static readonly Regex b64DataRegex = new(":(B64):(\\S+):([0-9a-fA-F]+)", RegexOptions.Compiled);

        /// <param name="dataHex">Hex, <c>:Z64:</c>, <c>:B64:</c> or ACS data</param>
        /// <param name="bytesPerRow">Row length for ACS</param>
        /// <param name="verifyCrc">Check the CRC of <c>:Z64:</c>/<c>:B64:</c> data; throws <see cref="System.FormatException"/> on mismatch</param>
        public static byte[] GetImageBytes(string dataHex, int bytesPerRow, bool verifyCrc = false)
        {
            if (z64DataRegex.IsMatch(dataHex))
            {
                return ZebraZ64CompressionHelper.Uncompress(dataHex, verifyCrc);
            }

            if (b64DataRegex.IsMatch(dataHex))
            {
                return ZebraB64CompressionHelper.Uncompress(dataHex, verifyCrc);
            }

            if (hexDataRegex.IsMatch(dataHex))
            {
                return dataHex.ToBytesFromHex();
            }

            return ZebraACSCompressionHelper.Uncompress(dataHex, bytesPerRow).ToBytesFromHex();
        }
    }
}
