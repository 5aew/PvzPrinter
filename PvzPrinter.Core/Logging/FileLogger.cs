namespace PvzPrinter.Core.Logging;

/// <summary>
/// Простая реализация логирования в файл.
/// Ротация по размеру (5 МБ) + запись с датой/временем.
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 МБ

    public FileLogger(string logDirectory = "logs")
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, "app.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message} | {ex}" : message;
        Write("ERROR", fullMessage);
    }

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                RotateIfNeeded();
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, line);
            }
            catch
            {
                // Тихий fail: логгер никогда не должен ронять приложение
            }
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFilePath)) return;

        var fileInfo = new FileInfo(_logFilePath);
        if (fileInfo.Length < MaxFileSizeBytes) return;

        var backupPath = _logFilePath + $".{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        File.Move(_logFilePath, backupPath);
    }
}