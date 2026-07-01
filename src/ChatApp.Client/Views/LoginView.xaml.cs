using System.Windows;
using ChatApp.Client.ViewModels;

namespace ChatApp.Client.Views;

public partial class LoginView : Window
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}