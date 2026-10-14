using System.Drawing;
using System.Drawing.Printing;

using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;

namespace PvzPrinter.Core.Printing;

public class LabelPrinter : IPrinterService
{
    private readonly IConfigProvider _configProvider;
    private readonly ILogger _logger;
    private readonly ILabelRenderer _renderer;

    // Храним текущий код ячейки для обработчика PrintPage
    private string _currentCellCode = "";

    public LabelPrinter(IConfigProvider configProvider, ILogger logger, ILabelRenderer renderer)
    {
        _configProvider = configProvider;
        _logger = logger;
        _renderer = renderer;
    }

    public Task PrintLabelAsync(string cellCode, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var settings = _configProvider.GetSettings();

                if (string.IsNullOrWhiteSpace(settings.PrinterName))
                {
                    _logger.Error("Printer name is not configured");
                    throw new InvalidOperationException("Принтер не выбран в настройках");
                }

                _currentCellCode = cellCode;

                using var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = settings.PrinterName;

                // Устанавливаем размер бумаги из настроек
                printDoc.DefaultPageSettings.PaperSize = new PaperSize(
                    "Custom",
                    MmToHundredthsOfInch(settings.LabelWidthMm),
                    MmToHundredthsOfInch(settings.LabelHeightMm)
                );

                printDoc.PrintPage += OnPrintPage;

                _logger.Info($"Printing label '{cellCode}' to '{settings.PrinterName}'");
                printDoc.Print();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to print label '{cellCode}'", ex);
                throw;
            }
        }, ct);
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        var settings = _configProvider.GetSettings();
        using var bitmap = _renderer.Render(_currentCellCode, settings);

        if (e.Graphics == null) return;

        e.Graphics.PageUnit = GraphicsUnit.Pixel;

        // Смещения уже учтены внутри Render() — просто кладём Bitmap в начало координат
        e.Graphics.DrawImage(bitmap, 0, 0);
    }

    /// <summary>
    /// Переводит миллиметры в сотые доли дюйма (единица PaperSize).
    /// </summary>
    private static int MmToHundredthsOfInch(double mm)
    {
        return (int)Math.Round(mm / 25.4 * 100);
    }
    private static int MmToPixels(double mm)
    {
        const float dpi = 203f;
        return (int)Math.Round(mm * dpi / 25.4);
    }
}