using Firebase.Auth;
using Firebase.Auth.Providers;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace AuthenticationMaui.Services;

public class FirebaseLoginService : ILoginService
{
    #region inner class FirebaseLoginData
    public class FirebaseLoginData
    {
        public IUserStorageService? UserStorageService { get; set; }
        public string? ApiKey { get; set; }
        public string? AuthDomain { get; set; }
        public string? GoogleClientId { get; set; }
        public string? GoogleRedirectUri { get; set; }
        public string? CallbackScheme { get; set; }
    } 
    #endregion

    #region Private Variables
    private FirebaseAuthClient authClient;
    private IUserStorageService userStorageService;
    private string googleClientId;
    private string googleRedirectUri;
    private string callbackScheme;
    #endregion

    #region Constructor
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="FirebaseLoginService"/>.
    /// </summary>
    /// <param name="data">FirebaseLoginData - объект, который содержит необходимые для аутентификации данные: 
    /// IUserStorageService UserStorageService - сервис, через который будут сохраняться логин и email пользователя;
    /// string ApiKey - Ваш Web API Key из Firebase Console (Firebase Console > Project Settings > General > "Web API Key");
    /// string AuthDomain - Обычно это your-project-id.firebaseapp.com (Firebase Console > Authentication > Settings > "Authorized domains");
    /// string GoogleClientId - Ваш Google Client ID (Firebase Console > Authentication > Sign-in method > Google > Web SDK configuration > "Web client ID");
    /// string GoogleRedirectUri - Обычно это https://your-project-id.firebaseapp.com/__/auth/handler (Google Cloud Console > APIs & Services > Credentials > )Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs</param>
    public FirebaseLoginService(FirebaseLoginData data)
    {
        var authConfig = new FirebaseAuthConfig
        {
            ApiKey = data.ApiKey,
            AuthDomain = data.AuthDomain,
            Providers =
            [
                new EmailProvider(),
                new GoogleProvider()
            ],

        };

        authClient = new FirebaseAuthClient(authConfig);
        userStorageService = data.UserStorageService ?? throw new ArgumentNullException(nameof(userStorageService), "User storage service cannot be null.");
        googleClientId = data.GoogleClientId ?? throw new ArgumentNullException(nameof(data.GoogleClientId), "Google Client ID cannot be null.");
        googleRedirectUri = data.GoogleRedirectUri ?? throw new ArgumentNullException(nameof(data.GoogleRedirectUri), "Google Redirect URI cannot be null.");

        var _callbackScheme = data.CallbackScheme ?? throw new ArgumentNullException(nameof(data.CallbackScheme), "Callback scheme cannot be null.");
        callbackScheme = _callbackScheme.EndsWith("://") ? _callbackScheme : $"{_callbackScheme}://"; // добавляем схему, если не указана
    }
    #endregion

    #region LoginWithEmailAsync Method
    /// <summary>
    /// Вход с помощью логина (или электронной почты) и пароля.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <param name="password">Пароль аккаунта (не от почты).</param>
    /// <returns>В случае успеха возвращается true, инача - false.</returns>
    /// <exception cref="FirebaseAuthException">Обработка ошибок аутентификации. Например, неверный пароль или адрес электронной почты.</exception>
    /// <exception cref="Exception">Обработка других ошибок. Например, в хранилище типа IUserStorageService не найден логин.</exception>
    public async Task<bool> LoginWithEmailAsync(string loginOrEmail, string password)
    {
        try
        {
            var email = IsValidEmail(loginOrEmail)
                ? loginOrEmail
                : await userStorageService.GetEmailByLoginAsync(loginOrEmail) 
                    ?? throw new Exception("Login not found in the IUserStorageService.");

            if (!IsValidPassword(password))
                throw new ArgumentException("Password is not valid.");

            var result = await authClient.SignInWithEmailAndPasswordAsync(email, password);
            return !string.IsNullOrEmpty(result?.User?.Info?.Email);
        }
        catch (FirebaseAuthException ex)
        {
            throw new FirebaseAuthException($"Login failed: {ex.Message}", ex.Reason);
        }
        catch (Exception ex)
        {
            throw new Exception($"Login error: {ex.Message}", ex);
        } 
    }
    #endregion

