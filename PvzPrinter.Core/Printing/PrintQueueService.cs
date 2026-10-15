using System.Collections.Concurrent;
using PvzPrinter.Core.Logging;

namespace PvzPrinter.Core.Printing;

public class PrintQueueService : IDisposable
{
    private readonly IPrinterService _printer;
    private readonly ILogger _logger;
    private readonly BlockingCollection<string> _queue = new();
    private readonly Thread _workerThread;
    private bool _disposed = false;

    public PrintQueueService(IPrinterService printer, ILogger logger)
    {
        _printer = printer;
        _logger = logger;

        _workerThread = new Thread(ProcessQueue)
        {
            Name = "PrintQueueWorker",
            IsBackground = true
        };
        _workerThread.Start();

        _logger.Info("Print queue service started");
    }

    /// <summary>
    /// Добавляет команду печати в очередь.
    /// Возвращается мгновенно — печать происходит в фоне.
    /// </summary>
    public void Enqueue(string cellCode)
    {
        if (_disposed) return;
        _queue.Add(cellCode);
        _logger.Info($"Enqueued '{cellCode}' (queue size: {_queue.Count})");
    }

    private void ProcessQueue()
    {
        try
        {
            foreach (var cellCode in _queue.GetConsumingEnumerable())
            {
                try
                {
                    _printer.PrintLabelAsync(cellCode).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Print failed for '{cellCode}'", ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение при Dispose
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _queue.CompleteAdding();
            _workerThread.Join(TimeSpan.FromSeconds(5));
            _queue.Dispose();
            _logger.Info("Print queue service stopped");
        }
    }
}