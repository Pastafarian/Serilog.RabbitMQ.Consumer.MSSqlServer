using Serilog;
using Serilog.Loggers.RabbitMQ;
using Serilog.Producer.RabbitMq.Example;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddHttpContextAccessor();

IConfiguration loggerConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.log.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.log.json", true, true)
    .Build();

var logger = LoggerBuilder.BuildLogger(loggerConfiguration);
builder.Services.AddSingleton(_ => logger);

Log.Logger = logger;
IConfiguration auditConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.audit.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.log.json", true, true)
    .Build();

var auditLogger = LoggerBuilder.BuildAuditLogger(auditConfiguration);
builder.Services.AddSingleton<IAuditLogger>(_ => new AuditLogger(auditLogger));

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

namespace Serilog.Producer.RabbitMq.Example
{
    public partial class Program { }
}