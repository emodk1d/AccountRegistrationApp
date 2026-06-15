using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountRegistrationApp.Models;
using AccountRegistrationApp.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AccountRegistrationApp.ViewModels;

public partial class RegistrationViewModel : ReactiveObject
{
    private readonly DatabaseService _dbService;
    private readonly Interaction<Unit, bool> _closeWindow = new();
    public Interaction<Unit, bool> CloseWindow => _closeWindow;

    private readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private readonly Regex _passwordRegex = new Regex(@"^.{4,}$");

    public RegistrationViewModel(DatabaseService dbService)
    {
        _dbService = dbService;

        this.WhenAnyValue(
                x => x.NewUser.UserName,
                x => x.NewUser.Email,
                x => x.NewUser.Password,
                x => x.ConfirmPassword,
                (user, email, pass, confirm) =>
                    !string.IsNullOrWhiteSpace(user) &&
                    user.Length >= 3 &&
                    _emailRegex.IsMatch(email) &&
                    _passwordRegex.IsMatch(pass) &&
                    pass == confirm
            )
            .Subscribe(canReg => CanRegister = canReg);
    }

    [Reactive] public User NewUser { get; set; } = new User();
    [Reactive] public string ConfirmPassword { get; set; } = string.Empty;
    [Reactive] public bool IsLoading { get; set; }
    [Reactive] public bool CanRegister { get; set; }

    [ReactiveCommand]
    private async Task Register()
    {
        IsLoading = true;

        try
        {
            if (string.IsNullOrWhiteSpace(NewUser.UserName) || NewUser.UserName.Length < 3)
                return;

            if (!_emailRegex.IsMatch(NewUser.Email))
                return;

            if (!_passwordRegex.IsMatch(NewUser.Password))
                return;

            if (NewUser.Password != ConfirmPassword)
                return;

            if (_dbService.CheckUserExists(NewUser.UserName))
                return;

            var success = _dbService.RegisterUser(NewUser.UserName, NewUser.Password, NewUser.Email);

            if (success)
            {
                await _closeWindow.Handle(Unit.Default);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [ReactiveCommand]
    private void GoBack()
    {
        _closeWindow.Handle(Unit.Default).Subscribe();
    }
}