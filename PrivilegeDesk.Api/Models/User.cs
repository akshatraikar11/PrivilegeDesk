namespace PrivilegeDesk.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    // We never store the real password — only a BCrypt hash.
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
}
