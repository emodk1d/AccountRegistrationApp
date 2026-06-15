namespace AccountRegistrationApp.Models;

public class Admin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;

    public User? User { get; set; }
}