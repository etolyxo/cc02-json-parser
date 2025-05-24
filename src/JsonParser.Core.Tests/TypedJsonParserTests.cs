using JsonParser.Core.Tests.Models;
using JsonParser.Core;
using JsonParser.Core.Parsing;
namespace JsonParser.Core.Tests;
public class TypedJsonParserTests
{
    [Fact]
    public void Parse_SimpleObject_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<Person>();
        var json = """{"name": "John", "age": 30, "isActive": true}""";

        var result = parser.Parse(json);

        Assert.Equal("John", result.Name);
        Assert.Equal(30, result.Age);
        Assert.True(result.IsActive);
    }

    [Fact]
    public void Parse_EmptyObject_CreatesInstance()
    {
        var parser = new TypedJsonParser<Person>();
        var json = "{}";

        var result = parser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Name);
        Assert.Equal(0, result.Age);
        Assert.False(result.IsActive);
    }

    [Fact]
    public void Parse_NestedObject_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<Person>();
        var json = """
        {
            "name": "John",
            "age": 30,
            "address": {
                "street": "123 Main St",
                "city": "Anytown",
                "zipCode": 12345
            }
        }
        """;

        var result = parser.Parse(json);

        Assert.Equal("John", result.Name);
        Assert.Equal(30, result.Age);
        Assert.NotNull(result.Address);
        Assert.Equal("123 Main St", result.Address.Street);
        Assert.Equal("Anytown", result.Address.City);
        Assert.Equal(12345, result.Address.ZipCode);
    }

    [Fact]
    public void Parse_Array_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<Person[]>();
        var json = """[{"name": "John", "age": 30}, {"name": "Jane", "age": 25}]""";

        var result = parser.Parse(json);

        Assert.Equal(2, result.Length);
        Assert.Equal("John", result[0].Name);
        Assert.Equal(30, result[0].Age);
        Assert.Equal("Jane", result[1].Name);
        Assert.Equal(25, result[1].Age);
    }

    [Fact]
    public void Parse_List_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<List<Person>>();
        var json = """[{"name": "John", "age": 30}, {"name": "Jane", "age": 25}]""";

        var result = parser.Parse(json);

        Assert.Equal(2, result.Count);
        Assert.Equal("John", result[0].Name);
        Assert.Equal(30, result[0].Age);
        Assert.Equal("Jane", result[1].Name);
        Assert.Equal(25, result[1].Age);
    }

    [Fact]
    public void Parse_EmptyArray_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<Person[]>();
        var json = "[]";

        var result = parser.Parse(json);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("-456", -456)]
    [InlineData("0", 0)]
    public void Parse_Integer_ParsesCorrectly(string json, int expected)
    {
        var parser = new TypedJsonParser<int>();

        var result = parser.Parse(json);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123.45", 123.45)]
    [InlineData("-123.45", -123.45)]
    [InlineData("1.23e2", 123.0)]
    public void Parse_Double_ParsesCorrectly(string json, double expected)
    {
        var parser = new TypedJsonParser<double>();

        var result = parser.Parse(json);

        Assert.Equal(expected, result, 0.0001);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Parse_Boolean_ParsesCorrectly(string json, bool expected)
    {
        var parser = new TypedJsonParser<bool>();

        var result = parser.Parse(json);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_String_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<string>();
        var json = "\"Hello World\"";

        var result = parser.Parse(json);

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Parse_Null_ReturnsNull()
    {
        var parser = new TypedJsonParser<string>();
        var json = "null";

        var result = parser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_NullToValueType_ThrowsException()
    {
        var parser = new TypedJsonParser<int>();
        var json = "null";

        Assert.Throws<JsonException>(() => parser.Parse(json));
    }

    [Fact]
    public void Parse_NullableValueType_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<int?>();
        var json = "null";

        var result = parser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_Enum_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<PersonWithEnum>();
        var json = """{"name": "John", "status": "Active"}""";

        var result = parser.Parse(json);

        Assert.Equal("John", result.Name);
        Assert.Equal(Status.Active, result.Status);
    }

    [Fact]
    public void Parse_EnumAsNumber_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<PersonWithEnum>();
        var json = """{"name": "John", "status": 1}""";

        var result = parser.Parse(json);

        Assert.Equal("John", result.Name);
        Assert.Equal(Status.Inactive, result.Status);
    }

    [Fact]
    public void Parse_JsonPropertyName_UsesCustomName()
    {
        var parser = new TypedJsonParser<PersonWithJsonNames>();
        var json = """{"full_name": "John Doe", "years_old": 30}""";

        var result = parser.Parse(json);

        Assert.Equal("John Doe", result.FullName);
        Assert.Equal(30, result.YearsOld);
    }

    [Fact]
    public void Parse_UnknownProperty_IgnoresProperty()
    {
        var parser = new TypedJsonParser<Person>();
        var json = """{"name": "John", "age": 30, "unknownProperty": "value"}""";

        var result = parser.Parse(json);

        Assert.Equal("John", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void Parse_InvalidJson_ThrowsException()
    {
        var parser = new TypedJsonParser<Person>();
        var json = "{invalid json}";

        Assert.Throws<JsonException>(() => parser.Parse(json));
    }

    [Fact]
    public void Parse_MismatchedType_ThrowsException()
    {
        var parser = new TypedJsonParser<int>();
        var json = "\"not a number\"";

        Assert.Throws<JsonException>(() => parser.Parse(json));
    }

    [Fact]
    public void Parse_HashSet_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<HashSet<string>>();
        var json = """["apple", "banana", "cherry"]""";

        var result = parser.Parse(json);

        Assert.Equal(3, result.Count);
        Assert.Contains("apple", result);
        Assert.Contains("banana", result);
        Assert.Contains("cherry", result);
    }

    [Fact]
    public void Parse_DateTime_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<DateTime>();
        var json = "\"2023-12-25T10:30:00Z\"";

        var result = parser.Parse(json);

        Assert.Equal(new DateTime(2023, 12, 25, 10, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void Parse_Guid_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<Guid>();
        var expectedGuid = Guid.NewGuid();
        var json = $"\"{expectedGuid}\"";

        var result = parser.Parse(json);

        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public void Parse_StringCollection_ParsesCorrectly()
    {
        var parser = new TypedJsonParser<Person>();
        var json = """
        {
            "name": "John",
            "hobbies": ["reading", "gaming", "cooking"]
        }
        """;

        var result = parser.Parse(json);

        Assert.Equal("John", result.Name);
        Assert.Equal(3, result.Hobbies.Count);
        Assert.Contains("reading", result.Hobbies);
        Assert.Contains("gaming", result.Hobbies);
        Assert.Contains("cooking", result.Hobbies);
    }
}
