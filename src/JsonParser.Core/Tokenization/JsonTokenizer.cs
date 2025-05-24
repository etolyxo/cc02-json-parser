namespace JsonParser.Core.Tokenization;


public ref struct JsonTokenizer
{
    private ReadOnlySpan<char> _json;
    private int _position;
    private JsonTokenType _tokenType;
    private ReadOnlySpan<char> _value;

    public JsonTokenType TokenType => _tokenType;
    public ReadOnlySpan<char> Value => _value;

    public JsonTokenizer(ReadOnlySpan<char> json)
    {
        _json = json;
        _position = 0;
        _tokenType = JsonTokenType.None;
        _value = default;
    }

    private void SkipWhitespace()
    {
        while (_position < _json.Length && char.IsWhiteSpace(_json[_position]))
            _position++;
    }

    private bool ReadSingleChar(JsonTokenType tokenType)
    {
        _tokenType = tokenType;
        _value = _json.Slice(_position, 1);
        _position++;
        return true;
    }

    public bool Read()
    {
        SkipWhitespace();

        if (_position >= _json.Length)
        {
            _tokenType = JsonTokenType.EndOfJson;
            _value = default;
            return false;
        }

        char current = _json[_position];

        return current switch
        {
            '{' => ReadSingleChar(JsonTokenType.StartObject),
            '}' => ReadSingleChar(JsonTokenType.EndObject),
            '[' => ReadSingleChar(JsonTokenType.StartArray),
            ']' => ReadSingleChar(JsonTokenType.EndArray),
            ':' => ReadSingleChar(JsonTokenType.Colon),
            ',' => ReadSingleChar(JsonTokenType.Comma),
            '"' => ReadString(),
            't' or 'f' => ReadBoolean(),
            'n' => ReadNull(),
            >= '0' and <= '9' or '-' => ReadNumber(),
            _ => throw new JsonException($"Unexpected character: '{current}' at position {_position}")
        };
    }

    private bool ReadNull()
    {
        if (_position + 4 <= _json.Length && _json.Slice(_position, 4).SequenceEqual("null".AsSpan()))
        {
            _value = _json.Slice(_position, 4);
            _position += 4;
            _tokenType = JsonTokenType.Null;
            return true;
        }

        throw new JsonException($"Invalid null value starting at position {_position}");
    }

    private bool ReadBoolean()
    {
        if (_position + 4 <= _json.Length && _json.Slice(_position, 4).SequenceEqual("true".AsSpan()))
        {
            _value = _json.Slice(_position, 4);
            _position += 4;
            _tokenType = JsonTokenType.Boolean;
            return true;
        }

        if (_position + 5 <= _json.Length && _json.Slice(_position, 5).SequenceEqual("false".AsSpan()))
        {
            _value = _json.Slice(_position, 5);
            _position += 5;
            _tokenType = JsonTokenType.Boolean;
            return true;
        }

        throw new JsonException($"Invalid boolean value starting at position {_position}");
    }

    private bool ReadNumber()
    {
        int start = _position;
        bool isFloat = false;

        // Handle negative sign
        if (_json[_position] == '-')
        {
            _position++;

            // Must have at least one digit after minus
            if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                throw new JsonException($"Invalid number: missing digits after '-' at position {start}");
        }

        // Must start with a digit (after optional minus)
        if (_position >= _json.Length || !char.IsDigit(_json[_position]))
            throw new JsonException($"Invalid number: expected digit at position {_position}");

        // Handle leading zero (JSON doesn't allow 01, 02, etc.)
        if (_json[_position] == '0')
        {
            _position++;

            // If next char is digit, that's invalid (leading zeros not allowed)
            if (_position < _json.Length && char.IsDigit(_json[_position]))
                throw new JsonException($"Invalid number: leading zeros not allowed at position {start}");
        }
        else
        {
            // Read remaining digits for non-zero numbers
            while (_position < _json.Length && char.IsDigit(_json[_position]))
                _position++;
        }

        // Check for decimal point
        if (_position < _json.Length && _json[_position] == '.')
        {
            isFloat = true;
            _position++;

            // Must have at least one digit after decimal point
            if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                throw new JsonException($"Invalid number: missing digits after decimal point at position {_position - 1}");

            while (_position < _json.Length && char.IsDigit(_json[_position]))
                _position++;
        }

        // Check for exponent
        if (_position < _json.Length && (_json[_position] == 'e' || _json[_position] == 'E'))
        {
            isFloat = true;
            _position++;

            // Optional sign
            if (_position < _json.Length && (_json[_position] == '+' || _json[_position] == '-'))
                _position++;

            // Must have at least one digit in exponent
            if (_position >= _json.Length || !char.IsDigit(_json[_position]))
                throw new JsonException($"Invalid number: missing digits in exponent at position {_position}");

            while (_position < _json.Length && char.IsDigit(_json[_position]))
                _position++;
        }

        _value = _json[start.._position];
        _tokenType = isFloat ? JsonTokenType.Float : JsonTokenType.Integer;
        return true;
    }

    private bool ReadString()
    {
        int start = ++_position; // Skip opening quote

        while (_position < _json.Length)
        {
            char c = _json[_position];

            if (c == '"')
            {
                _value = _json[start.._position];
                _position++; // Skip closing quote
                _tokenType = JsonTokenType.String;
                return true;
            }

            if (c == '\\')
            {
                _position++; // Skip backslash

                // Validate escape sequence
                if (_position >= _json.Length)
                    throw new JsonException($"Unterminated escape sequence at position {_position - 1}");

                char escaped = _json[_position];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/':
                    case 'b':
                    case 'f':
                    case 'n':
                    case 'r':
                    case 't':
                        break;
                    case 'u':
                        // Unicode escape: \uXXXX
                        if (_position + 4 >= _json.Length)
                            throw new JsonException($"Incomplete unicode escape sequence at position {_position - 1}");

                        for (int i = 1; i <= 4; i++)
                        {
                            char hex = _json[_position + i];
                            if (!IsHexDigit(hex))
                                throw new JsonException($"Invalid unicode escape sequence at position {_position - 1}");
                        }
                        _position += 4; // Skip the 4 hex digits
                        break;
                    default:
                        throw new JsonException($"Invalid escape sequence '\\{escaped}' at position {_position - 1}");
                }
            }
            else if (c < 0x20) // Control characters (except tab) are not allowed in JSON strings
            {
                throw new JsonException($"Invalid control character in string at position {_position}");
            }

            _position++;
        }

        throw new JsonException($"Unterminated string starting at position {start - 1}");
    }

    private static bool IsHexDigit(char c)
    {
        return (c >= '0' && c <= '9') ||
               (c >= 'A' && c <= 'F') ||
               (c >= 'a' && c <= 'f');
    }

    // Helper method to peek at the current character without advancing
    public char PeekChar()
    {
        SkipWhitespace();
        return _position < _json.Length ? _json[_position] : '\0';
    }

    // Helper method to check if we're at the end
    public bool IsAtEnd()
    {
        SkipWhitespace();
        return _position >= _json.Length;
    }
}