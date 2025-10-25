using System;
using System.IO;

namespace ParadoxTranslator.Services;

/// <summary>
/// Logging service for debugging and monitoring
/// </summary>
public static class LoggingService
{
    private static readonly object _lock = new object();
    private static string? _logFilePath;

    public static void Initialize()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParadoxTranslator", "Logs");
        Directory.CreateDirectory(dir);
        
        var fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
        _logFilePath = Path.Combine(dir, fileName);
    }

    public static void Log(string level, string message, Exception? ex = null)
    {
        var config = SettingsService.LoadConfig();
        if (!config.EnableLogging) return;

        if (_logFilePath == null) Initialize();

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] [{level}] {message}";
        
        if (ex != null)
        {
            logLine += $"\n  Exception: {ex.GetType().Name}: {ex.Message}\n  StackTrace: {ex.StackTrace}";
        }

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath!, logLine + "\n");
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }

    public static void Debug(string message) => Log("DEBUG", message);
    public static void Info(string message) => Log("INFO", message);
    public static void Warning(string message) => Log("WARN", message);
    public static void Error(string message, Exception? ex = null) => Log("ERROR", message, ex);

    public static string GetLogDirectory()
    {
        if (_logFilePath == null) Initialize();
        return Path.GetDirectoryName(_logFilePath!) ?? string.Empty;
    }
}
