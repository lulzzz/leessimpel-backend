using System.Text.Json;
using System.Text.Json.Serialization;

abstract record AppMessage;
record SectionAppMessage(string index, string title) : AppMessage;
record TextBlockAppMessage(string text, string? emoji) : AppMessage;
record TitleAppMessage(string title, bool success) : AppMessage;
    
class AppMessageJsonConverter : JsonConverter<AppMessage>
{
    public static JsonSerializerOptions Options { get; } = new() {Converters = {new AppMessageJsonConverter()}, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault};
    
    public override AppMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, AppMessage value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", value switch
        {
            SectionAppMessage => "section",
            TextBlockAppMessage => "textblock",
            TitleAppMessage => "title",
            _ => throw new NotImplementedException()
        });
        foreach (var prop in value.GetType().GetProperties())
        {
            writer.WritePropertyName(prop.Name.ToLower());
            JsonSerializer.Serialize(writer, prop.GetValue(value), options);
        }

        writer.WriteEndObject();
    }
}