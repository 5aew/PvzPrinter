namespace PvzPrinter.Core.Printing;

/// <summary>
/// Контракт сервиса печати этикеток.
/// Реализация (LabelPrinter) знает про System.Drawing.Printing,
/// вызывающий код знает только этот интерфейс.
/// </summary>
public interface IPrinterService
{
    /// <summary>
    /// Печатает этикетку с номером ячейки.
    /// Настройки (размер, шрифт, фон) берёт из IConfigProvider.
    /// </summary>
    /// <param name="cellCode">Номер ячейки (например, "47-1" или "123-12")</param>
    /// <param name="ct">Токен отмены</param>
    Task PrintLabelAsync(string cellCode, CancellationToken ct = default);
}