using JsonParser.Core;
using JsonParser.Core.Utils;
namespace JsonParser.Core.Tests;
public class StringUtilsTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData("no escapes here", "no escapes here")]
    public void UnescapeString_NoEscapes_ReturnsOriginal(string input, string expected)
    {
        var result = StringUtils.UnescapeString(input.AsSpan());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello\\nworld", "hello\nworld")]
    [InlineData("hello\\\"world", "hello\"world")]
    [InlineData("hello\\\\world", "hello\\world")]
    [InlineData("hello\\/world", "hello/world")]
    [InlineData("hello\\bworld", "hello\bworld")]
    [InlineData("hello\\fworld", "hello\fworld")]
    [InlineData("hello\\rworld", "hello\rworld")]
    [InlineData("hello\\tworld", "hello\tworld")]
    public void UnescapeString_BasicEscapes_UnescapesCorrectly(string input, string expected)
    {
        var result = StringUtils.UnescapeString(input.AsSpan());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello\\u0041world", "helloAworld")] // A
    [InlineData("\\u0048\\u0065\\u006C\\u006C\\u006F", "Hello")]
    [InlineData("test\\u0020string", "test string")] // space
    public void UnescapeString_UnicodeEscapes_UnescapesCorrectly(string input, string expected)
    {
        var result = StringUtils.UnescapeString(input.AsSpan());
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid\\x")]
    [InlineData("invalid\\q")]
    public void UnescapeString_InvalidEscape_ThrowsException(string input)
    {
        Assert.Throws<JsonException>(() => StringUtils.UnescapeString(input.AsSpan()));
    }
}
