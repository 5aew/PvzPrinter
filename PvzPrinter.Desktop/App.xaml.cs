using System.Windows;

using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<ILogger, FileLogger>();
        services.AddSingleton<IConfigProvider, JsonConfigProvider>();
        services.AddTransient<IPrinterService, LabelPrinter>();
        Services = services.BuildServiceProvider();

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
}