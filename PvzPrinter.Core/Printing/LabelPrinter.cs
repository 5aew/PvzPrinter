using System.Drawing;
using System.Drawing.Printing;

using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;

// Явно указываем, что Font и Image — из System.Drawing, а не из Mime
using Font = System.Drawing.Font;
using Image = System.Drawing.Image;

using static System.Net.Mime.MediaTypeNames;

namespace PvzPrinter.Core.Printing;

/// <summary>
/// Реализация печати этикеток через System.Drawing.Printing.
/// Берёт настройки из IConfigProvider, логирует через ILogger.
/// </summary>
public class LabelPrinter : IPrinterService
{
    private readonly IConfigProvider _configProvider;
    private readonly ILogger _logger;

    public LabelPrinter(IConfigProvider configProvider, ILogger logger)
    {
        _configProvider = configProvider;
        _logger = logger;
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
                    _logger.Warn("Printer name is empty, skipping print");
                    return;
                }

                var doc = new PrintDocument();
                doc.PrinterSettings.PrinterName = settings.PrinterName;

                // Устанавливаем размер бумаги в мм → дюймы (1 inch = 25.4 mm)
                doc.DefaultPageSettings.PaperSize = new PaperSize(
                    "CustomLabel",
                    MmToHundredthsInch(settings.LabelWidthMm),
                    MmToHundredthsInch(settings.LabelHeightMm)
                );
                doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                // Рисуем содержимое этикетки
                doc.PrintPage += (sender, e) => DrawLabel(e!, cellCode, settings);

                doc.Print();
                _logger.Info($"Printed label for cell '{cellCode}' on '{settings.PrinterName}'");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to print label for cell '{cellCode}'", ex);
            }
        }, ct);
    }

    private void DrawLabel(PrintPageEventArgs e, string cellCode, PrintSettings settings)
    {
        var g = e.Graphics!;

        // 1. Фон
        DrawBackground(g, e.PageBounds, settings);

        // 2. Номер ячейки по центру
        using var font = new Font(settings.FontFamily, settings.FontSize, FontStyle.Bold);
        var textSize = g.MeasureString(cellCode, font);

        var x = (e.PageBounds.Width - textSize.Width) / 2;
        var y = (e.PageBounds.Height - textSize.Height) / 2;

        g.DrawString(cellCode, font, Brushes.Black, x, y);

        // 3. Дата и время внизу (если включено)
        if (settings.PrintTimestamp)
        {
            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            using var smallFont = new Font(settings.FontFamily, Math.Max(6, settings.FontSize / 3), FontStyle.Regular);
            var tsSize = g.MeasureString(timestamp, smallFont);

            var tsX = (e.PageBounds.Width - tsSize.Width) / 2;
            var tsY = e.PageBounds.Height - tsSize.Height - 2; // 2px отступ снизу

            g.DrawString(timestamp, smallFont, Brushes.Gray, tsX, tsY);
        }
    }

    private void DrawBackground(Graphics g, Rectangle bounds, PrintSettings settings)
    {
        try
        {
            string? imagePath = null;

            if (settings.BackgroundType == BackgroundType.Default)
            {
                var defaultPath = Path.Combine(
                    _configProvider.GetDefaultBackgroundsPath(),
                    settings.BackgroundImage
                );
                if (File.Exists(defaultPath)) imagePath = defaultPath;
            }
            else if (settings.BackgroundType == BackgroundType.Custom)
            {
                if (File.Exists(settings.BackgroundImage))
                    imagePath = settings.BackgroundImage;
            }

            if (imagePath != null)
            {
                using var bgImage = Image.FromFile(imagePath);
                g.DrawImage(bgImage, bounds);
            }
        }
        catch (Exception ex)
        {
            // Тихий fallback: если фон не загрузился — печатаем без него
            _logger.Warn($"Failed to load background image, printing without it: {ex.Message}");
        }
    }

    /// <summary>
    /// Перевод миллиметров в сотые доли дюйма (единица PaperSize)
    /// </summary>
    private static int MmToHundredthsInch(double mm) => (int)(mm / 25.4 * 100);
}