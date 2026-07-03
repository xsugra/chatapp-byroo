using System.IO;
using System.Windows;
using ChatApp.Client.Services;
using ChatApp.Client.ViewModels;
using ChatApp.Client.Views;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Client;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Načítaj .env zo záhlavia repozitára (ak existuje) - hodnoty sa nastavia ako
        // skutočné environment premenné, ktoré nižšie prečíta AddEnvironmentVariables().
        var envDir = AppContext.BaseDirectory;
        while (envDir is not null && !File.Exists(Path.Combine(envDir, ".env")))
            envDir = Directory.GetParent(envDir)?.FullName;
        if (envDir is not null)
            Env.Load(Path.Combine(envDir, ".env"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
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

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is not null)
        {
            var chatService = _serviceProvider.GetService<IChatService>();
            if (chatService is not null)
                await chatService.DisconnectAsync();

            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }

        base.OnExit(e);
    }
}