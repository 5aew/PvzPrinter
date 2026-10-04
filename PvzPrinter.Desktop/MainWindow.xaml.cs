using System.Drawing;
using System.Windows;
using System.Windows.Forms;

using Application = System.Windows.Application;

namespace PvzPrinter.Desktop;

public partial class MainWindow : Window
{
    private NotifyIcon? _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "PvzPrinter",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Открыть", null, (_, _) => ShowWindow());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Выход", null, (_, _) => ExitApplication());

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
    }

    // ← ЭТОТ МЕТОД КРИТИЧЕСКИ ВАЖЕН! Без него крестик закрывает окно
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;   // Отменяем реальное закрытие
        Hide();            // Прячем окно (оно уходит в трей)
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _trayIcon?.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnClosed(e);
    }
}