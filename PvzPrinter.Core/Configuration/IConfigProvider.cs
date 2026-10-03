using PvzPrinter.Core.Printing;

namespace PvzPrinter.Core.Configuration;

/// <summary>
/// Абстракция для работы с настройками приложения.
/// Реализация (JsonConfigProvider) знает про settings.json,
/// Core знает только этот интерфейс.
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    /// Читает текущие настройки печати.
    /// Если файла нет или он повреждён — возвращает дефолтные значения.
    /// </summary>
    PrintSettings GetSettings();

    /// <summary>
    /// Сохраняет настройки печати.
    /// </summary>
    void SaveSettings(PrintSettings settings);

    /// <summary>
    /// Возвращает абсолютный путь к папке с дефолтными фонами.
    /// Обычно это /backgrounds/default/ рядом с exe-файлом.
    /// </summary>
    string GetDefaultBackgroundsPath();
}