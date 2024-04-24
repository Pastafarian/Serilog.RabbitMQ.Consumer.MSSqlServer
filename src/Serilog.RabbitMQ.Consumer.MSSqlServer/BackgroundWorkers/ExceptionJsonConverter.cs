using System.Text.Json;
using System.Text.Json.Serialization;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

public class ExceptionJsonConverter : JsonConverter<Exception>
{
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? message = null;
        string? stackTrace = null;
        string? source = null;
        Exception? innerException = null;
        string? type = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (options.PropertyNameCaseInsensitive ? propertyName : propertyName?.ToLowerInvariant())
                {
                    case "Type":
                    case "type":
                        type = reader.GetString();
                        break;

                    case "Message":
                    case "message":
                        message = reader.GetString();
                        break;

                    case "Source":
                    case "source":
                        source = reader.GetString();
                        break;

                    case "StackTrace":
                    case "stackTrace":
                    case "stacktrace":
                        stackTrace = reader.GetString();
                        break;

                    case "InnerException":
                    case "innerException":
                    case "innerexception":
                        innerException = JsonSerializer.Deserialize<Exception>(ref reader, options);
                        break;
                }
            }
        }

        type ??= typeof(Exception).AssemblyQualifiedName!;

        return new JsonSerializedException(message, source, stackTrace, innerException);
    }

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        string ConvertName(string name)
            => options.PropertyNamingPolicy?.ConvertName(name) ?? name;

        var ignoreNullValues = options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull;

        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();


        if (value.Message is not null || !ignoreNullValues)
        {
            writer.WriteString(ConvertName(nameof(value.Message)), value.Message);
        }

        writer.WriteString(ConvertName(nameof(value.StackTrace)), value.StackTrace);

        if (value.Source is not null || !ignoreNullValues)
        {
            writer.WriteString(ConvertName(nameof(value.Source)), value.Source);
        }

        if (value.InnerException is not null || !ignoreNullValues)
        {
            writer.WritePropertyName(ConvertName(nameof(value.InnerException)));
            if (value.InnerException is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonSerializer.Serialize(writer, value.InnerException, options);
            }
        }

        writer.WriteEndObject();
    }

    //writer.WriteStartObject();

    //    var exceptionType = value.GetType();

    //    writer.WriteString("ClassName", exceptionType.FullName);

    //    var properties = exceptionType.GetProperties()
    //        .Where(e => e.PropertyType != typeof(Type))
    //        .Where(e => e.PropertyType.Namespace != typeof(MemberInfo).Namespace)
    //        .ToList();

    //    foreach (var property in properties)
    //    {
    //        var propertyValue = property.GetValue(value, null);

    //        if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && propertyValue == null)
    //            continue;

    //        writer.WritePropertyName(property.Name);

    //        JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
    //    }

    //    writer.WriteEndObject();
    //var serializableProperties = value.GetType()
    //    .GetProperties()
    //    .Select(uu => new { uu.Name, Value = uu.GetValue(value) })
    //    .Where(uu => uu.Name != nameof(Exception.TargetSite));

    //if (options?.IgnoreNullValues == true)
    //{
    //    serializableProperties = serializableProperties.Where(uu => uu.Value != null);
    //}

    //var propList = serializableProperties.ToList();

    //if (propList.Count == 0)
    //{
    //    // Nothing to write
    //    return;
    //}

    //writer.WriteStartObject();

    //foreach (var prop in propList)
    //{
    //    writer.WritePropertyName(prop.Name);
    //    JsonSerializer.Serialize(writer, prop.Value, options);
    //}

    //writer.WriteEndObject();
    //}

    public sealed class JsonSerializedException : Exception
    {
        private readonly string _stackTrace;

        public JsonSerializedException(string? message, string? source, string? stackTrace, Exception? innerException)
            : base(message, innerException)
        {
            Source = source ?? string.Empty;
            _stackTrace = stackTrace ?? string.Empty;
        }

        public override string StackTrace => _stackTrace;
    }
}