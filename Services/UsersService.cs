using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace DQVMsManagement.Services
{
    public class UsersService
    {
        private const string Path = @"C:\users.json";
        private List<UserRecord> _users;

        public UsersService()
        {
            if (!File.Exists(Path))
                File.WriteAllText(Path, "[]");
            _users = JsonConvert.DeserializeObject<List<UserRecord>>(File.ReadAllText(Path))
                     ?? new List<UserRecord>();

            if (_users.Count == 0)
            {
                Create("admin", "Admin@123", "Admin", out _);
            }
        }

        public IEnumerable<UserRecord> GetAll() => _users;

        public bool Validate(string username, string password, out string role)
        {
            role = "";
            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                && u.IsActive);
            if (user == null) return false;
            var hash = Hash(password);
            if (hash != user.PasswordHash) return false;
            role = user.Role;
            return true;
        }

        public bool Create(string username, string password, string role, out string error)
        {
            error = "";
            if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                error = "Username already exists.";
                return false;
            }
            var record = new UserRecord
            {
                Username    = username,
                PasswordHash= Hash(password),
                Role        = role,
                IsActive    = true
            };
            _users.Add(record);
            Save();
            return true;
        }

        public bool Delete(string username, out string error)
        {
            error = "";
            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                error = "User not found.";
                return false;
            }
            _users.Remove(user);
            Save();
            return true;
        }

        public bool ToggleActive(string username, out string error)
        {
            error = "";
            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                error = "User not found.";
                return false;
            }
            user.IsActive = !user.IsActive;
            Save();
            return true;
        }

        public bool ChangePassword(string username, string newPassword, out string error)
        {
            error = "";
            var user = _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                error = "User not found.";
                return false;
            }
            user.PasswordHash = Hash(newPassword);
            Save();
            return true;
        }

        private void Save()
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(_users, Formatting.Indented));
        }

        private static string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public void SetSessionId(string username, string sessionId)
{
    var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    if (user != null)
    {
        user.SessionId = sessionId;
        Save();
    }
}

public string? GetSessionId(string username)
{
    return _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                 ?.SessionId;
}

    }

public class UserRecord
{
    public string Username     { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role         { get; set; } = "User";
    public bool   IsActive     { get; set; } = true;

    // <<< New: track current valid session
    public string? SessionId   { get; set; }
}

}
