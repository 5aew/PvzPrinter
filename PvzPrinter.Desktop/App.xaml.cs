using System.Windows;

using Microsoft.Extensions.DependencyInjection;

using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;
using PvzPrinter.Core.Printing;

namespace PvzPrinter.Desktop;

using Application = System.Windows.Application;  // ← WPF Application
using NotifyIcon = System.Windows.Forms.NotifyIcon;  // ← явное указание
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using ToolStripSeparator = System.Windows.Forms.ToolStripSeparator;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // DI контейнер
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, FileLogger>();
        services.AddSingleton<IConfigProvider, JsonConfigProvider>();
        services.AddTransient<IPrinterService, LabelPrinter>();
        Services = services.BuildServiceProvider();

        // Создаём ТОЛЬКО ОДНО окно
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}