    #region LoginWithGoogleAsync Method
    /// <summary>
    /// Вход с помощью Google аккаунта.
    /// </summary>
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> LoginWithGoogleAsync()
    {
        // WinUI не поддерживает WebAuthenticator.
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
            return false;

        try
        {
            var startUri = new Uri(
              "https://accounts.google.com/o/oauth2/v2/auth" +
              $"?client_id={googleClientId}" +
              $"&redirect_uri={googleRedirectUri}" +
              "&response_type=id_token" +
              "&scope=openid%20email%20profile" +
              $"&nonce={Guid.NewGuid()}");

            var endUri = new Uri($"{callbackScheme}");

            var authResult = await WebAuthenticator.AuthenticateAsync(startUri, endUri);

            if (!authResult.Properties.TryGetValue("id_token", out var idToken))
                throw new Exception("Google authentication failed: No code received.");

            var credential = GoogleProvider.GetCredential(idToken, OAuthCredentialTokenType.IdToken);

            var result = await authClient.SignInWithCredentialAsync(credential);

            return !string.IsNullOrEmpty(result?.User?.Info?.Email);
        }
        catch (Exception ex)
        {
            throw new Exception($"Google Login failed: {ex.Message}", ex);
        }
    }
    #endregion

    #region Logout Method
    /// <summary>
    /// Выход из аккаунта.
    /// </summary>
    public void Logout()
    {
        try
        {
            authClient.SignOut();
        }
        catch (Exception ex)
        {
            throw new Exception($"Logout error: {ex.Message}");
        }
    }
    #endregion

    #region RegisterWithEmailAsync Method
    /// <summary>
    /// Регистрация с помощью электронной почты и пароля.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="email">Адрес электронной почты.</param>
    /// <param name="password">Пароль аккаунта (не от почты).</param>
    /// <returns>true случае успеха; иначе - false</returns>
    /// <exception cref="FirebaseAuthException">Обработка ошибок аутентификации. Например, неверный пароль или адрес электронной почты.</exception>
    /// <exception cref="Exception">Обработка других ошибок.Например, невалидная почта или невалидный пароль.</exception>
    public async Task<bool> RegisterWithEmailAsync(string login, string email, string password)
    {
        try
        {
            if (!IsValidEmail(email))
                throw new ArgumentException("Email is not valid.");
            if (!IsValidPassword(password))
                throw new ArgumentException("Password is not valid.");

            // проверяем, существует ли логин
            if (!await userStorageService.LoginExistsAsync(login))
                await userStorageService.AddUserAsync(login, email);

            var registrationResult = await authClient.CreateUserWithEmailAndPasswordAsync(email, password, login);

            return !string.IsNullOrEmpty(registrationResult?.User?.Info?.Email);
        }
        catch (FirebaseAuthException ex)
        {
            throw new FirebaseAuthException($"Register failed: {ex.Message}", ex.Reason);
        }
        catch (Exception ex)
        {
            throw new Exception($"Register error: {ex.Message}", ex);
        }
    }
    #endregion

    #region SendPasswordResetEmailAsync Method
    /// <summary>
    /// Восстановление пароля по логину или электронной почте.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <returns>true в случае успеха; иначе - false.</returns>
    /// <exception cref="Exception">Ошибка сброса пароля. Например, неверный email или логин.</exception>
    public async Task SendPasswordResetEmailAsync(string loginOrEmail)
    {
        try
        {
            var email = IsValidEmail(loginOrEmail)
                ? loginOrEmail
                : await userStorageService.GetEmailByLoginAsync(loginOrEmail)
                    ?? throw new Exception("Login not found in the IUserStorageService.");
            
            await authClient.ResetEmailPasswordAsync(loginOrEmail);
            // очистка хранилища
            userStorageService.RemoveUserAsync(loginOrEmail);
        }
        catch (Exception ex)
        {
            throw new Exception($"Password reset error: {ex.Message}", ex);
        }
    }
    #endregion

    #region IsValidEmail Method
    /// <summary>
    /// Определяет, имеет ли указанный адрес электронной почты правильный формат.
    /// </summary>
    /// <param name="email">Адрес электронной почты для проверки.</param>
    /// <returns>true, если адрес электронной почты указан в правильном формате; иначе - false. </returns>
    private bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false; 

        try
        {
            MailAddress mail = new(email);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
    #endregion

    #region IsValidPassword Method
    /// <summary>
    /// Проверяет, соответствует ли строка пароля требованиям:
    /// - минимум 6 символов;
    /// - допускаются латинские буквы, цифры, спецсимволы и пробелы.
    /// </summary>
    /// <param name="password">Пароль для проверки.</param>
    /// <returns>true, если пароль соответствует требованиям; иначе - false.</returns>
    private bool IsValidPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        // Проверяем, что пароль содержит минимум 6 символов
        if (password.Length < 6) return false;
        // Проверяем, что пароль состоит только из латинских букв, цифр, спецсимволов и пробелов
        var regex = new Regex(@"^[a-zA-Z0-9~`!@#$%^&*()\-_+=|}\]{\["":;?\/>.<,\s]{6,}$");
        return regex.IsMatch(password);
    }
    #endregion
}
