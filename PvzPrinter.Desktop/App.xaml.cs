using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using PvzPrinter.Core.Communication;
using PvzPrinter.Core.Configuration;
using PvzPrinter.Core.Logging;
using PvzPrinter.Core.Printing;
using Application = System.Windows.Application;

namespace PvzPrinter.Desktop;


public partial class App : Application
{

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // DI контейнер
        var services = new ServiceCollection();

        services.AddSingleton<TwoLevelLabelCache>();
        services.AddSingleton<ILogger, FileLogger>();
        services.AddSingleton<IConfigProvider, JsonConfigProvider>();
        services.AddSingleton<ILabelRenderer>(sp =>
        new LabelRenderer(sp.GetRequiredService<ILogger>()));

        services.AddSingleton<IPrinterService>(sp =>
    new LabelPrinter(
        sp.GetRequiredService<IConfigProvider>(),
        sp.GetRequiredService<ILogger>(),
        sp.GetRequiredService<ILabelRenderer>(),
        sp.GetRequiredService<TwoLevelLabelCache>()
    ));
        services.AddSingleton<PrintQueueService>();
        services.AddSingleton<IWebSocketServer, OzonWebSocketServer>();

        Services = services.BuildServiceProvider();

        // Запускаем WebSocket-сервер
        var wsServer = Services.GetRequiredService<IWebSocketServer>();
        wsServer.Start();

        // Подписываем на команды печати
        var printer = Services.GetRequiredService<IPrinterService>();
        var printQueue = Services.GetRequiredService<PrintQueueService>();
        wsServer.PrintRequested += cmd =>
        {
            printQueue.Enqueue(cmd.CellCode);
        };

        // Проверяем, запущены ли из автозагрузки
        bool isAutoStart = e.Args.Contains("--autostart", StringComparer.OrdinalIgnoreCase);

        // Регистрируем в автозагрузке (если ещё не зарегистрированы)
        RegisterInAutoStart();

        // Создаём окно
        var mainWindow = new MainWindow();

        if (isAutoStart)
        {
            // При автозапуске: создаём трей-иконку, но НЕ показываем окно
            mainWindow.Hide(); // Окно скрыто, но NotifyIcon уже создан в конструкторе
        }
        else
        {
            // При обычном запуске: показываем окно нормально
            mainWindow.Show();
        }
    }

    private static void RegisterInAutoStart()
    {
        const string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string appName = "PvzPrinter";

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyName, writable: true);
            if (key == null) return;

            var currentValue = key.GetValue(appName) as string;
            var exePath = Environment.ProcessPath ?? "";

            // Если уже зарегистрирован с правильным путём — ничего не делаем
            if (currentValue != null && currentValue.Contains(exePath))
                return;

            // Регистрируем с флагом --autostart
            key.SetValue(appName, $"\"{exePath}\" --autostart");
        }
        catch
        {
            // Тихий fail: если нет прав на реестр — просто пропускаем
            // Логгер здесь недоступен (DI ещё не собран), поэтому молча игнорируем
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        // СНАЧАЛА останавливаем очередь (ждём завершения текущей печати)
        var printQueue = Services.GetService<PrintQueueService>();
        printQueue?.Dispose();

        // ПОТОМ останавливаем WebSocket
        var wsServer = Services.GetService<IWebSocketServer>();
        wsServer?.Stop();

        base.OnExit(e);
    }
}