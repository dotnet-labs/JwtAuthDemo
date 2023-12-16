namespace JwtAuthDemo.Services;

public interface IUserService
{
    bool IsAnExistingUser(string userName);
    bool IsValidUserCredentials(string userName, string password);
    string GetUserRole(string userName);
}

public class UserService(ILogger<UserService> logger) : IUserService
{
    private readonly Dictionary<string, string> _users = new()
    {
        { "test1", "password1" },
        { "test2", "password2" },
        { "admin", "securePassword" }
    };
    // inject your database here for user validation

    public bool IsValidUserCredentials(string userName, string password)
    {
        logger.LogInformation("Validating user [{userName}]", userName);
        if (string.IsNullOrWhiteSpace(userName))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        return _users.TryGetValue(userName, out var p) && p == password;
    }

    public bool IsAnExistingUser(string userName)
    {
        return _users.ContainsKey(userName);
    }

    public string GetUserRole(string userName)
    {
        if (!IsAnExistingUser(userName))
        {
            return string.Empty;
        }

        if (userName == "admin")
        {
            return UserRoles.Admin;
        }

        return UserRoles.BasicUser;
    }
}

public static class UserRoles
{
    public const string Admin = nameof(Admin);
    public const string BasicUser = nameof(BasicUser);
}