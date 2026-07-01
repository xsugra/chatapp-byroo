using System.Windows;
using ChatApp.Client.Services;
using ChatApp.Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Client.ViewModels;

public partial class LoginViewModel(IChatService chatService, IServiceProvider serviceProvider) : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Please enter a username.";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var response = await chatService.LoginAsync(Username.Trim());
            if (response is null)
            {
                ErrorMessage = "Login failed. Try again.";
                return;
            }

            // Pripoj sa na SignalR
            await chatService.ConnectAsync();

            // Otvor hlavné okno
            var mainView = serviceProvider.GetRequiredService<MainView>();
            var mainVm = serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.Initialize(response.UserId, response.Username);
            mainView.DataContext = mainVm;
            mainView.Show();

            // Zatvor login okno
            Application.Current.Windows
                .OfType<LoginView>()
                .FirstOrDefault()?.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}