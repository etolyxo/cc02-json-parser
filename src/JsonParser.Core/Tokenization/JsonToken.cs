namespace JsonParser.Core.Tokenization;

public enum JsonTokenType
{
    None,
    StartObject,     // {
    EndObject,       // }
    StartArray,      // [
    EndArray,        // ]
    PropertyName,    // "key"
    String,          // "value"
    Integer,         // 123
    Float,           // 123.45
    Boolean,         // true/false
    Null,            // null
    Colon,           // :
    Comma,           // ,
    EndOfJson        // End of input
}

