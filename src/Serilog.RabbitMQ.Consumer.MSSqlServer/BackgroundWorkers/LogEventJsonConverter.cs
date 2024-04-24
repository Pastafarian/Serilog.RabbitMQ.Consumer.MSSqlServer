using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;

public class LogEventWithExceptionAsJsonString : LogEvent
{
    public LogEventWithExceptionAsJsonString(LogEvent logEvent, string? exception) : base(logEvent.Timestamp,
        logEvent.Level, logEvent.Exception, logEvent.MessageTemplate,
        new List<LogEventProperty>(logEvent.Properties.Select(x => new LogEventProperty(x.Key, x.Value))))
    {
        JsonException = exception;
    }

    public string? JsonException { get; }

}

public class LogEventJsonConverter : JsonConverter<LogEventWithExceptionAsJsonString>
{
    public override LogEventWithExceptionAsJsonString Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        //using (var jsonDocument = JsonDocument.ParseValue(ref reader))
        //{
        //    var jsonText = jsonDocument.RootElement.GetRawText();
        //    var trtrtb = jsonText;
        //}

        Exception? exception = null;
        var level = LogEventLevel.Information;
        _ = JsonDocument.TryParseValue(ref reader, out var document);
        if (document == null) throw new ArgumentNullException(nameof(document));

        var root = document.RootElement;
        var messageTemplateText = root.GetProperty("MessageTemplate").Deserialize<string>();
        var timestamp = root.GetProperty("Timestamp").Deserialize<DateTimeOffset>();
        var exceptionJson = string.Empty;
        var exceptionJsonElement = document.RootElement;
        if (root.TryGetProperty("Exception", out exceptionJsonElement))
        {
            var exceptionText = exceptionJsonElement.ToString();

            exception = new Exception(exceptionText);
            exceptionJson = exceptionText;
        }

        var levelJsonElement = document.RootElement;
        if (root.TryGetProperty("Level", out levelJsonElement))
        {
            var levell = levelJsonElement.ToString();
            level = Enum.Parse<LogEventLevel>(levell, true);
        }

        var messageTemplateParser = new MessageTemplateParser();
        var messageTemplate = messageTemplateParser.Parse(messageTemplateText ?? "");

        var logEventPropertyValues = new Dictionary<string, LogEventPropertyValue>();
        var propertiesJsonElement = document.RootElement;
        if (root.TryGetProperty("Properties", out propertiesJsonElement))
        {

            foreach (var property in propertiesJsonElement.EnumerateObject())
            {
                if (property.Name == "ExceptionDetail")
                {
                    exceptionJson = property.Value.ToString();
                    exception = property.Value.Deserialize<Exception?>(ProjectConstants.JsonSerializerOptions);
                    continue;
                }

                var name = property.Name;
                var value = property.Value.ToString();

                logEventPropertyValues.Add(name, new ScalarValue(value));
            }
        }

        //var propertiesElement = root.GetProperty("Properties");
        //var logEventPropertyValues = new Dictionary<string, LogEventPropertyValue>();
        //foreach (var property in propertiesElement.EnumerateObject())
        //{
        //    if (property.Name == "ExceptionDetail")
        //    {
        //        exception = property.Value.Deserialize<Exception?>(ProjectConstants.JsonSerializerOptions);
        //        continue;
        //    }

        //    var name = property.Name;
        //    var value = property.Value.ToString();

        //    logEventPropertyValues.Add(name, new ScalarValue(value));
        //}

        var logEvent = new LogEvent(timestamp, level, exception, messageTemplate, logEventPropertyValues.Select(x => new LogEventProperty(x.Key, x.Value)));
        return new LogEventWithExceptionAsJsonString(logEvent, exceptionJson);
    }

    public override void Write(Utf8JsonWriter writer, LogEventWithExceptionAsJsonString value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(LogEvent).IsAssignableFrom(objectType);
    }
}