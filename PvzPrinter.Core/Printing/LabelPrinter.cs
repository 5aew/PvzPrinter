using System.Drawing;
using System.Drawing.Printing;
using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;

namespace PvzPrinter.Core.Printing;

public class LabelPrinter : IPrinterService, IDisposable
{
    private readonly IConfigProvider _configProvider;
    private readonly ILogger _logger;
    private readonly TwoLevelLabelCache _cache;

    // Переиспользуемый PrintDocument
    private readonly PrintDocument _printDoc;
    private string _currentCellCode = "";
    private bool _disposed = false;

    public LabelPrinter(
        IConfigProvider configProvider,
        ILogger logger,
        ILabelRenderer renderer,
        TwoLevelLabelCache cache)
    {
        _configProvider = configProvider;
        _logger = logger;
        _cache = cache;

        // Создаём PrintDocument ОДИН раз
        _printDoc = new PrintDocument();
        _printDoc.PrintPage += OnPrintPage;

        _logger.Info("LabelPrinter initialized with reusable PrintDocument");
    }

    public Task PrintLabelAsync(string cellCode, CancellationToken ct = default)
    {
        // Больше не нужен Task.Run — вызов идёт из выделенного потока очереди
        try
        {
            var settings = _configProvider.GetSettings();

            if (string.IsNullOrWhiteSpace(settings.PrinterName))
            {
                _logger.Error("Printer name is not configured");
                throw new InvalidOperationException("Принтер не выбран в настройках");
            }

            _currentCellCode = cellCode;

            _printDoc.PrinterSettings.PrinterName = settings.PrinterName;
            _printDoc.DefaultPageSettings.PaperSize = new PaperSize(
                "Custom",
                MmToHundredthsOfInch(settings.LabelWidthMm),
                MmToHundredthsOfInch(settings.LabelHeightMm)
            );

            _logger.Info($"Printing label '{cellCode}' to '{settings.PrinterName}'");
            _printDoc.Print();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to print label '{cellCode}'", ex);
            throw;
        }

        return Task.CompletedTask;
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        var settings = _configProvider.GetSettings();

        // Кэш делает всю работу — рендеринг или выдача из памяти/диска
        using var bitmap = _cache.GetOrRender(_currentCellCode, settings);

        if (e.Graphics == null) return;

        e.Graphics.PageUnit = GraphicsUnit.Pixel;
        e.Graphics.DrawImage(bitmap, 0, 0);
    }

    private static int MmToHundredthsOfInch(double mm)
    {
        return (int)Math.Round(mm / 25.4 * 100);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _printDoc.Dispose();
            _disposed = true;
        }
    }
}