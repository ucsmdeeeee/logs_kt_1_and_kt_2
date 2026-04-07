using logs_kt_1.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using System.Diagnostics;

var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsPath);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
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

AppTracing.PerfSource.Listeners.Clear();
AppTracing.PerfSource.Listeners.Add(new SerilogTraceListener());
AppTracing.PerfSource.Switch = new SourceSwitch("PerfSwitch", "Verbose")
{
    Level = SourceLevels.All
};

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
        var swSwagger = Stopwatch.StartNew();
        AppTracing.PerfSource.TraceEvent(TraceEventType.Start, 1, "Начало операции Swagger");

        Trace.WriteLine("Начало операции Swagger.");
        app.UseSwagger();
        app.UseSwaggerUI();
        Trace.WriteLine("Конец операции Swagger.");

        swSwagger.Stop();
        Trace.WriteLine($"[PERF] Swagger занял {swSwagger.ElapsedMilliseconds} мс");
        AppTracing.PerfSource.TraceEvent(
            TraceEventType.Information,
            2,
            $"Swagger занял {swSwagger.ElapsedMilliseconds} мс");
        AppTracing.PerfSource.TraceEvent(TraceEventType.Stop, 3, "Завершение операции Swagger");
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Trace.TraceError($"Критическая ошибка: {ex.Message}. Приложение завершается.");
}
finally
{
    Trace.TraceInformation("Приложение завершено.");
    Log.CloseAndFlush();
}

public static class AppTracing
{
    public static readonly TraceSource PerfSource = new TraceSource("PerfTracer");
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

    public override void TraceEvent(
        TraceEventCache? eventCache,
        string? source,
        TraceEventType eventType,
        int id,
        string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        switch (eventType)
        {
            case TraceEventType.Start:
                Log.Information("{Message}", $"[TRACE-SOURCE][START] {message}");
                break;
            case TraceEventType.Stop:
                Log.Information("{Message}", $"[TRACE-SOURCE][STOP] {message}");
                break;
            case TraceEventType.Information:
                Log.Information("{Message}", $"[TRACE-SOURCE][INFO] {message}");
                break;
            case TraceEventType.Warning:
                Log.Warning("{Message}", $"[TRACE-SOURCE][WARN] {message}");
                break;
            case TraceEventType.Error:
                Log.Error("{Message}", $"[TRACE-SOURCE][ERROR] {message}");
                break;
            case TraceEventType.Critical:
                Log.Fatal("{Message}", $"[TRACE-SOURCE][CRITICAL] {message}");
                break;
            default:
                Log.Information("{Message}", $"[TRACE-SOURCE] {message}");
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

        string level = message.Contains("[PERF]")
            ? "PERF"
            : message.StartsWith("[TRACE]")
                ? "TRACE"
                : message.StartsWith("[TRACE-SOURCE][START]")
                    ? "START"
                    : message.StartsWith("[TRACE-SOURCE][STOP]")
                        ? "STOP"
                        : message.StartsWith("[TRACE-SOURCE][INFO]")
                            ? "INFO"
                            : message.StartsWith("[TRACE-SOURCE][WARN]")
                                ? "WARN"
                                : message.StartsWith("[TRACE-SOURCE][ERROR]")
                                    ? "ERROR"
                                    : message.StartsWith("[TRACE-SOURCE][CRITICAL]")
                                        ? "CRITICAL"
                                        : logEvent.Level switch
                                        {
                                            LogEventLevel.Information => "INFO",
                                            LogEventLevel.Warning => "WARN",
                                            LogEventLevel.Error => "ERROR",
                                            LogEventLevel.Fatal => "CRITICAL",
                                            _ => logEvent.Level.ToString().ToUpper()
                                        };

        output.WriteLine($"{timestamp} [{level}] {message}");

        if (logEvent.Exception != null)
            output.WriteLine(logEvent.Exception);
    }
}