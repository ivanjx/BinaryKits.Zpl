using System;
using System.Text;

namespace BinaryKits.Zpl.Viewer.Symologies;

public static class Industrial2of5Symbology
{
    private static readonly string[] digitPatterns =
    [
        "10101110111010",
        "11101010101110",
        "10111010101110",
        "11101110101010",
        "10101110101110",
        "11101011101010",
        "10111011101010",
        "10101011101110",
        "11101010111010",
        "10111010111010"
    ];

    public static bool[] Encode(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        StringBuilder encodedValue = new("11011010");

        for (int index = 0; index < content.Length; index++)
        {
            char digit = content[index];

            if (!char.IsDigit(digit))
            {
                throw new ArgumentException("Barcode content must contain only digits.", nameof(content));
            }

            encodedValue.Append(digitPatterns[digit - '0']);
        }

        encodedValue.Append("11010110");
        bool[] result = new bool[encodedValue.Length];

        for (int index = 0; index < encodedValue.Length; index++)
        {
            result[index] = encodedValue[index] == '1';
        }

        return result;
    }
}
