using logs_kt_1.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System.Diagnostics;

var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        new CustomTraceFormatter(),
        Path.Combine(logsPath, "app-log-.txt"),
        rollingInterval: RollingInterval.Minute,
        shared: true)
    .CreateLogger();

Trace.Listeners.Clear();
Trace.Listeners.Add(new SerilogTraceListener());
Trace.AutoFlush = true;

try
{
    Trace.TraceInformation("Приложение запущено.");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDbContext<ApplicationDbConext>(options =>
        options.UseInMemoryDatabase("LibraryDB"));

    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        Trace.WriteLine("Начало операции Swagger.");
        app.UseSwagger();
        app.UseSwaggerUI();
        Trace.WriteLine("Конец операции Swagger.");
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Trace.TraceError($"Критическая ошибка: {ex.Message}. Приложение завершается."); //"Trace" не содержит определение для "TraceEvent".
}
finally
{
    Trace.TraceInformation("Приложение завершено.");
    Log.CloseAndFlush();
}

public class SerilogTraceListener : TraceListener
{
    public override void Write(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Log.Information("{Message}", $"[TRACE] {message}");
    }

    public override void WriteLine(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Log.Information("{Message}", $"[TRACE] {message}");
    }

    public override void TraceEvent(TraceEventCache? eventCache, string? source, TraceEventType eventType, int id, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        switch (eventType)
        {
            case TraceEventType.Information:
                Log.Information("{Message}", message);
                break;
            case TraceEventType.Warning:
                Log.Warning("{Message}", message);
                break;
            case TraceEventType.Error:
                Log.Error("{Message}", message);
                break;
            case TraceEventType.Critical:
                Log.Fatal("{Message}", message);
                break;
            default:
                Log.Information("{Message}", message);
                break;
        }
    }
}

public class CustomTraceFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        var message = logEvent.RenderMessage();

        string level = message.StartsWith("[TRACE]")
            ? "TRACE"
            : logEvent.Level switch
            {
                LogEventLevel.Information => "INFO",
                LogEventLevel.Warning => "WARN",
                LogEventLevel.Error => "ERROR",
                LogEventLevel.Fatal => "CRITICAL",
                _ => logEvent.Level.ToString().ToUpper()
            };

        output.WriteLine($"{timestamp} [{level}] {message.Replace("[TRACE] ", "")}");

        if (logEvent.Exception != null)
            output.WriteLine(logEvent.Exception);
    }
}