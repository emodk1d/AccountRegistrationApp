using System;
using System.Collections.Generic;
using System.IO;
using AccountRegistrationApp.Models;
using Microsoft.Data.Sqlite;

namespace AccountRegistrationApp.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        // База данных будет в папке приложения
        var dbPath = Path.Combine(AppContext.BaseDirectory, "users.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    /// <summary>
    /// Создание таблицы пользователей при первом запуске
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                IsAdmin INTEGER NOT NULL DEFAULT 0,
                RegisteredAt TEXT NOT NULL
            );
            
            -- Создаем индекс для быстрого поиска по логину
            CREATE INDEX IF NOT EXISTS idx_username ON Users(Username);
        ";

        using var command = new SqliteCommand(sql, connection);
        command.ExecuteNonQuery();

        // Создаем администратора по умолчанию, если таблица пуста
        var adminExists = CheckUserExists("admin");
        if (!adminExists)
        {
            RegisterUser("admin", "admin", isAdmin: true);
        }
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    public bool RegisterUser(string username, string password, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = @"
            INSERT INTO Users (Username, Password, IsAdmin, RegisteredAt)
            VALUES (@username, @password, @isAdmin, @registeredAt)
        ";

        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password); 
        command.Parameters.AddWithValue("@isAdmin", isAdmin ? 1 : 0);
        command.Parameters.AddWithValue("@registeredAt", DateTime.Now.ToString("o"));

        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException ex) when (ex.Message.Contains("UNIQUE"))
        {
            return false;
        }
    }

    /// <summary>
    /// Проверка существования пользователя
    /// </summary>
    public bool CheckUserExists(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT COUNT(*) FROM Users WHERE Username = @username";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);

        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    /// <summary>
    /// Авторизация пользователя
    /// </summary>
    public User? Login(string username, string password)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var sql =
            "SELECT Id, Username, Password, IsAdmin, RegisteredAt FROM Users WHERE Username = @username AND Password = @password";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password);

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Password = reader.GetString(2), // Теперь храним пароль
                IsAdmin = reader.GetInt32(3) == 1,
                RegisteredAt = DateTime.Parse(reader.GetString(4))
            };
        }

        return null;
    }

    /// <summary>
    /// Получение всех пользователей (для админа)
    /// </summary>
    public List<User> GetAllUsers()
    {
        var users = new List<User>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT Id, Username, Password, IsAdmin, RegisteredAt FROM Users ORDER BY Id";
        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Password = reader.GetString(2),
                IsAdmin = reader.GetInt32(3) == 1,
                RegisteredAt = DateTime.Parse(reader.GetString(4))
            });
        }

        return users;
    }

    /// <summary>
    /// Удаление пользователя (только для админа)
    /// </summary>
    public bool DeleteUser(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "DELETE FROM Users WHERE Id = @id AND IsAdmin = 0";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@id", userId);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Проверка, является ли пользователь админом
    /// </summary>
    public bool IsAdmin(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var sql = "SELECT IsAdmin FROM Users WHERE Username = @username";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);

        var result = command.ExecuteScalar();
        return result != null && Convert.ToInt32(result) == 1;
    }
}