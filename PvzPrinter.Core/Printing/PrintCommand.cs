namespace PvzPrinter.Core.Printing;

/// <summary>
/// Команда печати от внешнего клиента (Ozon extension).
/// </summary>
public class PrintCommand
{
    public string CellCode { get; set; } = "";
    public int Copies { get; set; } = 1;
}