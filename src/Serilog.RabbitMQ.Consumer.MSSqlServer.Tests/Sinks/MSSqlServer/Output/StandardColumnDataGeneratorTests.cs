﻿using System.Data;
using System.Diagnostics;
using System.Globalization;
using Moq;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.RabbitMQ.Consumer.MSSqlServer.BackgroundWorkers;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer.Output;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.Output
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class StandardColumnDataGeneratorTests
    {
        private readonly Mock<IXmlPropertyFormatter> _xmlPropertyFormatterMock;
        private StandardColumnDataGenerator _sut;

        public StandardColumnDataGeneratorTests()
        {
            _xmlPropertyFormatterMock = new Mock<IXmlPropertyFormatter>();
        }

        [Fact]
        public void GetStandardColumnNameAndValueWhenCalledWithoutFormatterRendersLogEventPropertyUsingInternalJsonFormatter()
        {
            // Arrange
            const string expectedLogEventContent =
                "{\"TimeStamp\":\"2020-01-01T09:00:00.0000000\",\"Level\":\"Information\",\"Message\":\"\",\"MessageTemplate\":\"\"}";
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            options.Store.Add(StandardColumn.LogEvent);
            var testDateTimeOffset = new DateTimeOffset(2020, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var logEvent = CreateLogEvent(testDateTimeOffset);
            SetupSut(options, CultureInfo.InvariantCulture);

            // Act
            var column = _sut.GetStandardColumnNameAndValue(StandardColumn.LogEvent, logEvent);

            // Assert
            Assert.Equal(expectedLogEventContent, column.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsSimpleTextMessageKeyValue()
        {
            // Arrange
            const string messageText = "Test message";
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken(messageText) }),
                new List<LogEventProperty>()), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(messageText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsSimpleTextMessageKeyValueWithMaxDataLengthDefined()
        {
            // Arrange
            const string messageText = "A long test message";
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken(messageText) }),
                new List<LogEventProperty>()), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions { Message = { DataLength = -1 } };
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(messageText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsTruncatedSimpleTextMessageKeyValue()
        {
            // Arrange
            const string messageText = "Test message";
            var messageTextWithOverflow = $"{messageText} being too long";
            var expectedMessageText = $"{messageText}...";
            var messageFieldLength = expectedMessageText.Length;

            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken(messageTextWithOverflow) }),
                new List<LogEventProperty>()), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions { Message = { DataLength = messageFieldLength } };
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(expectedMessageText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsMessageKeyValueWithDefaultFormatting()
        {
            // Arrange
            const string expectedText = "2.4";
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new PropertyToken("NumberProperty", "{NumberProperty}") }),
                new List<LogEventProperty> { new("NumberProperty", new ScalarValue(2.4)) }), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(expectedText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsTruncatedMessageKeyValueWithDefaultFormatting()
        {
            // Arrange
            const string expectedText = "2.4 seconds...";
            var messageFieldLength = expectedText.Length;

            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken>
                {
                    new PropertyToken("NumberProperty", "{NumberProperty}"),
                    new TextToken(" seconds duration")
                }),
                new List<LogEventProperty> { new("NumberProperty", new ScalarValue(2.4)) }), "");

            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions { Message = { DataLength = messageFieldLength } };
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(expectedText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsMessageKeyValueWithCustomFormatting()
        {
            // Arrange
            const string expectedText = "2,4";
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new PropertyToken("NumberProperty", "{NumberProperty}") }),
                new List<LogEventProperty> { new("NumberProperty", new ScalarValue(2.4)) }), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), new CultureInfo("de-AT"));

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(expectedText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageReturnsTruncatedMessageKeyValueWithCustomFormatting()
        {
            // Arrange
            const string expectedText = "2,4 seconds...";
            var messageFieldLength = expectedText.Length;

            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken>
                {
                    new PropertyToken("NumberProperty", "{NumberProperty}"),
                    new TextToken(" seconds duration")
                }),
                new List<LogEventProperty> { new("NumberProperty", new ScalarValue(2.4)) }), "");

            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions { Message = { DataLength = messageFieldLength } };
            SetupSut(columnOptions, new CultureInfo("de-AT"));

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Message, logEvent);

            // Assert
            Assert.Equal("Message", result.Key);
            Assert.Equal(expectedText, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageTemplateReturnsMessageTemplateKeyValue()
        {
            // Arrange
            var messageTemplate = new MessageTemplate(new List<MessageTemplateToken> { new PropertyToken("NumberProperty", "{NumberProperty}") });
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, messageTemplate,
                new List<LogEventProperty> { new("NumberProperty", new ScalarValue(2.4)) }), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.MessageTemplate, logEvent);

            // Assert
            Assert.Equal("MessageTemplate", result.Key);
            Assert.Equal(messageTemplate.Text, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForMessageTemplateReturnsTruncatedMessageTemplateKeyValue()
        {
            // Arrange
            var messageTemplate = new MessageTemplate(new List<MessageTemplateToken> { new PropertyToken("NumberProperty", "{NumberProperty}") });
            var expectedMessageTemplate = $"{messageTemplate.Text.Substring(0, 7)}...";
            const int messageTemplateFieldLength = 10;

            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, messageTemplate,
                new List<LogEventProperty> { new("NumberProperty", new ScalarValue(2.4)) }), "");

            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions { MessageTemplate = { DataLength = messageTemplateFieldLength } };
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.MessageTemplate, logEvent);

            // Assert
            Assert.Equal("MessageTemplate", result.Key);
            Assert.Equal(expectedMessageTemplate, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForLogLevelReturnsLogLevelKeyValue()
        {
            // Arrange
            var logLevel = LogEventLevel.Debug;
            var expectedValue = logLevel.ToString();
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                logLevel, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>()), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Level, logEvent);

            // Assert
            Assert.Equal("Level", result.Key);
            Assert.Equal(expectedValue, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForTraceIdReturnsLogLevelKeyValue()
        {
            // Arrange
            var traceId = ActivityTraceId.CreateFromString("34898a9020e0390190b0982370034f00".AsSpan());
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>(), traceId, ActivitySpanId.CreateRandom()), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.TraceId, logEvent);

            // Assert
            Assert.Equal("TraceId", result.Key);
            Assert.Equal(traceId, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForSpanIdReturnsLogLevelKeyValue()
        {
            // Arrange
            var spanId = ActivitySpanId.CreateFromString("0390190b09823700".AsSpan());
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>(), ActivityTraceId.CreateRandom(), spanId), "");
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.SpanId, logEvent);

            // Assert
            Assert.Equal("SpanId", result.Key);
            Assert.Equal(spanId, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForLogLevelReturnsLogLevelKeyValueAsEnum()
        {
            // Arrange
            var logLevel = LogEventLevel.Debug;
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                logLevel, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>()), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Level.StoreAsEnum = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Level, logEvent);

            // Assert
            Assert.Equal("Level", result.Key);
            Assert.Equal(logLevel, result.Value);
        }

        [Trait("Bugfix", "#187")]
        [Fact]
        public void GetStandardColumnNameAndValueForTimeStampCreatesTimeStampOfTypeDateTimeAccordingToColumnOptions()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            var testDateTimeOffset = new DateTimeOffset(2020, 1, 1, 9, 0, 0, new TimeSpan(1, 0, 0)); // Timezone +1:00
            var logEvent = CreateLogEvent(testDateTimeOffset);
            SetupSut(options, CultureInfo.InvariantCulture);

            // Act
            var column = _sut.GetStandardColumnNameAndValue(StandardColumn.TimeStamp, logEvent);

            // Assert
            Assert.IsType<DateTime>(column.Value);
            Assert.Equal(testDateTimeOffset.Hour, ((DateTime)column.Value).Hour);
        }

        [Trait("Bugfix", "#187")]
        [Fact]
        public void GetStandardColumnNameAndValueForTimeStampCreatesUtcConvertedTimeStampOfTypeDateTimeAccordingToColumnOptions()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                TimeStamp = { ConvertToUtc = true }
            };
            var testDateTimeOffset = new DateTimeOffset(2020, 1, 1, 9, 0, 0, new TimeSpan(1, 0, 0)); // Timezone +1:00
            var logEvent = CreateLogEvent(testDateTimeOffset);
            SetupSut(options, CultureInfo.InvariantCulture);

            // Act
            var column = _sut.GetStandardColumnNameAndValue(StandardColumn.TimeStamp, logEvent);

            // Assert
            Assert.IsType<DateTime>(column.Value);
            Assert.Equal(testDateTimeOffset.Hour - 1, ((DateTime)column.Value).Hour);
        }

        [Trait("Bugfix", "#187")]
        [Fact]
        public void GetStandardColumnNameAndValueForTimeStampCreatesTimeStampOfTypeDateTimeOffsetAccordingToColumnOptions()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                TimeStamp = { DataType = SqlDbType.DateTimeOffset }
            };
            var testDateTimeOffset = new DateTimeOffset(2020, 1, 1, 9, 0, 0, new TimeSpan(1, 0, 0)); // Timezone +1:00
            var logEvent = CreateLogEvent(testDateTimeOffset);
            SetupSut(options, CultureInfo.InvariantCulture);

            // Act
            var column = _sut.GetStandardColumnNameAndValue(StandardColumn.TimeStamp, logEvent);

            // Assert
            Assert.IsType<DateTimeOffset>(column.Value);
            var timeStampColumnOffset = (DateTimeOffset)column.Value;
            Assert.Equal(testDateTimeOffset.Hour, timeStampColumnOffset.Hour);
            Assert.Equal(testDateTimeOffset.Offset, timeStampColumnOffset.Offset);
        }

        [Trait("Bugfix", "#187")]
        [Fact]
        public void GetStandardColumnNameAndValueForTimeStampCreatesUtcConvertedTimeStampOfTypeDateTimeOffsetAccordingToColumnOptions()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                TimeStamp = { DataType = SqlDbType.DateTimeOffset, ConvertToUtc = true }
            };
            var testDateTimeOffset = new DateTimeOffset(2020, 1, 1, 9, 0, 0, new TimeSpan(1, 0, 0)); // Timezone +1:00
            var logEvent = CreateLogEvent(testDateTimeOffset);
            SetupSut(options, CultureInfo.InvariantCulture);

            // Act
            var column = _sut.GetStandardColumnNameAndValue(StandardColumn.TimeStamp, logEvent);

            // Assert
            Assert.IsType<DateTimeOffset>(column.Value);
            var timeStampColumnOffset = (DateTimeOffset)column.Value;
            Assert.Equal(testDateTimeOffset.Hour - 1, timeStampColumnOffset.Hour);
            Assert.Equal(new TimeSpan(0), timeStampColumnOffset.Offset);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForExceptionReturnsExceptionKeyValue()
        {
            // Arrange

            var expectedValue = "Something went wrong";
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, new Exception(), new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>()), "Something went wrong");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Level.StoreAsEnum = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Exception, logEvent);

            // Assert
            Assert.Equal("Exception", result.Key);
            Assert.Equal(expectedValue, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForExceptionWhenCalledWithoutExceptionReturnsNullValue()
        {
            // Arrange
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>()), null);
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Level.StoreAsEnum = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Exception, logEvent);

            // Assert
            Assert.Equal("Exception", result.Key);
            Assert.Null(result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesUsesRootElementName()
        {
            // Arrange
            const string rootElementName = "TestRootElement";
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>()), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.RootElementName = rootElementName;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            Assert.Equal($"<{rootElementName}></{rootElementName}>", result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterSimplifyForEachProperty()
        {
            // Arrange
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new("Property3", property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property1Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property2Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property3Value, columnOptions.Properties), Times.Once);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterSimplifyForEachNonAdditionalProperty()
        {
            // Arrange
            const string additionalColumnName = "AdditionalColumn1";
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new(additionalColumnName, property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn> { new() { PropertyName = additionalColumnName, DataType = SqlDbType.NVarChar } }
            };
            columnOptions.Properties.ExcludeAdditionalProperties = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property1Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property2Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property3Value, columnOptions.Properties), Times.Never);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterSimplifyForAlsoForAdditionalProperties()
        {
            // Arrange
            const string additionalColumnName = "AdditionalColumn1";
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new(additionalColumnName, property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn> { new() { PropertyName = additionalColumnName, DataType = SqlDbType.NVarChar } }
            };
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property1Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property2Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property3Value, columnOptions.Properties), Times.Once);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterSimplifyUnfilteredProperties()
        {
            // Arrange
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new("Property3", property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.PropertiesFilter = k => k != "Property2";
            SetupSut(columnOptions, CultureInfo.InvariantCulture);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property1Value, columnOptions.Properties), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property2Value, columnOptions.Properties), Times.Never);
            _xmlPropertyFormatterMock.Verify(x => x.Simplify(property3Value, columnOptions.Properties), Times.Once);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesDoesNotCallXmlPropertyFormatterGetValidElementNameIfUsePropertyKeyAsElementNameFalse()
        {
            // Arrange
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", new ScalarValue("1")),
                    new("Property2", new ScalarValue(2)),
                    new("Property3", new ScalarValue("Three"))
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            SetupSut(columnOptions, CultureInfo.InvariantCulture);
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(It.IsAny<LogEventPropertyValue>(), It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns("Somevalue");

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterGetValidElementNameForEachPropertyIfUsePropertyKeyAsElementNameTrue()
        {
            // Arrange
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
           new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new("Property3", property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.UsePropertyKeyAsElementName = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(It.IsAny<LogEventPropertyValue>(), It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns("Somevalue");

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property1"), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property2"), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property3"), Times.Once);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterGetValidElementNameAlsoForEmptyPropertyIfOmitEmptyFalse()
        {
            // Arrange
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
             new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new("Property3", property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.UsePropertyKeyAsElementName = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(property1Value, It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns("Value1");
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(property2Value, It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns(string.Empty);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property1"), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property2"), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property3"), Times.Once);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesCallsXmlPropertyFormatterGetValidElementNameForOnlyNonEmptyPropertiesIfOmitEmptyTrue()
        {
            // Arrange
            var property1Value = new ScalarValue("1");
            var property2Value = new ScalarValue(2);
            var property3Value = new ScalarValue("Three");
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
                 new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", property1Value),
                    new("Property2", property2Value),
                    new("Property3", property3Value)
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.UsePropertyKeyAsElementName = true;
            columnOptions.Properties.OmitElementIfEmpty = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(property1Value, It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns("Value1");
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(property2Value, It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns(string.Empty);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property1"), Times.Once);
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property2"), Times.Never);
            _xmlPropertyFormatterMock.Verify(x => x.GetValidElementName("Property3"), Times.Never);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesGeneratesCorrectXmlIfUsePropertyKeyAsElementNameTrue()
        {
            // Arrange
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
                  new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", new ScalarValue("1")),
                    new("Property2", new ScalarValue("2")),
                    new("Property3", new ScalarValue("3"))
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.RootElementName = "Root";
            columnOptions.Properties.UsePropertyKeyAsElementName = true;
            SetupSut(columnOptions, CultureInfo.InvariantCulture);
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(It.IsAny<LogEventPropertyValue>(), It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns("x");
            _xmlPropertyFormatterMock.Setup(x => x.GetValidElementName("Property1")).Returns("Element1");
            _xmlPropertyFormatterMock.Setup(x => x.GetValidElementName("Property2")).Returns("Element2");
            _xmlPropertyFormatterMock.Setup(x => x.GetValidElementName("Property3")).Returns("Element3");

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            Assert.Equal("Properties", result.Key);
            Assert.Equal("<Root><Element1>x</Element1><Element2>x</Element2><Element3>x</Element3></Root>", result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForPropertiesGeneratesCorrectXmlIfUsePropertyKeyAsElementNameFalse()
        {
            // Arrange
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken> { new TextToken("Test message") }),
                new List<LogEventProperty>
                {
                    new("Property1", new ScalarValue("1")),
                    new("Property2", new ScalarValue("2")),
                    new("Property3", new ScalarValue("3"))
                }), "");
            var columnOptions = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            columnOptions.Properties.RootElementName = "Root";
            columnOptions.Properties.PropertyElementName = "P";
            SetupSut(columnOptions, CultureInfo.InvariantCulture);
            _xmlPropertyFormatterMock.Setup(x => x.Simplify(It.IsAny<LogEventPropertyValue>(), It.IsAny<Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions.PropertiesColumnOptions>())).Returns("x");

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.Properties, logEvent);

            // Assert
            Assert.Equal("Properties", result.Key);
            Assert.Equal("<Root><P key=\'Property1\'>x</P><P key=\'Property2\'>x</P><P key=\'Property3\'>x</P></Root>", result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForLogEventRendersLogEventPropertyUsingCustomFormatter()
        {
            // Arrange
            const string testLogEventContent = "Content of LogEvent";
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();
            options.Store.Add(StandardColumn.LogEvent);
            var logEventFormatterMock = new Mock<ITextFormatterLogEventWithExceptionAsJsonString?>();
            logEventFormatterMock.Setup(f => f.Format(It.IsAny<LogEventWithExceptionAsJsonString>(), It.IsAny<TextWriter>()))
                .Callback<LogEvent, TextWriter>((e, w) => w.Write(testLogEventContent));
            var logEvent = CreateLogEvent(DateTimeOffset.UtcNow);
            SetupSut(options, CultureInfo.InvariantCulture, logEventFormatter: logEventFormatterMock.Object);

            // Act
            var result = _sut.GetStandardColumnNameAndValue(StandardColumn.LogEvent, logEvent);

            // Assert
            Assert.Equal(testLogEventContent, result.Value);
        }

        [Fact]
        public void GetStandardColumnNameAndValueForLogEventHandlesExcludeAdditionalPropertiesTrue()
        {
            // Arrange
            const string additionalColumnName = "AdditionalColumn1";
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn> { new(additionalColumnName, SqlDbType.NVarChar) }
            };
            options.LogEvent.ExcludeAdditionalProperties = true;
            options.Store.Add(StandardColumn.LogEvent);
            var logEventFormatterMock = new Mock<ITextFormatterLogEventWithExceptionAsJsonString?>();
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken>()),
                new List<LogEventProperty> { new(additionalColumnName, new ScalarValue("1234")) }), "");
            SetupSut(options, CultureInfo.InvariantCulture, logEventFormatter: logEventFormatterMock.Object);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.LogEvent, logEvent);

            // Assert
            //_sut.
            //logEventFormatterMock.Verify(f => f.Format(
            //    It.Is<LogEventWithExceptionAsJsonString>(e => !e.Properties.ContainsKey(additionalColumnName)),
            //    It.IsAny<StringWriter>()));
        }

        [Fact]
        public void GetStandardColumnNameAndValueForLogEventHandlesExcludeAdditionalPropertiesFalse()
        {
            // Arrange
            const string additionalColumnName = "AdditionalColumn1";
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn> { new(additionalColumnName, SqlDbType.NVarChar) }
            };
            options.Store.Add(StandardColumn.LogEvent);
            var logEventFormatterMock = new Mock<ITextFormatterLogEventWithExceptionAsJsonString>();
            var properties = new List<LogEventProperty> { new(additionalColumnName, new ScalarValue("1234")) };
            var logEvent = new LogEventWithExceptionAsJsonString(new LogEvent(
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero),
                LogEventLevel.Debug, null, new MessageTemplate(new List<MessageTemplateToken>()),
                properties), "");
            SetupSut(options, CultureInfo.InvariantCulture, logEventFormatter: logEventFormatterMock.Object);

            // Act
            _sut.GetStandardColumnNameAndValue(StandardColumn.LogEvent, logEvent);

            // Assert
            logEventFormatterMock.Verify(f => f.Format(
                It.Is<LogEventWithExceptionAsJsonString>(e => e.Properties.ContainsKey(additionalColumnName)),
                It.IsAny<StringWriter>()));
        }

        [Fact]
        public void GetStandardColumnNameAndValueForUnsupportedColumnThrows()
        {
            // Arrange
            var logEvent = CreateLogEvent(new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, TimeSpan.Zero));
            SetupSut(new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions(), CultureInfo.InvariantCulture);

            // Act + assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GetStandardColumnNameAndValue(StandardColumn.Id, logEvent));
        }

        private static LogEventWithExceptionAsJsonString CreateLogEvent(DateTimeOffset testDateTimeOffset)
        {
            return new LogEventWithExceptionAsJsonString(new LogEvent(testDateTimeOffset, LogEventLevel.Information, null,
                new MessageTemplate(new List<MessageTemplateToken>()), new List<LogEventProperty>()), "");
        }

        private void SetupSut(
            Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions options,
            IFormatProvider formatProvider = null,
            ITextFormatterLogEventWithExceptionAsJsonString? logEventFormatter = null)
        {
            _sut = new StandardColumnDataGenerator(options, formatProvider, _xmlPropertyFormatterMock.Object, logEventFormatter);
        }

    }
}
