using Microsoft.Extensions.DependencyInjection;
using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;
using PvzPrinter.Core.Printing;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MessageBox = System.Windows.MessageBox; // ← Разрешаем конфликт

namespace PvzPrinter.Desktop;

public partial class SettingsWindow : Window
{
    private readonly IConfigProvider _configProvider;
    private readonly ILogger _logger;
    private readonly ILabelRenderer _renderer;

    public SettingsWindow()
    {
        InitializeComponent();

        // 1. СНАЧАЛА получаем все сервисы
        _configProvider = App.Services.GetRequiredService<IConfigProvider>();
        _logger = App.Services.GetRequiredService<ILogger>();
        _renderer = App.Services.GetRequiredService<ILabelRenderer>();

        // 2. ПОТОМ подписываемся на события
        TxtWidth.TextChanged += (_, _) => UpdatePreview();
        TxtHeight.TextChanged += (_, _) => UpdatePreview();
        CmbFontFamily.SelectionChanged += (_, _) => UpdatePreview();
        CmbFontFamily.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
            new System.Windows.Controls.TextChangedEventHandler((_, _) => UpdatePreview()));
        TxtFontSize.TextChanged += (_, _) => UpdatePreview();
        ChkPrintTimestamp.Checked += (_, _) => UpdatePreview();
        ChkPrintTimestamp.Unchecked += (_, _) => UpdatePreview();
        TxtTimestampFontSize.TextChanged += (_, _) => UpdatePreview();

        // 3. Загружаем настройки (заполняет поля)
        LoadSettings();

        // 4. И ТОЛЬКО ПОСЛЕ всего показываем первое превью
        UpdatePreview();
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
            var renderer = App.Services.GetRequiredService<PvzPrinter.Core.Printing.ILabelRenderer>();
            var printer = new PvzPrinter.Core.Printing.LabelPrinter(
                new TestConfigProvider(testSettings),
                logger,
                renderer  // ← Добавлен третий аргумент
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
    private void UpdatePreview()
    {
        try
        {
            if (!double.TryParse(TxtWidth.Text, out var w)) w = 30;
            if (!double.TryParse(TxtHeight.Text, out var h)) h = 20;
            if (!int.TryParse(TxtFontSize.Text, out var fs)) fs = 14;
            if (!int.TryParse(TxtTimestampFontSize.Text, out var tsFs)) tsFs = 7;

            var previewSettings = new Core.Printing.PrintSettings
            {
                LabelWidthMm = w,
                LabelHeightMm = h,
                FontFamily = string.IsNullOrWhiteSpace(CmbFontFamily.Text) ? "Arial" : CmbFontFamily.Text.Trim(),
                FontSize = fs,
                PrintTimestamp = ChkPrintTimestamp.IsChecked ?? true,
                TimestampFontSize = tsFs
            };

            // Старый битмап нужно задиспозить
            var oldSource = ImgPreview.Source as System.Windows.Media.Imaging.BitmapImage;

            using var bitmap = _renderer.Render("ТЕСТ-001", previewSettings);

            // Конвертируем System.Drawing.Bitmap → WPF ImageSource
            var bmpImage = new System.Windows.Media.Imaging.BitmapImage();
            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            bmpImage.BeginInit();
            bmpImage.StreamSource = ms;
            bmpImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmpImage.EndInit();
            bmpImage.Freeze(); // Важно для потокобезопасности

            ImgPreview.Source = bmpImage;
        }
        catch
        {
            // Тихий fail: если превью не получилось — просто не обновляем
        }
    }
}