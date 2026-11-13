using System.Text.Json;
using System.Text.Json.Serialization;
sealed class CountingProbeConverter : JsonConverter<ConverterProbe>
{
    public CountingProbeConverter() { }
    public override ConverterProbe Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>throw new NotSupportedException();
    public override void Write(Utf8JsonWriter writer,ConverterProbe value,JsonSerializerOptions options)
    {
        value.Writes++;
        if(value.Fail)throw new InvalidOperationException("controlled-converter-failure");
        writer.WriteStartObject();writer.WriteNumber("z",1);writer.WriteNumber("a",2);writer.WriteNumber("a",3);writer.WriteString("é😀","<>&\"\n");writer.WriteEndObject();
    }
}
