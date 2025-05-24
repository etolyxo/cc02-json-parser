using JsonParser.Core;
using JsonParser.Core.Tokenization;
namespace JsonParser.Core.Tests;

public class JsonTokenizerTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var json = "{}".AsSpan();
        var tokenizer = new JsonTokenizer(json);

        Assert.Equal(JsonTokenType.None, tokenizer.TokenType);
        Assert.True(tokenizer.Value.IsEmpty);
    }

    [Theory]
    [InlineData("{", JsonTokenType.StartObject)]
    [InlineData("}", JsonTokenType.EndObject)]
    [InlineData("[", JsonTokenType.StartArray)]
    [InlineData("]", JsonTokenType.EndArray)]
    [InlineData(":", JsonTokenType.Colon)]
    [InlineData(",", JsonTokenType.Comma)]
    public void ReadSingleChar_ParsesCorrectly(string input, JsonTokenType expectedType)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(expectedType, tokenizer.TokenType);
        Assert.Equal(input, tokenizer.Value.ToString());
    }

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"\"", "")]
    [InlineData("\"hello world\"", "hello world")]
    public void ReadString_ParsesCorrectly(string input, string expected)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(JsonTokenType.String, tokenizer.TokenType);
        Assert.Equal(expected, tokenizer.Value.ToString());
    }

    [Theory]
    [InlineData("\"hello\\nworld\"")]
    [InlineData("\"hello\\\"world\"")]
    [InlineData("\"hello\\\\world\"")]
    [InlineData("\"hello\\u0041world\"")]
    public void ReadString_WithEscapes_ParsesCorrectly(string input)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(JsonTokenType.String, tokenizer.TokenType);
    }

    [Theory]
    [InlineData("\"unterminated")]
    [InlineData("\"invalid\\q\"")]
    [InlineData("\"invalid\\u12\"")]
    [InlineData("\"control\u0001char\"")]
    public void ReadString_InvalidStrings_ThrowsException(string input)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());
        try
        {
            tokenizer.Read();
            Assert.Fail("Expected JsonException was not thrown.");
        }
        catch (JsonException)
        {
            // Test passes, expected exception was thrown.
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected JsonException, but got {ex.GetType().Name}.");
        }
    }

    [Theory]
    [InlineData("123", JsonTokenType.Integer, "123")]
    [InlineData("-456", JsonTokenType.Integer, "-456")]
    [InlineData("0", JsonTokenType.Integer, "0")]
    [InlineData("123.45", JsonTokenType.Float, "123.45")]
    [InlineData("-123.45", JsonTokenType.Float, "-123.45")]
    [InlineData("1.23e10", JsonTokenType.Float, "1.23e10")]
    [InlineData("1.23E-5", JsonTokenType.Float, "1.23E-5")]
    public void ReadNumber_ParsesCorrectly(string input, JsonTokenType expectedType, string expectedValue)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(expectedType, tokenizer.TokenType);
        Assert.Equal(expectedValue, tokenizer.Value.ToString());
    }

    [Theory]
    [InlineData("01")]
    [InlineData("-")]
    [InlineData("1.")]
    [InlineData("1e")]
    public void ReadNumber_InvalidNumbers_ThrowsException(string input)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());
        try
        {
            tokenizer.Read();
            Assert.Fail("Expected JsonException was not thrown.");
        }
        catch (JsonException)
        {
            // Test passes, expected exception was thrown.
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected JsonException, but got {ex.GetType().Name}.");
        }
    }

    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    public void ReadBoolean_ParsesCorrectly(string input, string expected)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(JsonTokenType.Boolean, tokenizer.TokenType);
        Assert.Equal(expected, tokenizer.Value.ToString());
    }

    [Theory]
    [InlineData("tru")]
    [InlineData("fals")]
    [InlineData("True")]
    public void ReadBoolean_InvalidBooleans_ThrowsException(string input)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());
        try
        {
            tokenizer.Read();
            Assert.Fail("Expected JsonException was not thrown.");
        }
        catch (JsonException)
        {
            // Test passes, expected exception was thrown.
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected JsonException, but got {ex.GetType().Name}.");
        }
    }

    [Fact]
    public void ReadNull_ParsesCorrectly()
    {
        var tokenizer = new JsonTokenizer("null".AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(JsonTokenType.Null, tokenizer.TokenType);
        Assert.Equal("null", tokenizer.Value.ToString());
    }

    [Theory]
    [InlineData("nul")]
    [InlineData("Null")]
    [InlineData("NULL")]
    public void ReadNull_InvalidNull_ThrowsException(string input)
    {
        var tokenizer = new JsonTokenizer(input.AsSpan());
        try
        {
            tokenizer.Read();
            Assert.Fail("Expected JsonException was not thrown.");
        }
        catch (JsonException)
        {
            // Test passes, expected exception was thrown.
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected JsonException, but got {ex.GetType().Name}.");
        }
    }

    [Fact]
    public void SkipWhitespace_SkipsAllWhitespace()
    {
        var tokenizer = new JsonTokenizer("   \t\n\r  {".AsSpan());

        Assert.True(tokenizer.Read());
        Assert.Equal(JsonTokenType.StartObject, tokenizer.TokenType);
    }

    [Fact]
    public void PeekChar_ReturnsCurrentCharWithoutAdvancing()
    {
        var tokenizer = new JsonTokenizer("{".AsSpan());

        Assert.Equal('{', tokenizer.PeekChar());
        Assert.Equal('{', tokenizer.PeekChar()); // Should return same char
    }

    [Fact]
    public void IsAtEnd_ReturnsTrueWhenAtEnd()
    {
        var tokenizer = new JsonTokenizer("".AsSpan());

        Assert.True(tokenizer.IsAtEnd());
    }

    [Fact]
    public void Read_AtEndOfJson_ReturnsFalse()
    {
        var tokenizer = new JsonTokenizer("{}".AsSpan());

        tokenizer.Read(); // {
        tokenizer.Read(); // }

        Assert.False(tokenizer.Read()); // Should return false at end
        Assert.Equal(JsonTokenType.EndOfJson, tokenizer.TokenType);
    }

    [Fact]
    public void Read_UnexpectedCharacter_ThrowsException()
    {
        var tokenizer = new JsonTokenizer("@".AsSpan());
        try
        {
            tokenizer.Read();
            Assert.Fail("Expected JsonException was not thrown.");
        }
        catch (JsonException)
        {
            // Test passes, expected exception was thrown.
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected JsonException, but got {ex.GetType().Name}.");
        }
    }
}
