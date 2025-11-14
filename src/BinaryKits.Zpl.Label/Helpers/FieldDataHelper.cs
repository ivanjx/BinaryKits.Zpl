using System.Text;

namespace BinaryKits.Zpl.Label.Helpers;

public static class FieldDataHelper
{
    public static string RenderFieldDataSection(
        this string text,
        char? hexadecimalIndicator,
        bool reversePrint = false,
        NewLineConversionMethod newLineConversionMethod = default)
    {
        StringBuilder sb = new();

        if (hexadecimalIndicator == default)
        {
            hexadecimalIndicator = '_';
        }

        if (reversePrint)
        {
            sb.Append("^FR");
        }

        bool requiresFh = false;

        if (text != null)
        {
            sb.Append("^FD");

            foreach (char c in text)
            {
                string s = c.SanitizeCharacter(
                    newLineConversionMethod,
                    hexadecimalIndicator);

                if (s.Length > 1 &&
                    hexadecimalIndicator != null &&
                    s.StartsWith(hexadecimalIndicator.Value))
                {
                    requiresFh = true;
                }
                
                sb.Append(s);
            }

            sb.Append("^FS");
        }

        if (requiresFh)
        {
            sb.Insert(0, "^FH");
        }

        return sb.ToString();
    }

    public static string SanitizeCharacter(
        this char input,
        NewLineConversionMethod newLineConversion = NewLineConversionMethod.ToSpace,
        char? hexadecimalIndicator = null)
    {
        switch (input)
        {
            case '^':
            case '~':
            case '\\':
                return " ";
        }

        if (input == '\n')
        {
            switch (newLineConversion)
            {
                case NewLineConversionMethod.ToEmpty:
                    return "";
                case NewLineConversionMethod.ToSpace:
                    return " ";
                case NewLineConversionMethod.ToZplNewLine:
                    return @"\&";
            }
        }
        
        if (input > 127 ||
            input < 32)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(new[] { input });
            StringBuilder hexBuilder = new();
            foreach (byte b in bytes)
            {
                hexBuilder.Append(hexadecimalIndicator);
                hexBuilder.Append(b.ToString("X2"));
            }
            return hexBuilder.ToString();
        }

        return input.ToString();
    }
}
