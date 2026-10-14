using PvzPrinter.Core.Printing;

namespace PvzPrinter.Core.Communication;

public interface IWebSocketServer : IDisposable
{
    void Start();
    void Stop();
    bool IsRunning { get; }

    /// <summary>
    /// Событие получения команды печати.
    /// </summary>
    event Action<PrintCommand>? PrintRequested;
}