﻿using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;

namespace Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;

public class MsSqlServerSinkOptionsProvider
{
    public MSSqlServerSinkOptions MsSqlServerSinkOptions = new("logs", 2000, TimeSpan.FromSeconds(5), true, true, "dbo");
}