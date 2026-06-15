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

public partial class RegistrationViewModel
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
        
        this.WhenAnyValue(
            x => x.NewUser.UserName, 
            x => x.NewUser.Email, 
            x => x.NewUser.Password, 
            x => x.ConfirmPassword)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Subscribe(_ => ErrorMessage = string.Empty);
    }
    
    [Reactive] public User NewUser { get; set; } = new User();
    [Reactive] public string ConfirmPassword { get; set; } = string.Empty;
    [Reactive] public string ErrorMessage { get; set; } = string.Empty;
    [Reactive] public bool IsLoading { get; set; } = false;
    [Reactive] public bool CanRegister { get; set; } = false;
    
    [ReactiveCommand]
    private async Task Register()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            if (string.IsNullOrWhiteSpace(NewUser.UserName) || NewUser.UserName.Length < 3)
            {
                ErrorMessage = "Логин должен содержать минимум 3 символа";
                return;
            }
            
            if (!_emailRegex.IsMatch(NewUser.Email))
            {
                ErrorMessage = "Введите корректный Email (пример: user@mail.com)";
                return;
            }
            
            if (!_passwordRegex.IsMatch(NewUser.Password))
            {
                ErrorMessage = "Пароль должен содержать минимум 4 символа";
                return;
            }
            
            if (NewUser.Password != ConfirmPassword)
            {
                ErrorMessage = "Пароли не совпадают";
                return;
            }
            
            if (_dbService.CheckUserExists(NewUser.UserName))
            {
                ErrorMessage = "Пользователь с таким логином уже существует";
                return;
            }
            
            var success = _dbService.RegisterUser(NewUser.UserName, NewUser.Password, NewUser.Email);
            
            if (success)
            {
                await _closeWindow.Handle(Unit.Default);
            }
            else
            {
                ErrorMessage = "Ошибка при регистрации. Попробуйте позже.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
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