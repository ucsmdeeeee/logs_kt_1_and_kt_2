using logs_kt_1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Diagnostics;

var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        formatter: new JsonFormatter(),
        path: Path.Combine(logsPath, "structured-.json"),
        rollingInterval: RollingInterval.Day,
        shared: true)
    .CreateLogger();

Trace.Listeners.Clear();
Trace.Listeners.Add(new SerilogTraceListener());
Trace.AutoFlush = true;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseSerilog();

    builder.Services.AddDbContext<ApplicationDbConext>(options =>
        options.UseInMemoryDatabase("LibraryDB"));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    Log.Information("Приложение запущено");

    if (app.Environment.IsDevelopment())
    {
        Log.Debug("Начало операции Swagger");
        app.UseSwagger();
        app.UseSwaggerUI();
        Log.Debug("Конец операции Swagger");
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Error(ex, "Критическая ошибка: {Message}. Приложение завершается.", ex.Message);
}
finally
{
    Log.Information("Приложение завершено");
    Log.CloseAndFlush();
}

public class SerilogTraceListener : TraceListener
{
    public override void Write(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Log.Debug("TRACE: {TraceMessage}", message);
    }

    public override void WriteLine(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Log.Debug("TRACE: {TraceMessage}", message);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        switch (eventType)
        {
            case TraceEventType.Information:
                Log.Information("{TraceMessage}", message);
                break;
            case TraceEventType.Warning:
                Log.Warning("{TraceMessage}", message);
                break;
            case TraceEventType.Error:
                Log.Error("{TraceMessage}", message);
                break;
            case TraceEventType.Critical:
                Log.Fatal("{TraceMessage}", message);
                break;
            default:
                Log.Debug("{TraceMessage}", message);
                break;
        }
    }
}