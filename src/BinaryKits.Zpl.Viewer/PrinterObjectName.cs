using System;

namespace BinaryKits.Zpl.Viewer
{
    /// <summary>
    /// Normalized name of an object in printer memory: uppercase device letter and uppercase <c>NAME.EXT</c>.
    /// </summary>
    public readonly record struct PrinterObjectName(char Device, string Name)
    {
        public const char DefaultDevice = 'R';

        /// <summary>
        /// Storage key, <c>D:NAME.EXT</c>.
        /// </summary>
        public string Key => $"{this.Device}:{this.Name}";

        public override string ToString()
        {
            return this.Key;
        }

        /// <summary>
        /// Parses <c>d:o.x</c>: trims, uppercases, defaults the device to <c>R:</c> and appends
        /// <paramref name="defaultExtension"/> (for example <c>.FNT</c>) only when the name has no extension.
        /// Pass <c>null</c> to keep names without extension unchanged (font references do no extension probing).
        /// </summary>
        /// <returns><c>false</c> when the name part is empty or the device is not a letter.</returns>
        public static bool TryParse(string raw, string defaultExtension, out PrinterObjectName name)
        {
            name = default;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string value = raw.Trim().ToUpperInvariant();
            char device = DefaultDevice;

            if (value.Length >= 2 && value[1] == ':')
            {
                device = value[0];
                value = value.Substring(2).Trim();
            }

            if (device < 'A' || device > 'Z' || value.Length == 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(defaultExtension) && value.IndexOf('.') < 0)
            {
                value += defaultExtension.ToUpperInvariant();
            }

            name = new PrinterObjectName(device, value);
            return true;
        }

        /// <summary>
        /// Same as <see cref="TryParse"/>, throws when the name cannot be parsed.
        /// </summary>
        public static PrinterObjectName Parse(string raw, string defaultExtension)
        {
            if (!TryParse(raw, defaultExtension, out PrinterObjectName name))
            {
                throw new FormatException($"Invalid printer object name '{raw}'.");
            }

            return name;
        }
    }
}
