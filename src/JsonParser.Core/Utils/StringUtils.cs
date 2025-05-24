using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonParser.Core.Utils;
public static class StringUtils
{
    public static string UnescapeString(ReadOnlySpan<char> escaped)
    {
        var result = new StringBuilder(escaped.Length);

        for (int i = 0; i < escaped.Length; i++)
        {
            char c = escaped[i];

            if (c == '\\' && i + 1 < escaped.Length)
            {
                char next = escaped[i + 1];
                switch (next)
                {
                    case '"': result.Append('"'); break;
                    case '\\': result.Append('\\'); break;
                    case '/': result.Append('/'); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u': // Unicode escape \uXXXX
                        if (i + 5 < escaped.Length)
                        {
                            var hex = escaped.Slice(i + 2, 4);
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int unicode))
                            {
                                result.Append((char)unicode);
                                i += 4; // Skip the 4 hex digits
                            }
                        }
                        break;
                    default:
                        throw new JsonException($"Invalid escape sequence: \\{next}");
                }
                i++; // Skip the escape character
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}
