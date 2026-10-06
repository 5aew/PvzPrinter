using System.Drawing.Printing;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;
using MessageBox = System.Windows.MessageBox; // ← Разрешаем конфликт

namespace PvzPrinter.Desktop;

public partial class SettingsWindow : Window
{
    private readonly IConfigProvider _configProvider;
    private readonly ILogger _logger;

    public SettingsWindow()
    {
        InitializeComponent();

        _configProvider = App.Services.GetRequiredService<IConfigProvider>();
        _logger = App.Services.GetRequiredService<ILogger>();

        LoadSettings();
    }
    /// <summary>
    /// Временный провайдер конфига для тестовой печати.
    /// Возвращает настройки из полей формы, а не из файла.
    /// </summary>
    internal class TestConfigProvider : PvzPrinter.Core.Configuration.IConfigProvider
    {
        private readonly PvzPrinter.Core.Printing.PrintSettings _settings;

        public TestConfigProvider(PvzPrinter.Core.Printing.PrintSettings settings) => _settings = settings;

        public PvzPrinter.Core.Printing.PrintSettings GetSettings() => _settings;
        public void SaveSettings(PvzPrinter.Core.Printing.PrintSettings settings) { /* no-op */ }
        public string GetDefaultBackgroundsPath() =>
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backgrounds", "default");
    }
    private void LoadSettings()
    {
        try
        {
            // Заполняем список принтеров
            CmbPrinters.ItemsSource = PrinterSettings.InstalledPrinters.Cast<string>().ToList();

            // Заполняем список системных шрифтов
            CmbFontFamily.ItemsSource = System.Drawing.FontFamily.Families
                .Select(f => f.Name)
                .OrderBy(n => n)
                .ToList();

            // Читаем текущие настройки
            var settings = _configProvider.GetSettings();

            CmbPrinters.SelectedItem = settings.PrinterName;
            TxtWidth.Text = settings.LabelWidthMm.ToString();
            TxtHeight.Text = settings.LabelHeightMm.ToString();
            CmbFontFamily.SelectedItem = settings.FontFamily; // ← Было TxtFontFamily
            ChkPrintTimestamp.IsChecked = settings.PrintTimestamp;
            TxtTimestampFontSize.Text = settings.TimestampFontSize.ToString();
            TxtFontSize.Text = settings.FontSize.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings", ex);
            MessageBox.Show("Ошибка загрузки настроек. Проверьте лог.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async void OnTestPrintClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Читаем текущие значения из полей (НЕ из файла!)
            if (!double.TryParse(TxtWidth.Text, out var width) || !double.TryParse(TxtHeight.Text, out var height))
            {
                MessageBox.Show("Некорректные размеры этикетки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtFontSize.Text, out var fontSize))
            {
                MessageBox.Show("Некорректный размер шрифта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var testSettings = new Core.Printing.PrintSettings
            {
                PrinterName = CmbPrinters.SelectedItem as string ?? "",
                LabelWidthMm = width,
                LabelHeightMm = height,
                FontFamily = CmbFontFamily.Text.Trim(),
                FontSize = fontSize,
                PrintTimestamp = ChkPrintTimestamp.IsChecked ?? true,
                TimestampFontSize = int.TryParse(TxtTimestampFontSize.Text, out var tsSize) ? tsSize : 7,
                BackgroundType = Core.Printing.BackgroundType.Default,
                BackgroundImage = "white.png"
            };

            // Создаём временный принтер с тестовыми настройками
            var logger = App.Services.GetRequiredService<PvzPrinter.Core.Logging.ILogger>();
            var printer = new PvzPrinter.Core.Printing.LabelPrinter(
                new TestConfigProvider(testSettings),
                logger
            );

            await printer.PrintLabelAsync("ТЕСТ-001");

            MessageBox.Show("Этикетка отправлена на печать!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!double.TryParse(TxtWidth.Text, out var width) || !double.TryParse(TxtHeight.Text, out var height))
            {
                MessageBox.Show("Некорректные размеры этикетки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtFontSize.Text, out var fontSize))
            {
                MessageBox.Show("Некорректный размер шрифта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = new Core.Printing.PrintSettings
            {
                PrinterName = CmbPrinters.SelectedItem as string ?? "",
                LabelWidthMm = width,
                LabelHeightMm = height,
                FontFamily = CmbFontFamily.Text.Trim(), // ← Было TxtFontFamily
                FontSize = fontSize,
                TimestampFontSize = int.TryParse(TxtTimestampFontSize.Text, out var tsSize) ? tsSize : 7,
                PrintTimestamp = ChkPrintTimestamp.IsChecked ?? true,
                BackgroundType = Core.Printing.BackgroundType.Default,
                BackgroundImage = "white.png"
            };

            _configProvider.SaveSettings(settings);
            _logger.Info("Settings saved via GUI");

            MessageBox.Show("Настройки сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save settings", ex);
            MessageBox.Show("Ошибка сохранения настроек", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}