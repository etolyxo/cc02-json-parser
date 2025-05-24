using JsonParser.Core.Tokenization;
using JsonParser.Core.Utils;
using System.Reflection;
using System.Globalization;
using System.Collections;

namespace JsonParser.Core.Parsing;

public class TypedJsonParser<T>
{
    private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly Dictionary<string, PropertyInfo> PropertyLookup = new();

    static TypedJsonParser()
    {
        CacheTypeProperties(typeof(T));
    }

    private static void CacheTypeProperties(Type type)
    {
        if (PropertyCache.ContainsKey(type))
            return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanWrite)
                            .ToArray();
        PropertyCache[type] = properties;

        foreach (var prop in properties)
        {
            // Support JsonPropertyName attribute
            var jsonName = prop.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name ??
                          ConvertToJsonCase(prop.Name);
            PropertyLookup[$"{type.FullName}.{jsonName}"] = prop;
        }

        // Recursively cache nested types
        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propType);
            if (underlyingType != null)
                propType = underlyingType;

            // Cache complex types
            if (propType.IsClass && propType != typeof(string))
            {
                CacheTypeProperties(propType);
            }

            // Cache generic collection element types
            if (IsGenericCollection(propType))
            {
                var elementType = GetCollectionElementType(propType);
                if (elementType != null && elementType.IsClass && elementType != typeof(string))
                {
                    CacheTypeProperties(elementType);
                }
            }
        }
    }

    private static string ConvertToJsonCase(string propertyName)
    {
        // Convert PascalCase to camelCase
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
            return propertyName;

        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }

    public T Parse(ReadOnlySpan<char> json)
    {
        var tokenizer = new JsonTokenizer(json);
        tokenizer.Read();
        return (T)ParseTypedValue(typeof(T), ref tokenizer);
    }

    public T Parse(string json)
    {
        return Parse(json.AsSpan());
    }

    private object ParseTypedValue(Type targetType, ref JsonTokenizer tokenizer)
    {
        // Handle null values
        if (tokenizer.TokenType == JsonTokenType.Null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new JsonException($"Cannot assign null to non-nullable type {targetType}");
            return null;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
            targetType = underlyingType;

        // String handling
        if (targetType == typeof(string))
            return ParseString(tokenizer.Value);

        // Numeric types handling
        if (targetType == typeof(int))
            return ParseInteger<int>(tokenizer.Value, int.TryParse);

        if (targetType == typeof(long))
            return ParseInteger<long>(tokenizer.Value, long.TryParse);

        if (targetType == typeof(short))
            return ParseInteger<short>(tokenizer.Value, short.TryParse);

        if (targetType == typeof(byte))
            return ParseInteger<byte>(tokenizer.Value, byte.TryParse);

        if (targetType == typeof(uint))
            return ParseInteger<uint>(tokenizer.Value, uint.TryParse);

        if (targetType == typeof(ulong))
            return ParseInteger<ulong>(tokenizer.Value, ulong.TryParse);

        if (targetType == typeof(ushort))
            return ParseInteger<ushort>(tokenizer.Value, ushort.TryParse);

        if (targetType == typeof(sbyte))
            return ParseInteger<sbyte>(tokenizer.Value, sbyte.TryParse);

        if (targetType == typeof(double))
            return ParseFloat<double>(tokenizer.Value, double.TryParse);

        if (targetType == typeof(float))
            return ParseFloat<float>(tokenizer.Value, float.TryParse);

        if (targetType == typeof(decimal))
            return ParseFloat<decimal>(tokenizer.Value, decimal.TryParse);

        if (targetType == typeof(bool))
            return ParseBoolean(tokenizer.Value);

        // Date/Time handling
        if (targetType == typeof(DateTime))
            return ParseDateTime(tokenizer.Value.ToString());

        if (targetType == typeof(DateOnly))
            return ParseDateOnly(tokenizer.Value.ToString());

        if (targetType == typeof(DateTimeOffset))
            return ParseDateTimeOffset(tokenizer.Value.ToString());

        if (targetType == typeof(TimeOnly))
            return ParseTimeOnly(tokenizer.Value.ToString());


        // GUID handling
        if (targetType == typeof(Guid))
            return ParseGuid(tokenizer.Value.ToString());

        // Enum handling
        if (targetType.IsEnum)
            return ParseEnum(targetType, tokenizer.Value.ToString());

        // Array and collection handling
        if (tokenizer.TokenType == JsonTokenType.StartArray)
        {
            if (targetType.IsArray)
                return ParseArray(targetType, ref tokenizer);

            if (IsGenericCollection(targetType))
                return ParseCollection(targetType, ref tokenizer);
        }

        // Object handling
        if (tokenizer.TokenType == JsonTokenType.StartObject && targetType.IsClass)
            return ParseObject(targetType, ref tokenizer);

        throw new JsonException($"Cannot convert JSON token {tokenizer.TokenType} to type {targetType}");
    }

    private string ParseString(ReadOnlySpan<char> value)
    {
        // No escape sequences
        if (!value.Contains('\\'))
        {
            return value.ToString();
        }
        // Handle escape sequences using the same logic as the native parser
        return StringUtils.UnescapeString(value);
    }

    private T ParseInteger<T>(ReadOnlySpan<char> value, TryParseDelegate<T> tryParse)
        where T : struct
    {
        if (tryParse(value, out T result))
            return result;

        throw new JsonException($"Invalid integer: {value.ToString()}");
    }

    private T ParseFloat<T>(ReadOnlySpan<char> value, TryParseDelegate<T> tryParse)
        where T : struct
    {
        if (tryParse(value, out T result))
            return result;

        throw new JsonException($"Invalid float: {value.ToString()}");
    }

    private bool ParseBoolean(ReadOnlySpan<char> value)
    {
        if (value.SequenceEqual("true".AsSpan()))
            return true;

        if (value.SequenceEqual("false".AsSpan()))
            return false;

        throw new JsonException($"Invalid boolean: {value.ToString()}");
    }

    private object ParseEnum(Type enumType, string value)
    {
        // Try parsing as string first
        if (Enum.TryParse(enumType, value, true, out object enumValue))
            return enumValue;

        // Try parsing as numeric value
        if (int.TryParse(value, out int intValue))
        {
            if (Enum.IsDefined(enumType, intValue))
                return Enum.ToObject(enumType, intValue);
        }

        throw new JsonException($"Unable to parse '{value}' as {enumType.Name}");
    }

    private Array ParseArray(Type arrayType, ref JsonTokenizer tokenizer)
    {
        var elementType = arrayType.GetElementType();
        var list = new List<object>();

        tokenizer.Read(); // Skip StartArray '['

        // Handle empty array []
        if (tokenizer.TokenType == JsonTokenType.EndArray)
        {
            var emptyArray = Array.CreateInstance(elementType, 0);
            return emptyArray;
        }

        // Parse array elements
        while (true)
        {
            object value = ParseTypedValue(elementType, ref tokenizer);
            list.Add(value);

            tokenizer.Read(); // Move to next token

            if (tokenizer.TokenType == JsonTokenType.EndArray)
                break;

            if (tokenizer.TokenType != JsonTokenType.Comma)
                throw new JsonException("Expected ',' or ']' in array");

            tokenizer.Read(); // Skip comma, move to next value
        }

        // Convert list to typed array
        var typedArray = Array.CreateInstance(elementType, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            typedArray.SetValue(list[i], i);
        }

        return typedArray;
    }

    private object ParseCollection(Type collectionType, ref JsonTokenizer tokenizer)
    {
        var elementType = GetCollectionElementType(collectionType);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType);

        tokenizer.Read(); // Skip StartArray '['

        // Handle empty array []
        if (tokenizer.TokenType == JsonTokenType.EndArray)
        {
            // Convert to target collection type if needed
            return ConvertToTargetCollection(list, collectionType);
        }

        // Parse array elements
        while (true)
        {
            object value = ParseTypedValue(elementType, ref tokenizer);
            list.Add(value);

            tokenizer.Read(); // Move to next token

            if (tokenizer.TokenType == JsonTokenType.EndArray)
                break;

            if (tokenizer.TokenType != JsonTokenType.Comma)
                throw new JsonException("Expected ',' or ']' in array");

            tokenizer.Read(); // Skip comma, move to next value
        }

        return ConvertToTargetCollection(list, collectionType);
    }

    private object ParseObject(Type type, ref JsonTokenizer tokenizer)
    {
        var instance = Activator.CreateInstance(type);

        if (!PropertyCache.TryGetValue(type, out var properties))
            CacheTypeProperties(type);

        tokenizer.Read(); // Skip StartObject '{'

        // Handle empty object {}
        if (tokenizer.TokenType == JsonTokenType.EndObject)
        {
            return instance;
        }

        while (true)
        {
            if (tokenizer.TokenType != JsonTokenType.String)
                throw new JsonException("Expected property name");

            string propertyName = ParseString(tokenizer.Value);
            string lookupKey = $"{type.FullName}.{propertyName}";

            tokenizer.Read(); // Move to colon

            if (tokenizer.TokenType != JsonTokenType.Colon)
                throw new JsonException($"Expected ':' after property name '{propertyName}'");

            tokenizer.Read(); // Move to value

            if (PropertyLookup.TryGetValue(lookupKey, out PropertyInfo property))
            {
                object value = ParseTypedValue(property.PropertyType, ref tokenizer);
                property.SetValue(instance, value);
            }
            else
            {
                // Skip unknown property
                SkipValue(ref tokenizer);
            }

            tokenizer.Read(); // Move past value

            if (tokenizer.TokenType == JsonTokenType.EndObject)
                break;

            if (tokenizer.TokenType != JsonTokenType.Comma)
                throw new JsonException("Expected ',' or '}' in object");

            tokenizer.Read(); // Skip comma, move to next property
        }

        return instance;
    }

    private void SkipValue(ref JsonTokenizer tokenizer)
    {
        switch (tokenizer.TokenType)
        {
            case JsonTokenType.String:
            case JsonTokenType.Integer:
            case JsonTokenType.Float:
            case JsonTokenType.Boolean:
            case JsonTokenType.Null:
                // Simple values - already positioned correctly, nothing to skip
                break;

            case JsonTokenType.StartObject:
                SkipObject(ref tokenizer);
                break;

            case JsonTokenType.StartArray:
                SkipArray(ref tokenizer);
                break;

            default:
                throw new JsonException($"Unexpected token type {tokenizer.TokenType} when skipping value");
        }
    }

    private void SkipObject(ref JsonTokenizer tokenizer)
    {
        int depth = 1; // We're already at StartObject
        tokenizer.Read(); // Move past StartObject

        while (depth > 0)
        {
            if (tokenizer.TokenType == JsonTokenType.EndOfJson)
                throw new JsonException("Unexpected end of JSON while skipping object");

            switch (tokenizer.TokenType)
            {
                case JsonTokenType.StartObject:
                    depth++;
                    break;
                case JsonTokenType.EndObject:
                    depth--;
                    break;
            }

            if (depth > 0)
                tokenizer.Read();
        }
    }

    private void SkipArray(ref JsonTokenizer tokenizer)
    {
        int depth = 1; // We're already at StartArray
        tokenizer.Read(); // Move past StartArray

        while (depth > 0)
        {
            if (tokenizer.TokenType == JsonTokenType.EndOfJson)
                throw new JsonException("Unexpected end of JSON while skipping array");

            switch (tokenizer.TokenType)
            {
                case JsonTokenType.StartArray:
                    depth++;
                    break;
                case JsonTokenType.EndArray:
                    depth--;
                    break;
            }

            if (depth > 0)
                tokenizer.Read();
        }
    }

    private static bool IsGenericCollection(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(List<>) ||
               genericType == typeof(IList<>) ||
               genericType == typeof(ICollection<>) ||
               genericType == typeof(IEnumerable<>) ||
               genericType == typeof(HashSet<>) ||
               genericType == typeof(ISet<>);
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
            return collectionType.GetGenericArguments()[0];

        return typeof(object);
    }

    private static object ConvertToTargetCollection(IList list, Type targetType)
    {
        if (targetType.IsAssignableFrom(list.GetType()))
            return list;

        // Handle common collection conversions
        if (targetType.IsGenericType)
        {
            var genericType = targetType.GetGenericTypeDefinition();
            var elementType = targetType.GetGenericArguments()[0];

            if (genericType == typeof(HashSet<>) || genericType == typeof(ISet<>))
            {
                var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
                var hashSet = Activator.CreateInstance(hashSetType);
                var addMethod = hashSetType.GetMethod("Add");

                foreach (var item in list)
                {
                    addMethod.Invoke(hashSet, new[] { item });
                }

                return hashSet;
            }
        }

        return list;
    }

    #region Date/Time Parsing Methods

    private DateTime ParseDateTime(string value)
    {
        string[] formats = {
            "yyyy-MM-ddTHH:mm:ss.fffZ",      // ISO 8601 with milliseconds UTC
            "yyyy-MM-ddTHH:mm:ssZ",          // ISO 8601 UTC
            "yyyy-MM-ddTHH:mm:ss.fff",       // ISO 8601 with milliseconds local
            "yyyy-MM-ddTHH:mm:ss",           // ISO 8601 local
            "yyyy-MM-dd HH:mm:ss",           // SQL-style format
            "yyyy-MM-dd",                    // Date only
            "MM/dd/yyyy HH:mm:ss",           // US format with time
            "MM/dd/yyyy"                     // US date format
        };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                                  DateTimeStyles.RoundtripKind, out DateTime result))
        {
            return result;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' as DateTime");
    }

    private DateOnly ParseDateOnly(string value)
    {
        string[] formats = {
            "yyyy-MM-dd",                    // ISO 8601 date
            "MM/dd/yyyy",                    // US format
            "dd/MM/yyyy",                    // European format
            "yyyy/MM/dd"                     // Alternative format
        };

        if (DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out DateOnly result))
        {
            return result;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' as DateOnly");
    }

    private TimeOnly ParseTimeOnly(string value)
    {
        string[] formats = {
            "HH:mm:ss.fff",                  // Time with milliseconds
            "HH:mm:ss",                      // Time without milliseconds
            "HH:mm"                          // Time without seconds
        };

        if (TimeOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out TimeOnly result))
        {
            return result;
        }

        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' as TimeOnly");
    }

    private DateTimeOffset ParseDateTimeOffset(string value)
    {
        string[] formats = {
            "yyyy-MM-ddTHH:mm:ss.fffzzz",    // ISO 8601 with milliseconds and timezone
            "yyyy-MM-ddTHH:mm:sszzz",        // ISO 8601 with timezone
            "yyyy-MM-ddTHH:mm:ss.fffZ",      // ISO 8601 with milliseconds UTC
            "yyyy-MM-ddTHH:mm:ssZ",          // ISO 8601 UTC
            "yyyy-MM-ddTHH:mm:ss.fff",       // ISO 8601 with milliseconds (assumes local)
            "yyyy-MM-ddTHH:mm:ss"            // ISO 8601 (assumes local)
        };

        if (DateTimeOffset.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                                        DateTimeStyles.RoundtripKind, out DateTimeOffset result))
        {
            return result;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' as DateTimeOffset");
    }

    private Guid ParseGuid(string value)
    {
        if (Guid.TryParse(value, out Guid result))
        {
            return result;
        }
        throw new JsonException($"Unable to parse '{value}' as GUID");
    }

    #endregion

    // Delegate for TryParse methods
    private delegate bool TryParseDelegate<T>(ReadOnlySpan<char> s, out T result);
}