using System.Text.Json.Serialization;

namespace AuthenticationMAUI.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserAuthData? UserData { get; set; }
    public AuthTokens? Tokens { get; set; }

    public static AuthResult Successful(UserAuthData? userData, AuthTokens tokens) =>
        new() { Success = true, UserData = userData, Tokens = tokens };

    public static AuthResult Failed(string errorMessage) => 
        new() { Success = false, ErrorMessage = errorMessage };
}

public record UserAuthData(
    bool IsEmailVerified,
    string? DisplayName,
    string? PhotoUrl,
    string? PhoneNumber,
    string UserId = "",
    string Email = "",
    string Provider = "");

public class AuthTokens
{
    /// <summary>
    /// Токен ID - недолговечный токен
    /// </summary>
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = string.Empty;


    /// <summary>
    /// Refresh токен - долговечный токен
    /// </summary>
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Срок жизни токена ID
    /// </summary>
    [JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; set; } = string.Empty;

    /// <summary>
    /// Время, когда истечет срок жизни токена ID
    /// </summary>
    public DateTime TokenExpiry { get; set; }
}
