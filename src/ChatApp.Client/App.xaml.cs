using System.IO;
using System.Windows;
using ChatApp.Client.Services;
using ChatApp.Client.ViewModels;
using ChatApp.Client.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Client;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();

        // Configuration
        services.AddSingleton<IConfiguration>(configuration);

        // Services
        services.AddSingleton<IChatService, ChatService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();

        // Views
        services.AddTransient<LoginView>();
        services.AddTransient<MainView>();

        _serviceProvider = services.BuildServiceProvider();

        var loginView = _serviceProvider.GetRequiredService<LoginView>();
        loginView.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();

        base.OnExit(e);
    }
}