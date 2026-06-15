using System;

namespace AccountRegistrationApp.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; 
    public bool IsAdmin { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.Now;
}