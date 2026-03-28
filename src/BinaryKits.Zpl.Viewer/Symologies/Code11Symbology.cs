using BinaryKits.Zpl.Label;

using System;
using System.Collections.Generic;
using System.Text;

namespace BinaryKits.Zpl.Viewer.Symologies;

public static class Code11Symbology
{
    private const string Charset = "0123456789-";
    private const string StartStopPattern = "1011001";
    public const string StartStopInterpretationCharacter = "▵";
    public const string LargeStopInterpretationCharacter = "△";

    private static readonly Dictionary<char, string> patternMap = new()
    {
        ['0'] = "101011",
        ['1'] = "1101011",
        ['2'] = "1001011",
        ['3'] = "1100101",
        ['4'] = "1011011",
        ['5'] = "1101101",
        ['6'] = "1001101",
        ['7'] = "1010011",
        ['8'] = "1101001",
        ['9'] = "110101",
        ['-'] = "101101"
    };

    public static (bool[] Data, string Interpretation) Encode(string content, Code11CheckDigitCount checkDigitCount)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (content.Length == 0)
        {
            throw new ArgumentException("Barcode content cannot be empty.", nameof(content));
        }

        foreach (char character in content)
        {
            if (!patternMap.ContainsKey(character))
            {
                throw new ArgumentException($"Requested content contains a non-encodable character: '{character}'", nameof(content));
            }
        }

        string encodedContent = content + BuildCheckDigits(content, checkDigitCount);
        List<string> symbols = [StartStopPattern];
        foreach (char character in encodedContent)
        {
            symbols.Add(patternMap[character]);
        }

        symbols.Add(StartStopPattern);

        StringBuilder pattern = new();
        for (int i = 0; i < symbols.Count; i++)
        {
            if (i > 0)
            {
                pattern.Append('0');
            }

            pattern.Append(symbols[i]);
        }

        string stopInterpretationCharacter = checkDigitCount == Code11CheckDigitCount.Two
            ? LargeStopInterpretationCharacter
            : StartStopInterpretationCharacter;

        return (ToBoolArray(pattern.ToString()), StartStopInterpretationCharacter + encodedContent + stopInterpretationCharacter);
    }

    private static string BuildCheckDigits(string content, Code11CheckDigitCount checkDigitCount)
    {
        char cCheckDigit = ComputeCheckDigit(content, 10);
        if (checkDigitCount == Code11CheckDigitCount.One)
        {
            return cCheckDigit.ToString();
        }

        char kCheckDigit = ComputeCheckDigit(content + cCheckDigit, 9);
        return new string([cCheckDigit, kCheckDigit]);
    }

    private static char ComputeCheckDigit(string content, int maximumWeight)
    {
        int weight = 1;
        int sum = 0;

        for (int i = content.Length - 1; i >= 0; i--)
        {
            int value = Charset.IndexOf(content[i]);
            sum += value * weight;
            weight += 1;

            if (weight > maximumWeight)
            {
                weight = 1;
            }
        }

        return Charset[sum % 11];
    }

    private static bool[] ToBoolArray(string pattern)
    {
        bool[] result = new bool[pattern.Length];

        for (int i = 0; i < pattern.Length; i++)
        {
            result[i] = pattern[i] == '1';
        }

        return result;
    }
}
