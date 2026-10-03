using System.Text.Json;

using PvzPrinter.Core.Logging;
using PvzPrinter.Core.Printing;

namespace PvzPrinter.Core.Configuration;

/// <summary>
/// Реализация чтения/записи настроек через settings.json.
/// Безопасна: при ошибках возвращает дефолтные значения и пишет в лог.
/// </summary>
public class JsonConfigProvider : IConfigProvider
{
    private readonly string _configPath;
    private readonly string _defaultBackgroundsPath;
    private readonly ILogger _logger;

    public JsonConfigProvider(ILogger logger)
    {
        _logger = logger;

        // Конфиг лежит рядом с exe
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = Path.Combine(appDir, "settings.json");
        _defaultBackgroundsPath = Path.Combine(appDir, "backgrounds", "default");

        // Гарантируем существование папки с фонами
        Directory.CreateDirectory(_defaultBackgroundsPath);
    }

    public PrintSettings GetSettings()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.Info("settings.json not found, using defaults");
                return new PrintSettings();
            }

            var json = File.ReadAllText(_configPath);
            var settings = JsonSerializer.Deserialize<PrintSettings>(json);

            return settings ?? new PrintSettings();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to read settings.json, using defaults", ex);
            return new PrintSettings();
        }
    }

    public void SaveSettings(PrintSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_configPath, json);

            _logger.Info($"Settings saved to {_configPath}");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings.json", ex);
        }
    }

    public string GetDefaultBackgroundsPath() => _defaultBackgroundsPath;
}