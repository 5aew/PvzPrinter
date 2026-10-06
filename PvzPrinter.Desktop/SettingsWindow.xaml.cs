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
            TxtFontSize.Text = settings.FontSize.ToString();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load settings", ex);
            MessageBox.Show("Ошибка загрузки настроек. Проверьте лог.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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