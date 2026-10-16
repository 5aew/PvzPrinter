using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PvzPrinter.Desktop;

public partial class MainWindow : Window
{
    private NotifyIcon? _notifyIcon;
    private bool _isClosingFromTray = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();

        this.Closing += OnWindowClosing;
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = new System.Drawing.Icon("app.ico"),
            Text = "PvzPrinter",
            Visible = true
        };

        // Контекстное меню трея
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("⚙️ Открыть настройки", null, OnTraySettingsClick);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("❌ Выход", null, OnTrayExitClick);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Двойной клик по трею — открыть окно
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    /// <summary>
    /// Анимация засасывания окна в трей (к правому нижнему углу).
    /// </summary>
    private void AnimateToTray()
    {
        try
        {
            // Целевая позиция — правый нижний угол рабочей области
            var workArea = SystemParameters.WorkArea;
            var targetX = workArea.Right - Width / 2;
            var targetY = workArea.Bottom - Height / 2;

            var duration = TimeSpan.FromMilliseconds(400);
            var easing = new CubicEase { EasingMode = EasingMode.EaseIn };

            var storyboard = new Storyboard();

            var leftAnim = new DoubleAnimation(Left, targetX, duration) { EasingFunction = easing };
            var topAnim = new DoubleAnimation(Top, targetY, duration) { EasingFunction = easing };
            var widthAnim = new DoubleAnimation(Width, 0, duration) { EasingFunction = easing };
            var heightAnim = new DoubleAnimation(Height, 0, duration) { EasingFunction = easing };
            var opacityAnim = new DoubleAnimation(1.0, 0.0, duration) { EasingFunction = easing };

            storyboard.Children.Add(leftAnim);
            storyboard.Children.Add(topAnim);
            storyboard.Children.Add(widthAnim);
            storyboard.Children.Add(heightAnim);
            storyboard.Children.Add(opacityAnim);

            Storyboard.SetTarget(leftAnim, this);
            Storyboard.SetTargetProperty(leftAnim, new PropertyPath(LeftProperty));
            Storyboard.SetTarget(topAnim, this);
            Storyboard.SetTargetProperty(topAnim, new PropertyPath(TopProperty));
            Storyboard.SetTarget(widthAnim, this);
            Storyboard.SetTargetProperty(widthAnim, new PropertyPath(WidthProperty));
            Storyboard.SetTarget(heightAnim, this);
            Storyboard.SetTargetProperty(heightAnim, new PropertyPath(HeightProperty));
            Storyboard.SetTarget(opacityAnim, this);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(OpacityProperty));

            storyboard.Completed += (_, _) =>
            {
                Hide();
                // Сбрасываем размеры для следующего показа
                Opacity = 1.0;
                Width = 400;
                Height = 350;
            };

            storyboard.Begin(this);
        }
        catch
        {
            Hide();
        }
    }

    /// <summary>
    /// Анимация появления окна из трея (из правого нижнего угла).
    /// </summary>
    private void ShowFromTray()
    {
        try
        {
            var workArea = SystemParameters.WorkArea;

            // Начинаем из правого нижнего угла с нулевым размером
            Left = workArea.Right - 200;
            Top = workArea.Bottom - 175;
            Width = 0;
            Height = 0;
            Opacity = 0;

            Show();
            Activate();

            var centerX = (workArea.Width - 400) / 2 + workArea.Left;
            var centerY = (workArea.Height - 350) / 2 + workArea.Top;

            var duration = TimeSpan.FromMilliseconds(400);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

            var storyboard = new Storyboard();

            var leftAnim = new DoubleAnimation(Left, centerX, duration) { EasingFunction = easing };
            var topAnim = new DoubleAnimation(Top, centerY, duration) { EasingFunction = easing };
            var widthAnim = new DoubleAnimation(0, 400, duration) { EasingFunction = easing };
            var heightAnim = new DoubleAnimation(0, 350, duration) { EasingFunction = easing };
            var opacityAnim = new DoubleAnimation(0, 1, duration) { EasingFunction = easing };

            storyboard.Children.Add(leftAnim);
            storyboard.Children.Add(topAnim);
            storyboard.Children.Add(widthAnim);
            storyboard.Children.Add(heightAnim);
            storyboard.Children.Add(opacityAnim);

            Storyboard.SetTarget(leftAnim, this);
            Storyboard.SetTargetProperty(leftAnim, new PropertyPath(LeftProperty));
            Storyboard.SetTarget(topAnim, this);
            Storyboard.SetTargetProperty(topAnim, new PropertyPath(TopProperty));
            Storyboard.SetTarget(widthAnim, this);
            Storyboard.SetTargetProperty(widthAnim, new PropertyPath(WidthProperty));
            Storyboard.SetTarget(heightAnim, this);
            Storyboard.SetTargetProperty(heightAnim, new PropertyPath(HeightProperty));
            Storyboard.SetTarget(opacityAnim, this);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(OpacityProperty));

            storyboard.Begin(this);
        }
        catch
        {
            Show();
            Activate();
        }
    }

    

    #region Tray Menu Handlers

    private void OnTraySettingsClick(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        });
    }

    private void OnTrayExitClick(object? sender, EventArgs e)
    {
        _isClosingFromTray = true;
        Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    #endregion

    #region Window Button Handlers
    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }
    private void OnOpenLogClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            if (File.Exists(logPath))
                Process.Start(new ProcessStartInfo(logPath) { UseShellExecute = true });
            else
                MessageBox.Show("Файл лога не найден", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть лог: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnWebsiteClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com") { UseShellExecute = true });
        }
        catch { }
    }

    private void OnDonateClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Спасибо за интерес! 💛\n\n" +
            "Вы можете поддержать разработку:\n" +
            "• ⭐ Поставить звезду на GitHub\n" +
            "• 🐛 Сообщить о баге или предложить фичу\n" +
            "• 💬 Рассказать коллегам о программе\n\n" +
            "Лучшая благодарность — когда программа помогает в работе!",
            "Отблагодарить разработчика",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region Window Lifecycle

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosingFromTray)
        {
            // Реальное закрытие из трея — разрешаем
            return;
        }

        // Отменяем закрытие и запускаем анимацию
        e.Cancel = true;

        try
        {
            AnimateToTray();
        }
        catch (Exception ex)
        {
            // Если анимация упала — хотя бы скроем окно
            Hide();
            System.Diagnostics.Debug.WriteLine($"AnimateToTray failed: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }

    #endregion

    
}