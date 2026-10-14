using System.Drawing;
using System.Drawing.Drawing2D;

using PvzPrinter.Core.Logging;

namespace PvzPrinter.Core.Printing;

public class LabelRenderer : ILabelRenderer
{
    // Добавь поле для логгера
    private readonly ILogger _logger;

    public LabelRenderer(ILogger logger)
    {
        _logger = logger;
    }
    // Стандартное разрешение для термопринтеров (203 DPI)
    private const float Dpi = 203f;

    public Bitmap Render(string cellCode, PrintSettings settings)
    {
        // Переводим мм в пиксели при 203 DPI
        int widthPx = MmToPixels(settings.LabelWidthMm);
        int heightPx = MmToPixels(settings.LabelHeightMm);

        var bitmap = new Bitmap(widthPx, heightPx);
        bitmap.SetResolution(Dpi, Dpi);

        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Фон (белый по умолчанию)
        g.Clear(Color.White);

        // Номер ячейки по центру + калибровка
        using var font = new Font(settings.FontFamily, settings.FontSize, FontStyle.Bold);
        var textSize = g.MeasureString(cellCode, font);
        var x = (widthPx - textSize.Width) / 2 + MmToPixels(settings.HorizontalOffsetMm);
        var y = (heightPx - textSize.Height) / 2 + MmToPixels(settings.VerticalOffsetMm);
        g.DrawString(cellCode, font, Brushes.Black, x, y);

        // Диагностический лог
        _logger.Info($"Render called: PrintTimestamp={settings.PrintTimestamp}, TimestampFontSize={settings.TimestampFontSize}, CellCode={cellCode}");

        // Timestamp внизу
        if (settings.PrintTimestamp)
        {
            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            float tsFontSize = Math.Max(6, settings.TimestampFontSize); // Минимум 6pt вместо 4
            using var smallFont = new Font(settings.FontFamily, tsFontSize, FontStyle.Regular);
            var tsSize = g.MeasureString(timestamp, smallFont);

            var tsX = (widthPx - tsSize.Width) / 2 + MmToPixels(settings.HorizontalOffsetMm);
            var tsY = heightPx - tsSize.Height - MmToPixels(2); // 2мм отступ снизу

            g.DrawString(timestamp, smallFont, Brushes.Black, tsX, tsY);

            // Чёрный цвет вместо серого — надёжнее для термопечати
            g.DrawString(timestamp, smallFont, Brushes.Black, tsX, tsY);

            _logger.Info($"Timestamp drawn: '{timestamp}', size={tsFontSize}pt, y={tsY}px");
        }

        return bitmap;
    }

    private static int MmToPixels(double mm)
    {
        return (int)Math.Round(mm * Dpi / 25.4);
    }
}