namespace PvzPrinter.Core.Logging;

/// <summary>
/// Абстракция логирования. 
/// Реализация (FileLogger) знает про файлы, Core знает только интерфейс.
/// </summary>
public interface ILogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
}