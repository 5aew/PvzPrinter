using System.Drawing;

namespace PvzPrinter.Core.Printing;

/// <summary>
/// Генерирует изображение этикетки в памяти (без привязки к принтеру).
/// </summary>
public interface ILabelRenderer
{
    /// <summary>
    /// Создаёт Bitmap этикетки по заданным настройкам.
    /// Вызывающий код обязан задиспозить возвращённый Bitmap.
    /// </summary>
    Bitmap Render(string cellCode, PrintSettings settings);
}