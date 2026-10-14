using System.Drawing;
using System.Drawing.Drawing2D;

namespace PvzPrinter.Core.Printing;

public class LabelRenderer : ILabelRenderer
{
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

        // Номер ячейки по центру
        using var font = new Font(settings.FontFamily, settings.FontSize, FontStyle.Bold);
        var textSize = g.MeasureString(cellCode, font);
        var x = (widthPx - textSize.Width) / 2;
        var y = (heightPx - textSize.Height) / 2;
        g.DrawString(cellCode, font, Brushes.Black, x, y);

        // Timestamp внизу
        if (settings.PrintTimestamp)
        {
            var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            float tsFontSize = Math.Max(4, settings.TimestampFontSize);
            using var smallFont = new Font(settings.FontFamily, tsFontSize, FontStyle.Regular);
            var tsSize = g.MeasureString(timestamp, smallFont);

            var tsX = (widthPx - tsSize.Width) / 2;
            var tsY = heightPx - tsSize.Height - MmToPixels(1); // 1мм отступ снизу

            g.DrawString(timestamp, smallFont, Brushes.Gray, tsX, tsY);
        }

        return bitmap;
    }

    private static int MmToPixels(double mm)
    {
        return (int)Math.Round(mm * Dpi / 25.4);
    }
}