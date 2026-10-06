using System.Windows;
using DesktopRpa.App.Services;
using DesktopRpa.App.ViewModels;
using DesktopRpa.App.Views;
using DesktopRpa.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopRpa.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IStatementParser, StatementParser>();
        services.AddSingleton<IRegistryParser, RegistryParser>();
        services.AddSingleton<IReconciliationEngine, ReconciliationEngine>();
        services.AddSingleton<IReportExporter, ReportExporter>();
        services.AddSingleton<ITestDataGenerator, TestDataGenerator>();
        services.AddSingleton<IAutomationService, AutomationService>();

        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IClipboardService, ClipboardService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
