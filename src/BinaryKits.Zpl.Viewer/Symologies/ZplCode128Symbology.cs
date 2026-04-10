using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BinaryKits.Zpl.Viewer.Symologies
{
    public enum Code128CodeSet
    {
        Code128 = 0,
        Code128A = 103,
        Code128B = 104,
        Code128C = 105
    }

    public static class ZplCode128Symbology
    {
        // detect Type-C as late as possible
        // ABC12345 -> START_B A B C 1 CODE_C 23 45 CHECK STOP
        // and not  -> START_B A B C CODE_C 12 34 CODE_B 5 CHECK STOP
        private static readonly Regex swtichCRegex = new(@"^\d\d(?:\d\d)+(?!\d)", RegexOptions.Compiled);
        private static readonly Regex startCRegex = new(@"^\d{4}", RegexOptions.Compiled);
        private static readonly Regex digitPairRegex = new(@"^\d\d", RegexOptions.Compiled);

        // GS1 specific
        private static readonly Regex gs1SwitchCRegex = new(@"^\d\d(?:\d\d|>8)+(?!\d)", RegexOptions.Compiled);
        private static readonly Regex gs1StartCRegex = new(@"^(?:\d\d|>8){2}", RegexOptions.Compiled);
        private static readonly Regex gs1DigitPairRegex = new(@"^(?:\d\d|>8)", RegexOptions.Compiled);
        private static readonly Regex fnc1Regex = new(@"^>8", RegexOptions.Compiled);
        private static readonly Regex explicitUccRegex = new(@"^(?:>;)?>8(?<digits>\d{19,20})$", RegexOptions.Compiled);

        private static readonly Regex startCodeRegex = new(@"^(>[9:;])(.+)$", RegexOptions.Compiled);

        private static readonly Dictionary<string, int> invocationMap = new() {
            { "><", 62 },
            { ">0", 30 },
            { ">=", 94 },
            { ">1", 95 },
            { ">2", 96 },
            { ">3", 97 },
            { ">4", 98 },
            { ">5", 99 },
            { ">6", 100 },
            { ">7", 101 },
            { ">8", 102 }
        };

        private static readonly Dictionary<string, Code128CodeSet> startCodeMap = new() {
            { ">9", Code128CodeSet.Code128A },
            { ">:", Code128CodeSet.Code128B },
            { ">;", Code128CodeSet.Code128C }
        };

        private static readonly Dictionary<Code128CodeSet, string> codeSetCodeMap = new() {
            { Code128CodeSet.Code128A, CODE_A },
            { Code128CodeSet.Code128B, CODE_B },
            { Code128CodeSet.Code128C, CODE_C }
        };

        private const string FNC_1 = "FNC_1";
        private const string FNC_2 = "FNC_2";
        private const string FNC_3 = "FNC_3";
        private const string FNC_4 = "FNC_4";

        private const string SHIFT_A = "SHIFT_A";
        private const string SHIFT_B = "SHIFT_B";

        private const string CODE_A = "CODE_A";
        private const string CODE_B = "CODE_B";
        private const string CODE_C = "CODE_C";

        private const string START_A = "START_A";
        private const string START_B = "START_B";
        private const string START_C = "START_C";

        private const string STOP = "STOP";

        private static readonly int[] patterns;
        private static readonly string[] codeAChars;
        private static readonly string[] codeBChars;
        private static readonly string[] codeCChars;

        private static readonly Dictionary<string, int> codeAMap;
        private static readonly Dictionary<string, int> codeBMap;
        private static readonly Dictionary<string, int> codeCMap;

        private static readonly Dictionary<Code128CodeSet, (string[], Dictionary<string, int>)> codeMaps;

        private readonly struct Gs1ApplicationIdentifierDefinition
        {
            public int FixedLength { get; }
            public int MaxLength { get; }
            public bool VariableLength { get; }
            public bool NumericOnly { get; }

            public Gs1ApplicationIdentifierDefinition(int fixedLength, int maxLength, bool variableLength, bool numericOnly)
            {
                this.FixedLength = fixedLength;
                this.MaxLength = maxLength;
                this.VariableLength = variableLength;
                this.NumericOnly = numericOnly;
            }
        }

        private readonly struct ParsedGs1Field
        {
            public string ApplicationIdentifier { get; }
            public Gs1ApplicationIdentifierDefinition Definition { get; }
            public string Data { get; }
            public string RawData { get; }

            public ParsedGs1Field(string applicationIdentifier, Gs1ApplicationIdentifierDefinition definition, string data, string rawData)
            {
                this.ApplicationIdentifier = applicationIdentifier;
                this.Definition = definition;
                this.Data = data;
                this.RawData = rawData;
            }
        }

        /// <summary>
        /// <see href="https://en.wikipedia.org/wiki/Code_128#Bar_code_widths"/>
        /// </summary>
        static ZplCode128Symbology()
        {
            var codeSetTable = new[]
            {
                new { Value = 0, A = " ", B = " ", C = "00", Pattern = 0b11011001100 },
                new { Value = 1, A = "!", B = "!", C = "01", Pattern = 0b11001101100 },
                new { Value = 2, A = "\"", B = "\"", C = "02", Pattern = 0b11001100110 },
                new { Value = 3, A = "#", B = "#", C = "03", Pattern = 0b10010011000 },
                new { Value = 4, A = "$", B = "$", C = "04", Pattern = 0b10010001100 },
                new { Value = 5, A = "%", B = "%", C = "05", Pattern = 0b10001001100 },
                new { Value = 6, A = "&", B = "&", C = "06", Pattern = 0b10011001000 },
                new { Value = 7, A = "'", B = "'", C = "07", Pattern = 0b10011000100 },
                new { Value = 8, A = "(", B = "(", C = "08", Pattern = 0b10001100100 },
                new { Value = 9, A = ")", B = ")", C = "09", Pattern = 0b11001001000 },
                new { Value = 10, A = "*", B = "*", C = "10", Pattern = 0b11001000100 },
                new { Value = 11, A = "+", B = "+", C = "11", Pattern = 0b11000100100 },
                new { Value = 12, A = ",", B = ",", C = "12", Pattern = 0b10110011100 },
                new { Value = 13, A = "-", B = "-", C = "13", Pattern = 0b10011011100 },
                new { Value = 14, A = ".", B = ".", C = "14", Pattern = 0b10011001110 },
                new { Value = 15, A = "/", B = "/", C = "15", Pattern = 0b10111001100 },
                new { Value = 16, A = "0", B = "0", C = "16", Pattern = 0b10011101100 },
                new { Value = 17, A = "1", B = "1", C = "17", Pattern = 0b10011100110 },
                new { Value = 18, A = "2", B = "2", C = "18", Pattern = 0b11001110010 },
                new { Value = 19, A = "3", B = "3", C = "19", Pattern = 0b11001011100 },
                new { Value = 20, A = "4", B = "4", C = "20", Pattern = 0b11001001110 },
                new { Value = 21, A = "5", B = "5", C = "21", Pattern = 0b11011100100 },
                new { Value = 22, A = "6", B = "6", C = "22", Pattern = 0b11001110100 },
                new { Value = 23, A = "7", B = "7", C = "23", Pattern = 0b11101101110 },
                new { Value = 24, A = "8", B = "8", C = "24", Pattern = 0b11101001100 },
                new { Value = 25, A = "9", B = "9", C = "25", Pattern = 0b11100101100 },
                new { Value = 26, A = ":", B = ":", C = "26", Pattern = 0b11100100110 },
                new { Value = 27, A = ";", B = ";", C = "27", Pattern = 0b11101100100 },
                new { Value = 28, A = "<", B = "<", C = "28", Pattern = 0b11100110100 },
                new { Value = 29, A = "=", B = "=", C = "29", Pattern = 0b11100110010 },
                new { Value = 30, A = ">", B = ">", C = "30", Pattern = 0b11011011000 },
                new { Value = 31, A = "?", B = "?", C = "31", Pattern = 0b11011000110 },
                new { Value = 32, A = "@", B = "@", C = "32", Pattern = 0b11000110110 },
                new { Value = 33, A = "A", B = "A", C = "33", Pattern = 0b10100011000 },
                new { Value = 34, A = "B", B = "B", C = "34", Pattern = 0b10001011000 },
                new { Value = 35, A = "C", B = "C", C = "35", Pattern = 0b10001000110 },
                new { Value = 36, A = "D", B = "D", C = "36", Pattern = 0b10110001000 },
                new { Value = 37, A = "E", B = "E", C = "37", Pattern = 0b10001101000 },
                new { Value = 38, A = "F", B = "F", C = "38", Pattern = 0b10001100010 },
                new { Value = 39, A = "G", B = "G", C = "39", Pattern = 0b11010001000 },
                new { Value = 40, A = "H", B = "H", C = "40", Pattern = 0b11000101000 },
                new { Value = 41, A = "I", B = "I", C = "41", Pattern = 0b11000100010 },
                new { Value = 42, A = "J", B = "J", C = "42", Pattern = 0b10110111000 },
                new { Value = 43, A = "K", B = "K", C = "43", Pattern = 0b10110001110 },
                new { Value = 44, A = "L", B = "L", C = "44", Pattern = 0b10001101110 },
                new { Value = 45, A = "M", B = "M", C = "45", Pattern = 0b10111011000 },
                new { Value = 46, A = "N", B = "N", C = "46", Pattern = 0b10111000110 },
                new { Value = 47, A = "O", B = "O", C = "47", Pattern = 0b10001110110 },
                new { Value = 48, A = "P", B = "P", C = "48", Pattern = 0b11101110110 },
                new { Value = 49, A = "Q", B = "Q", C = "49", Pattern = 0b11010001110 },
                new { Value = 50, A = "R", B = "R", C = "50", Pattern = 0b11000101110 },
                new { Value = 51, A = "S", B = "S", C = "51", Pattern = 0b11011101000 },
                new { Value = 52, A = "T", B = "T", C = "52", Pattern = 0b11011100010 },
                new { Value = 53, A = "U", B = "U", C = "53", Pattern = 0b11011101110 },
                new { Value = 54, A = "V", B = "V", C = "54", Pattern = 0b11101011000 },
                new { Value = 55, A = "W", B = "W", C = "55", Pattern = 0b11101000110 },
                new { Value = 56, A = "X", B = "X", C = "56", Pattern = 0b11100010110 },
                new { Value = 57, A = "Y", B = "Y", C = "57", Pattern = 0b11101101000 },
                new { Value = 58, A = "Z", B = "Z", C = "58", Pattern = 0b11101100010 },
                new { Value = 59, A = "[", B = "[", C = "59", Pattern = 0b11100011010 },
                new { Value = 60, A = "\\", B = "\\", C = "60", Pattern = 0b11101111010 },
                new { Value = 61, A = "]", B = "]", C = "61", Pattern = 0b11001000010 },
                new { Value = 62, A = "^", B = "^", C = "62", Pattern = 0b11110001010 },
                new { Value = 63, A = "_", B = "_", C = "63", Pattern = 0b10100110000 },
                new { Value = 64, A = "\x00", B = "`", C = "64", Pattern = 0b10100001100 },
                new { Value = 65, A = "\x01", B = "a", C = "65", Pattern = 0b10010110000 },
                new { Value = 66, A = "\x02", B = "b", C = "66", Pattern = 0b10010000110 },
                new { Value = 67, A = "\x03", B = "c", C = "67", Pattern = 0b10000101100 },
                new { Value = 68, A = "\x04", B = "d", C = "68", Pattern = 0b10000100110 },
                new { Value = 69, A = "\x05", B = "e", C = "69", Pattern = 0b10110010000 },
                new { Value = 70, A = "\x06", B = "f", C = "70", Pattern = 0b10110000100 },
                new { Value = 71, A = "\x07", B = "g", C = "71", Pattern = 0b10011010000 },
                new { Value = 72, A = "\x08", B = "h", C = "72", Pattern = 0b10011000010 },
                new { Value = 73, A = "\x09", B = "i", C = "73", Pattern = 0b10000110100 },
                new { Value = 74, A = "\x0a", B = "j", C = "74", Pattern = 0b10000110010 },
                new { Value = 75, A = "\x0b", B = "k", C = "75", Pattern = 0b11000010010 },
                new { Value = 76, A = "\x0c", B = "l", C = "76", Pattern = 0b11001010000 },
                new { Value = 77, A = "\x0d", B = "m", C = "77", Pattern = 0b11110111010 },
                new { Value = 78, A = "\x0e", B = "n", C = "78", Pattern = 0b11000010100 },
                new { Value = 79, A = "\x0f", B = "o", C = "79", Pattern = 0b10001111010 },
                new { Value = 80, A = "\x10", B = "p", C = "80", Pattern = 0b10100111100 },
                new { Value = 81, A = "\x11", B = "q", C = "81", Pattern = 0b10010111100 },
                new { Value = 82, A = "\x12", B = "r", C = "82", Pattern = 0b10010011110 },
                new { Value = 83, A = "\x13", B = "s", C = "83", Pattern = 0b10111100100 },
                new { Value = 84, A = "\x14", B = "t", C = "84", Pattern = 0b10011110100 },
                new { Value = 85, A = "\x15", B = "u", C = "85", Pattern = 0b10011110010 },
                new { Value = 86, A = "\x16", B = "v", C = "86", Pattern = 0b11110100100 },
                new { Value = 87, A = "\x17", B = "w", C = "87", Pattern = 0b11110010100 },
                new { Value = 88, A = "\x18", B = "x", C = "88", Pattern = 0b11110010010 },
                new { Value = 89, A = "\x19", B = "y", C = "89", Pattern = 0b11011011110 },
                new { Value = 90, A = "\x1a", B = "z", C = "90", Pattern = 0b11011110110 },
                new { Value = 91, A = "\x1b", B = "{", C = "91", Pattern = 0b11110110110 },
                new { Value = 92, A = "\x1c", B = "|", C = "92", Pattern = 0b10101111000 },
                new { Value = 93, A = "\x1d", B = "}", C = "93", Pattern = 0b10100011110 },
                new { Value = 94, A = "\x1e", B = "~", C = "94", Pattern = 0b10001011110 },
                new { Value = 95, A = "\x1f", B = "\x7f", C = "95", Pattern = 0b10111101000 },
                new { Value = 96, A = FNC_3, B = FNC_3, C = "96", Pattern = 0b10111100010 },
                new { Value = 97, A = FNC_2, B = FNC_2, C = "97", Pattern = 0b11110101000 },
                new { Value = 98, A = SHIFT_B, B = SHIFT_A, C = "98", Pattern = 0b11110100010 },
                new { Value = 99, A = CODE_C, B = CODE_C, C = "99", Pattern = 0b10111011110 },
                new { Value = 100, A = CODE_B, B = FNC_4, C = CODE_B, Pattern = 0b10111101110 },
                new { Value = 101, A = FNC_4, B = CODE_A, C = CODE_A, Pattern = 0b11101011110 },
                new { Value = 102, A = FNC_1, B = FNC_1, C = FNC_1, Pattern = 0b11110101110 },
                new { Value = 103, A = START_A, B = START_A, C = START_A, Pattern = 0b11010000100 },
                new { Value = 104, A = START_B, B = START_B, C = START_B, Pattern = 0b11010010000 },
                new { Value = 105, A = START_C, B = START_C, C = START_C, Pattern = 0b11010011100 },
                new { Value = 106, A = STOP, B = STOP, C = STOP, Pattern = 0b1100011101011 },
            };

            patterns = codeSetTable.Select(item => item.Pattern).ToArray();
            codeAChars = codeSetTable.Select(item => item.A).ToArray();
            codeBChars = codeSetTable.Select(item => item.B).ToArray();
            codeCChars = codeSetTable.Select(item => item.C).ToArray();

            codeAMap = codeSetTable.ToDictionary(item => item.A, item => item.Value);
            codeBMap = codeSetTable.ToDictionary(item => item.B, item => item.Value);
            codeCMap = codeSetTable.ToDictionary(item => item.C, item => item.Value);

            codeMaps = new Dictionary<Code128CodeSet, (string[], Dictionary<string, int>)>()
            {
                { Code128CodeSet.Code128A, (codeAChars, codeAMap) },
                { Code128CodeSet.Code128B, (codeBChars, codeBMap) },
                { Code128CodeSet.Code128C, (codeCChars, codeCMap) },
            };
        }

        public static (bool[], string) Encode(string content, Code128CodeSet initialCodeSet, bool gs1)
        {
            return EncodeInternal(content, initialCodeSet, gs1, null);
        }

        public static (bool[], string) Encode(string content, Code128CodeSet initialCodeSet, bool gs1, string mode, bool uccCheckDigit)
        {
            (string preparedContent, Code128CodeSet preparedCodeSet, bool preparedGs1, string interpretationOverride) = PrepareContent(content, initialCodeSet, gs1, mode, uccCheckDigit);
            return EncodeInternal(preparedContent, preparedCodeSet, preparedGs1, interpretationOverride);
        }

        private static (bool[], string) EncodeInternal(string content, Code128CodeSet initialCodeSet, bool gs1, string interpretationOverride)
        {
            List<bool> result = [];
            List<int> data;
            string interpretation;
            if (gs1)
            {
                (data, interpretation) = AnalyzeGS1(content);
            }
            else if (initialCodeSet == Code128CodeSet.Code128)
            {
                (data, interpretation) = AnalyzeAuto(content);
            }
            else
            {
                (data, interpretation) = Analyze(content, initialCodeSet);
            }

            // TODO: magic constant FNC_1
            if (gs1 && (data.Count == 1 || data[1] != 102))
            {
                data.Insert(1, 102);
            }

            foreach (int item in data)
            {
                result.AddRange(IntToBitArray(patterns[item]));
            }

            int checksum = ComputeChecksum(data.ToArray());
            result.AddRange(IntToBitArray(patterns[checksum]));
            // TODO: magic constant STOP
            result.AddRange(IntToBitArray(patterns[106]));

            return (result.ToArray(), interpretationOverride ?? interpretation);
        }

        private static (string, Code128CodeSet, bool, string) PrepareContent(string content, Code128CodeSet initialCodeSet, bool gs1, string mode, bool uccCheckDigit)
        {
            string normalizedMode = string.IsNullOrWhiteSpace(mode) ? "N" : mode.Trim().ToUpperInvariant();
            switch (normalizedMode)
            {
                case "D":
                    {
                        (string preparedContent, string interpretation) = PrepareApplicationIdentifierMode(content);
                        return (preparedContent, Code128CodeSet.Code128, true, interpretation);
                    }
                case "U":
                    {
                        string preparedContent = NormalizeAi00Content(RemoveWhitespace(content));
                        return (preparedContent, Code128CodeSet.Code128C, true, preparedContent);
                    }
                default:
                    if (uccCheckDigit)
                    {
                        Match explicitUccMatch = explicitUccRegex.Match(content);
                        if (explicitUccMatch.Success)
                        {
                            string preparedContent = NormalizeAi00Content(explicitUccMatch.Groups["digits"].Value);
                            return (preparedContent, Code128CodeSet.Code128C, true, preparedContent);
                        }
                    }

                    return (content, initialCodeSet, gs1, null);
            }
        }

        private static int ComputeChecksum(int[] data)
        {
            int checksum = data[0];
            for (int i = 1; i < data.Length; i++)
            {
                checksum += i * data[i] % 103;
            }

            return checksum % 103;
        }

        private static bool[] IntToBitArray(int value)
        {
            Stack<bool> stack = [];
            while (value > 0)
            {
                stack.Push((value & 1) == 1);
                value >>= 1;
            }

            return stack.ToArray();
        }

        private static (List<int>, string) Analyze(string content, Code128CodeSet initialCodeSet)
        {
            List<int> data = [];
            string interpretation = "";
            Code128CodeSet codeSet = initialCodeSet;
            Match startCodeMatch = startCodeRegex.Match(content);
            while (startCodeMatch.Success)
            {
                codeSet = startCodeMap[startCodeMatch.Groups[1].Value];
                content = startCodeMatch.Groups[2].Value;
                startCodeMatch = startCodeRegex.Match(content);
            }

            (string[] codeChars, Dictionary<string, int> codeMap) = codeMaps[codeSet];

            data.Add((int)codeSet);
            for (int i = 0; i < content.Length; i++)
            {
                string symbol = content[i].ToString();
                if (symbol == ">" && i + 1 < content.Length)
                {
                    string invocation = symbol + content[i + 1];
                    int value;
                    if (invocationMap.TryGetValue(invocation, out value))
                    {
                        i += 1;
                        data.Add(value);
                        string code = codeChars[value];
                        if (code == CODE_A)
                        {
                            codeSet = Code128CodeSet.Code128A;
                            (codeChars, codeMap) = codeMaps[codeSet];
                        }
                        else if (code == CODE_B)
                        {
                            codeSet = Code128CodeSet.Code128B;
                            (codeChars, codeMap) = codeMaps[codeSet];
                        }
                        else if (code == CODE_C)
                        {
                            codeSet = Code128CodeSet.Code128C;
                            (codeChars, codeMap) = codeMaps[codeSet];
                        }
                    }
                    else if (startCodeMap.TryGetValue(invocation, out Code128CodeSet newCodeSet))
                    {
                        i += 1;
                        if (newCodeSet != codeSet)
                        {
                            value = codeMap[codeSetCodeMap[newCodeSet]];
                            data.Add(value);
                            codeSet = newCodeSet;
                            (codeChars, codeMap) = codeMaps[codeSet];
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (codeSet == Code128CodeSet.Code128C)
                    {
                        if (i + 1 < content.Length)
                        {
                            string pair = symbol + content[i + 1];
                            if (codeMap.TryGetValue(pair, out int pairValue))
                            {
                                i += 1;
                                data.Add(pairValue);
                                interpretation += pair;
                            }

                            continue;
                        }

                        int switchValue = codeMap[CODE_B];
                        data.Add(switchValue);
                        codeSet = Code128CodeSet.Code128B;
                        (codeChars, codeMap) = codeMaps[codeSet];
                    }

                    if (codeMap.TryGetValue(symbol, out int value))
                    {
                        data.Add(value);
                        interpretation += symbol;
                    }
                }
            }

            return (data, interpretation);
        }

        private static (List<int>, string) AnalyzeAuto(string content)
        {
            List<int> data = [];
            string interpretation = "";
            Code128CodeSet codeSet = Code128CodeSet.Code128B;
            Dictionary<string, int> codeMap = codeBMap;
            if (startCRegex.IsMatch(content))
            {
                codeSet = Code128CodeSet.Code128C;
                codeMap = codeCMap;
            }

            data.Add((int)codeSet);

            while (content.Length > 0)
            {
                if (codeSet != Code128CodeSet.Code128C && swtichCRegex.IsMatch(content))
                {
                    data.Add(codeMap[CODE_C]);
                    codeSet = Code128CodeSet.Code128C;
                    codeMap = codeCMap;
                }
                else if (codeSet == Code128CodeSet.Code128C && !digitPairRegex.IsMatch(content))
                {
                    data.Add(codeMap[CODE_B]);
                    codeSet = Code128CodeSet.Code128B;
                    codeMap = codeBMap;
                }
                else
                {
                    string symbol = content[0].ToString();
                    if (codeSet == Code128CodeSet.Code128C)
                    {
                        symbol += content[1];
                        content = content.Substring(2);
                    }
                    else
                    {
                        content = content.Substring(1);
                    }

                    if (codeMap.TryGetValue(symbol, out int value))
                    {
                        data.Add(value);
                        interpretation += symbol;
                    }
                }
            }

            return (data, interpretation);
        }

        private static (List<int>, string) AnalyzeGS1(string content)
        {
            List<int> data = [];
            string interpretation = "";
            Code128CodeSet codeSet = Code128CodeSet.Code128B;
            Dictionary<string, int> codeMap = codeBMap;
            if (gs1StartCRegex.IsMatch(content))
            {
                codeSet = Code128CodeSet.Code128C;
                codeMap = codeCMap;
            }

            data.Add((int)codeSet);

            while (content.Length > 0)
            {
                if (codeSet != Code128CodeSet.Code128C && gs1SwitchCRegex.IsMatch(content))
                {
                    data.Add(codeMap[CODE_C]);
                    codeSet = Code128CodeSet.Code128C;
                    codeMap = codeCMap;
                }
                else if (codeSet == Code128CodeSet.Code128C && !gs1DigitPairRegex.IsMatch(content))
                {
                    data.Add(codeMap[CODE_B]);
                    codeSet = Code128CodeSet.Code128B;
                    codeMap = codeBMap;
                }
                else if (fnc1Regex.IsMatch(content))
                {
                    content = content.Substring(2);
                    data.Add(codeMap[FNC_1]);
                }
                else
                {
                    string symbol = content[0].ToString();
                    if (codeSet == Code128CodeSet.Code128C)
                    {
                        symbol += content[1];
                        content = content.Substring(2);
                    }
                    else
                    {
                        content = content.Substring(1);
                    }

                    if (codeMap.TryGetValue(symbol, out int value))
                    {
                        data.Add(value);
                        interpretation += symbol;
                    }
                }
            }

            return (data, interpretation);
        }

        private static (string, string) PrepareApplicationIdentifierMode(string content)
        {
            if (TryParseApplicationIdentifierMode(content, out List<ParsedGs1Field> fields))
            {
                StringBuilder encodedContent = new();
                StringBuilder interpretation = new();

                for (int i = 0; i < fields.Count; i++)
                {
                    ParsedGs1Field field = fields[i];
                    string data = field.Data;
                    string rawData = field.RawData;
                    if (field.ApplicationIdentifier == "00")
                    {
                        data = NormalizeAi00FieldData(field.Data);
                        rawData = ApplyCheckDigitToRawData(field.RawData, data[data.Length - 1], field.Data.Length == 18);
                    }

                    encodedContent.Append(field.ApplicationIdentifier);
                    encodedContent.Append(data);
                    if (field.Definition.VariableLength && i + 1 < fields.Count)
                    {
                        encodedContent.Append(">8");
                    }

                    interpretation.Append('(');
                    interpretation.Append(field.ApplicationIdentifier);
                    interpretation.Append(')');
                    interpretation.Append(rawData);
                }

                return (encodedContent.ToString(), interpretation.ToString());
            }

            string compactContent = RemoveWhitespace(content.Replace("(", string.Empty).Replace(")", string.Empty));
            string normalizedContent = NormalizeAi00Content(compactContent);
            string normalizedInterpretation = ApplyCheckDigitToRawData(content, normalizedContent[normalizedContent.Length - 1], compactContent.Length == 20);
            return (normalizedContent, normalizedInterpretation);
        }

        private static bool TryParseApplicationIdentifierMode(string content, out List<ParsedGs1Field> fields)
        {
            fields = [];
            int index = 0;

            while (index < content.Length)
            {
                while (index < content.Length && char.IsWhiteSpace(content[index]))
                {
                    index += 1;
                }

                if (index >= content.Length)
                {
                    break;
                }

                if (content[index] != '(')
                {
                    fields.Clear();
                    return false;
                }

                int closeIndex = content.IndexOf(')', index + 1);
                if (closeIndex < 0)
                {
                    fields.Clear();
                    return false;
                }

                string applicationIdentifier = content.Substring(index + 1, closeIndex - index - 1);
                if (!TryGetApplicationIdentifierDefinition(applicationIdentifier, out Gs1ApplicationIdentifierDefinition definition))
                {
                    fields.Clear();
                    return false;
                }

                index = closeIndex + 1;
                int rawDataStart = index;
                StringBuilder cleanData = new();

                if (definition.VariableLength)
                {
                    while (index < content.Length && content[index] != '(')
                    {
                        if (IsAllowedAiCharacter(content[index], definition))
                        {
                            cleanData.Append(content[index]);
                        }

                        index += 1;
                    }
                }
                else
                {
                    while (index < content.Length && cleanData.Length < definition.FixedLength)
                    {
                        if (content[index] == '(')
                        {
                            fields.Clear();
                            return false;
                        }

                        if (IsAllowedAiCharacter(content[index], definition))
                        {
                            cleanData.Append(content[index]);
                        }

                        index += 1;
                    }

                    if (cleanData.Length != definition.FixedLength)
                    {
                        fields.Clear();
                        return false;
                    }

                    while (index < content.Length && char.IsWhiteSpace(content[index]))
                    {
                        index += 1;
                    }
                }

                string rawData = content.Substring(rawDataStart, index - rawDataStart);
                string data = cleanData.ToString();
                if (string.IsNullOrEmpty(data) || data.Length > definition.MaxLength)
                {
                    fields.Clear();
                    return false;
                }

                if (definition.NumericOnly)
                {
                    if (!data.All(char.IsDigit))
                    {
                        fields.Clear();
                        return false;
                    }
                }
                else if (!data.All(char.IsLetterOrDigit))
                {
                    fields.Clear();
                    return false;
                }

                fields.Add(new ParsedGs1Field(applicationIdentifier, definition, data, rawData));
            }

            return fields.Count > 0;
        }

        private static bool IsAllowedAiCharacter(char ch, Gs1ApplicationIdentifierDefinition definition)
        {
            if (char.IsWhiteSpace(ch))
            {
                return false;
            }

            if (definition.NumericOnly)
            {
                return char.IsDigit(ch);
            }

            return char.IsLetterOrDigit(ch);
        }

        private static bool TryGetApplicationIdentifierDefinition(string applicationIdentifier, out Gs1ApplicationIdentifierDefinition definition)
        {
            switch (applicationIdentifier)
            {
                case "00":
                    definition = new Gs1ApplicationIdentifierDefinition(18, 18, false, true);
                    return true;
                case "01":
                    definition = new Gs1ApplicationIdentifierDefinition(14, 14, false, true);
                    return true;
                case "10":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 20, true, false);
                    return true;
                case "11":
                case "13":
                case "15":
                case "17":
                    definition = new Gs1ApplicationIdentifierDefinition(6, 6, false, true);
                    return true;
                case "20":
                    definition = new Gs1ApplicationIdentifierDefinition(2, 2, false, true);
                    return true;
                case "21":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 20, true, false);
                    return true;
                case "22":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 29, true, false);
                    return true;
                case "23":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 19, true, false);
                    return true;
                case "30":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 8, true, true);
                    return true;
                case "400":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 29, true, false);
                    return true;
                case "410":
                case "411":
                case "412":
                    definition = new Gs1ApplicationIdentifierDefinition(13, 13, false, true);
                    return true;
                case "420":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 9, true, false);
                    return true;
                case "421":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 12, true, false);
                    return true;
                case "8001":
                    definition = new Gs1ApplicationIdentifierDefinition(14, 14, false, true);
                    return true;
                case "8002":
                    definition = new Gs1ApplicationIdentifierDefinition(0, 20, true, false);
                    return true;
            }

            if (applicationIdentifier.Length == 4 && applicationIdentifier.StartsWith("31") && applicationIdentifier[2] >= '0' && applicationIdentifier[2] <= '6' && char.IsDigit(applicationIdentifier[3]))
            {
                definition = new Gs1ApplicationIdentifierDefinition(6, 6, false, true);
                return true;
            }

            if (applicationIdentifier.Length == 4 && applicationIdentifier.StartsWith("320") && char.IsDigit(applicationIdentifier[3]))
            {
                definition = new Gs1ApplicationIdentifierDefinition(6, 6, false, true);
                return true;
            }

            definition = default;
            return false;
        }

        private static string NormalizeAi00Content(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("AI 00 content must not be empty.");
            }

            if (!content.All(char.IsDigit))
            {
                throw new Exception($"AI 00 content must be numeric: {content}");
            }

            if ((content.Length != 19 && content.Length != 20) || !content.StartsWith("00"))
            {
                throw new Exception($"AI 00 content must include the AI and 17 or 18 digits: {content}");
            }

            string body = content.Length == 20 ? content.Substring(0, 19) : content;
            return body + CalculateMod10CheckDigit(body);
        }

        private static string NormalizeAi00FieldData(string data)
        {
            if ((data.Length != 17 && data.Length != 18) || !data.All(char.IsDigit))
            {
                throw new Exception($"AI 00 field data must contain 17 or 18 digits: {data}");
            }

            string body = data.Length == 18 ? data.Substring(0, 17) : data;
            return body + CalculateMod10CheckDigit($"00{body}");
        }

        private static char CalculateMod10CheckDigit(string data)
        {
            int checksum = 0;
            int weight = 3;
            for (int i = data.Length - 1; i >= 0; i--)
            {
                checksum += (data[i] - '0') * weight;
                weight = weight == 3 ? 1 : 3;
            }

            return (char)('0' + ((10 - checksum % 10) % 10));
        }

        private static string ApplyCheckDigitToRawData(string rawData, char checkDigit, bool replaceExisting)
        {
            int trimmedLength = rawData.TrimEnd().Length;
            string trimmedData = rawData.Substring(0, trimmedLength);
            string trailingWhitespace = rawData.Substring(trimmedLength);

            if (replaceExisting)
            {
                return ReplaceLastMeaningfulCharacter(trimmedData, checkDigit) + trailingWhitespace;
            }

            return trimmedData + checkDigit + trailingWhitespace;
        }

        private static string ReplaceLastMeaningfulCharacter(string content, char replacement)
        {
            char[] chars = content.ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(chars[i]))
                {
                    chars[i] = replacement;
                    return new string(chars);
                }
            }

            return content + replacement;
        }

        private static string RemoveWhitespace(string content)
        {
            return new string(content.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
        }
    }
}
