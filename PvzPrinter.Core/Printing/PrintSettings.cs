namespace PvzPrinter.Core.Printing;

/// <summary>
/// Тип фона этикетки
/// </summary>
public enum BackgroundType
{
    None,      // Без фона
    Default,   // Дефолтный из /backgrounds/default/
    Custom     // Пользовательский по абсолютному пути
}

/// <summary>
/// Настройки печати этикетки. 
/// Сериализуется в settings.json через IConfigProvider.
/// </summary>
public class PrintSettings
{
    /// <summary>Имя принтера в системе</summary>
    public string PrinterName { get; set; } = string.Empty;

    /// <summary>Ширина этикетки в мм</summary>
    public double LabelWidthMm { get; set; } = 30;

    /// <summary>Высота этикетки в мм</summary>
    public double LabelHeightMm { get; set; } = 20;

    /// <summary>Шрифт текста ячейки</summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>Размер шрифта</summary>
    public int FontSize { get; set; } = 14;

    /// <summary>Тип фона</summary>
    public BackgroundType BackgroundType { get; set; } = BackgroundType.Default;

    /// <summary>
    /// Путь к фону. 
    /// Для Default: относительный (white.png)
    /// Для Custom: абсолютный (C:\...\logo.png)
    /// </summary>
    public string BackgroundImage { get; set; } = "white.png";
}