using AccountRegistrationApp.Services;
using AccountRegistrationApp.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AccountRegistrationApp.Views;

public partial class RegistrationWindow : Window
{
    public RegistrationWindow()
    {
        InitializeComponent();
        
        // var viewModel = new RegistrationViewModel(dbService);
        // DataContext = viewModel;
        //
        // viewModel.CloseWindow.RegisterHandler(async interaction =>
        // {
        //     var loginWindow = new LoginWindow();
        //     loginWindow.Show();
        //     Close();
        //     interaction.SetOutput(true);
        // });
    }
}