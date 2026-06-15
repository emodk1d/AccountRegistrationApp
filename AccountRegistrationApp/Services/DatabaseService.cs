using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using AccountRegistrationApp.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AccountRegistrationApp.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                Email TEXT NOT NULL,
                IsAdmin INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL
            );
            
        ");

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Admins (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL UNIQUE,
                FullName TEXT NOT NULL,
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );
        ");


        if (GetAllUsers().Count == 0)
        {
            var adminUser = new User
            {
                UserName = "admin",
                Password = "admin123",
                Email = "admin@example.com",
                IsAdmin = true,
                CreatedAt = DateTime.Now
            };

            InsertUser(adminUser);

            var createdUser = Login(adminUser.UserName, adminUser.Password);
            if (createdUser != null)
            {
                AddAdmin(new Admin
                {
                    UserId = createdUser.Id,
                    FullName = "Главный Администратор"
                });
            }
        }
    }


    public bool InsertUser(User user)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO Users (Username, Password, Email, IsAdmin, CreatedAt)
            VALUES (@Username, @Password, @Email, @IsAdmin, @CreatedAt)
        ";

        var result = connection.Execute(sql, new
        {
            user.UserName,
            user.Password,
            user.Email,
            IsAdmin = user.IsAdmin ? 1 : 0,
            CreatedAt = user.CreatedAt.ToString("o")
        });

        return result > 0;
    }

    public bool RegisterUser(string username, string password, string email)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        var user = new User
        {
            UserName = username,
            Password = password,
            Email = email,
            IsAdmin = false,
            CreatedAt = DateTime.Now
        };

        try
        {
            return InsertUser(user);
        }
        catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
        {
            return false;
        }
    }

    public bool CheckUserExists(string username)
    {
        using var connection = CreateConnection();
        var sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
        return connection.ExecuteScalar<int>(sql, new { Username = username }) > 0;
    }

    public User? Login(string username, string password)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT Id, Username, Password, Email, IsAdmin, CreatedAt
            FROM Users 
            WHERE Username = @Username AND Password = @Password
        ";

        var user = connection.QueryFirstOrDefault<User>(sql, new { Username = username, Password = password });

        if (user != null)
        {
            user.CreatedAt = DateTime.Parse(user.CreatedAt.ToString());
        }

        return user;
    }

    public List<User> GetAllUsers()
    {
        using var connection = CreateConnection();
        var sql = "SELECT Id, Username, Password, Email, IsAdmin, CreatedAt FROM Users ORDER BY Id";

        var users = connection.Query<User>(sql).ToList();
        foreach (var user in users)
        {
            user.CreatedAt = DateTime.Parse(user.CreatedAt.ToString());
        }

        return users;
    }

    public bool DeleteUser(int userId)
    {
        using var connection = CreateConnection();

        var isAdmin = connection.ExecuteScalar<int>("SELECT IsAdmin FROM Users WHERE Id = @Id", new { Id = userId }) ==
                      1;
        if (isAdmin) return false;

        return connection.Execute("DELETE FROM Users WHERE Id = @Id", new { Id = userId }) > 0;
    }

    public bool AddAdmin(Admin admin)
    {
        using var connection = CreateConnection();
        var sql = "INSERT INTO Admins (UserId, FullName) VALUES (@UserId, @FullName)";
        return connection.Execute(sql, admin) > 0;
    }
}