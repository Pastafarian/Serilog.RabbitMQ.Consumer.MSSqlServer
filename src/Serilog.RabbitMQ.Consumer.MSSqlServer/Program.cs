using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog.RabbitMQ.Consumer.MSSqlServer.MSSqlServer;
using Serilog.RabbitMQ.Consumer.MSSqlServer.Setup;

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.SetupServices(new MSSqlServerSinkOptions("ApiLogs", 2000, TimeSpan.FromSeconds(5), true, true, "dbo"));

    var app = builder.Build();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();

}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}

namespace Serilog.RabbitMQ.Consumer.MSSqlServer
{
    public partial class Program { }
}
