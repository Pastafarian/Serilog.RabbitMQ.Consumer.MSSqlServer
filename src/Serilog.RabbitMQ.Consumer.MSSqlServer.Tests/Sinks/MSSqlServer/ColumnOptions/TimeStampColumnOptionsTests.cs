﻿using System.Data;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.TestUtils;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Tests.Sinks.MSSqlServer.ColumnOptions
{
    [Trait(TestCategory.TraitName, TestCategory.Unit)]
    public class TimeStampColumnOptionsTests
    {
        [Trait("Bugfix", "#187")]
        [Fact]
        public void CanSetDataTypeDateTime()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act - should not throw
            options.TimeStamp.DataType = SqlDbType.DateTime;
        }

        [Trait("Bugfix", "#187")]
        [Fact]
        public void CanSetDataTypeDateTimeOffset()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act - should not throw
            options.TimeStamp.DataType = SqlDbType.DateTimeOffset;
        }

        [Trait("Bugfix", "#187")]
        [Fact]
        public void CannotSetDataTypeNVarChar()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act and assert - should throw
            Assert.Throws<ArgumentException>(() => options.TimeStamp.DataType = SqlDbType.NVarChar);
        }

        [Trait("Feature", "#300")]
        [Fact]
        public void CanSetDataTypeDateTime2()
        {
            // Arrange
            var options = new Consumer.MSSqlServer.MSSqlServer.ColumnOptions.ColumnOptions();

            // Act - should not throw
            options.TimeStamp.DataType = SqlDbType.DateTime2;
        }
    }
}
