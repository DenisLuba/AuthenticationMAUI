using AuthenticationMAUI.Models;
using Firebase.Auth;
using Firebase.Auth.Providers;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AuthenticationMaui.Services;

public partial class FirebaseLoginService : ILoginService
{
    #region Public Inner Classes
    public class FirebaseLoginData
    {
        public IUserStorageService? UserStorageService { get; set; }
        public string? ApiKey { get; set; }
        public string? AuthDomain { get; set; }
        public string? GoogleClientId { get; set; }
        public string? GoogleRedirectUri { get; set; }
        public string? CallbackScheme { get; set; }
        public string? SecretKey { get; set; }
        public string? FacebookAppId { get; set; }
        public string? FacebookRedirectUri { get; set; }

    }
    #endregion

    #region Private Inner Classes
    private class PhoneCodeResponse
    {
        [JsonPropertyName("sessionInfo")]
        public string SessionInfo { get; set; } = string.Empty;
    }

    private class PhoneLoginResponse
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;
    }
    #endregion

    #region Private Variables
    private readonly FirebaseAuthClient authClient;
    private readonly IUserStorageService userStorageService;
    private readonly string? googleClientId;
    private readonly string? googleRedirectUri;
    private readonly string? callbackScheme;
    private readonly string apiKey;
    private static readonly Lazy<HttpClient> httpClient = new();
    private static PhoneAuthSessionResult? sessionResult;
    private readonly string? secretKey;
    private readonly string? authDomain;
    private readonly string? facebookAppId;
    private readonly string? facebookRedirectUri;
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
    /// string GoogleRedirectUri - Обычно это https://your-project-id.firebaseapp.com/__/auth/handler (Google Cloud Console > APIs & Services > Credentials > Auth 2.0 Client IDs > Web client (auto created by Google Service) > Authorized redirect URIs)</param>
    /// string CallbackScheme - Схема обратного вызова для аутентификации через Google. Например, "todolist" для todolist://.
    /// string SecretKey - Ваш Secret Key для reCAPTCHA (Google Cloud Console > Security > reCAPTCHA > > reCAPTCHA Keys > Key details > (Continue with the instructions) Use legacy key).
    public FirebaseLoginService(FirebaseLoginData data)
    {
        var authConfig = new FirebaseAuthConfig
        {
            ApiKey = data.ApiKey,
            AuthDomain = data.AuthDomain,
            Providers =
            [
                new EmailProvider(),
                new GoogleProvider(),
                new FacebookProvider(),
            ],
        };

        authClient = new FirebaseAuthClient(authConfig);
        apiKey = data.ApiKey ?? throw new ArgumentNullException(nameof(data.ApiKey), "API Key cannot be null.");
        userStorageService = data.UserStorageService ?? throw new ArgumentNullException(nameof(userStorageService), "User storage service cannot be null.");

        googleClientId = data.GoogleClientId;
        googleRedirectUri = data.GoogleRedirectUri;
        var _callbackScheme = data.CallbackScheme;
        callbackScheme = _callbackScheme is not null && _callbackScheme.EndsWith("://") ? _callbackScheme : $"{_callbackScheme}://"; // добавляем схему, если не указана

        secretKey = data.SecretKey;
        authDomain = data.AuthDomain?.Replace("http", "").Replace("https", "").Replace("://", "").Trim() ?? "";
        authDomain = authDomain.EndsWith("/") ? authDomain.Replace("/", "") : authDomain;

        facebookAppId = data.FacebookAppId;
        facebookRedirectUri = data.FacebookRedirectUri;
    }
    #endregion

    #region LoginWithEmailAsync Method
    /// <summary>
    /// Вход с помощью логина (или электронной почты) и пароля.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <param name="password">Пароль аккаунта (не от почты).</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns><see cref="AuthResult"/></returns>
    /// <exception cref="FirebaseAuthException">Обработка ошибок аутентификации. Например, неверный пароль или адрес электронной почты.</exception>
    /// <exception cref="Exception">Обработка других ошибок. Например, в хранилище типа IUserStorageService не найден логин.</exception>
    public async Task<AuthResult> LoginWithEmailAsync(string loginOrEmail, string password, long timeoutMilliseconds)
    {
        try
        {
            var email = IsValidEmail(loginOrEmail)
                ? loginOrEmail
                : await userStorageService.GetEmailByLoginAsync(loginOrEmail)
                    ?? throw new Exception("Login not found in the IUserStorageService.");

            if (!IsValidPassword(password))
                throw new ArgumentException("Password is not valid.");

            return await CallWithTimeout(async () =>
                {
                    var userCredential = await authClient.SignInWithEmailAndPasswordAsync(email, password);

                    if (string.IsNullOrEmpty(userCredential?.User?.Uid))
                        return AuthResult.Failed("Authentication failed.");

                    return await GetAuthResult(userCredential, "email");
                },
                timeoutMilliseconds);
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
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns><see cref="AuthResult"/></returns>
    /// <exception cref="Exception">Исключение в случае неудачи авторизации через Google.</exception>
    public async Task<AuthResult> LoginWithGoogleAsync(long timeoutMilliseconds)
    {
        // WinUI не поддерживает WebAuthenticator.
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
            throw new InvalidOperationException("Google login not supported on Windows.");
        if (callbackScheme is null)
            throw new InvalidOperationException("CallbackScheme must be set for Google login.");
        if (googleRedirectUri is null)
            throw new InvalidOperationException("Google Redirect URI must be set for Google login.");

        try
        {
#if WINDOWS
        throw new NotSupportedException("Google login is not supported on Windows platform. Use LoginWithEmailAsync instead.");
#else
            return await CallWithTimeout(async () =>
            {
                var appScheme = callbackScheme[..(callbackScheme.Length - "://".Length)];

                var startUri = new Uri(
                  "https://accounts.google.com/o/oauth2/v2/auth" +
                  $"?client_id={googleClientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(googleRedirectUri)}" +
                  "&response_type=id_token" +
                  "&scope=openid%20email%20profile" +
                  $"&nonce={Guid.NewGuid()}" +
                  $"&state={appScheme}");

                var endUri = new Uri($"{callbackScheme}");

                var authResult = await WebAuthenticator.AuthenticateAsync(startUri, endUri);

                if (!authResult.Properties.TryGetValue("id_token", out var idToken))
                    throw new Exception("Google authentication failed: No code received.");

                var credential = GoogleProvider.GetCredential(idToken, OAuthCredentialTokenType.IdToken);

                var userCredential = await authClient.SignInWithCredentialAsync(credential);

                if (string.IsNullOrEmpty(userCredential?.User?.Uid))
                    return AuthResult.Failed("Google authentication failed.");

                return await GetAuthResult(userCredential, "google");
            },
            timeoutMilliseconds);
#endif
        }
        catch (Exception ex)
        {
            throw new Exception($"Google Login failed: {ex.Message}", ex);
        }
    }
    #endregion

    #region LoginWithFacebookAsync Method
    /// <summary>
    /// Вход с помощью аккаунта Facebook.
    /// </summary>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns><see cref="AuthResult"/>></returns>
    public async Task<AuthResult> LoginWithFacebookAsync(long timeoutMilliseconds)
    {

        if (facebookAppId is null)
            throw new InvalidOperationException("Facebook App ID must be set for Facebook login.");
        if (facebookRedirectUri is null)
            throw new InvalidOperationException("Facebook Redirect URI must be set for Facebook login.");

        try
        {
#if WINDOWS
        throw new NotSupportedException("Google login is not supported on Windows platform. Use LoginWithEmailAsync instead.");
#else
            if (callbackScheme is null)
                throw new InvalidOperationException("CallbackScheme must be set for Facebook login.");

            return await CallWithTimeout(async () =>
            {
                var appScheme = callbackScheme[..(callbackScheme.Length - "://".Length)];

                var authUrl = new Uri($"https://www.facebook.com/v17.0/dialog/oauth" +
                    $"?client_id={facebookAppId}" +
                    $"&redirect_uri={facebookRedirectUri}" +
                    $"&scope=public_profile" +
                    $"&response_type=token" +
                    $"&state={appScheme}");
                var callbackUrl = new Uri(callbackScheme);

                // Открываем браузер и получаем access token
                var result = await WebAuthenticator.AuthenticateAsync(authUrl, callbackUrl);
                if (!result.Properties.TryGetValue("access_token", out var accessToken) || string.IsNullOrEmpty(accessToken))
                    throw new Exception("Facebook authentication failed: No access token received.");

                // Превращаем токен Facebook в учетные данные Firebase credential
                var credential = FacebookProvider.GetCredential(accessToken);
                var userCredential = await authClient.SignInWithCredentialAsync(credential);

                if (string.IsNullOrEmpty(userCredential?.User?.Uid))
                    return AuthResult.Failed("Facebook authentication failed.");

                return await GetAuthResult(userCredential, "facebook");
            },
            timeoutMilliseconds);

#endif
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Facebook Login failed: {ex.Message}");
            throw new Exception($"Facebook Login failed: {ex.Message}", ex);
        }
    }
    #endregion

    #region VerifyRecaptchaTokenAsync Method
    /// <summary>
    /// Проверяет токен reCAPTCHA, отправленный с клиентского приложения, с помощью Google reCAPTCHA API.
    /// </summary>
    /// <param name="token">Это одноразовый код, который: 
    /// 1) подтверждает, что пользователь прошёл проверку reCAPTCHA, 
    /// 2) валиден 2 минуты,
    /// 3) может быть проверен только 1 раз.</param>
    /// <param name="timeoutMilliseconds"></param>
    /// <returns>Максимальное время ожидания в миллисекундах для выполнения операции отправки кода.</returns>
    /// <exception cref="InvalidOperationException">Если не был передан siteKey или secretKey.</exception>
    public async Task<bool> VerifyRecaptchaTokenAsync(string token, long timeoutMilliseconds)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("SiteKey and SecretKey must be set for reCAPTCHA verification.");

        const string verifyUrl = "https://www.google.com/recaptcha/api/siteverify";
        var values = new Dictionary<string, string>
        {
            { "secret", secretKey },
            { "response", token }
        };

        using var content = new FormUrlEncodedContent(values);

        return await CallWithTimeout(async () =>
        {
            var response = await httpClient.Value.PostAsync(verifyUrl, content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Лог для отладки
            Trace.WriteLine($"reCAPTCHA verification response: {json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var successProperty) && successProperty.GetBoolean();
        },
        timeoutMilliseconds);
    }
    #endregion

    #region RequestVerificationCodeAsync Method
    /// <summary>
    /// Отправляет запрос на генерацию и доставку проверочного кода на указанный номер телефона.
    /// </summary>
    /// <remarks>Этот метод связывается с внешней службой аутентификации для отправки проверочного кода. 
    /// Убедитесь, что указанный номер телефона действителен и правильно отформатирован.</remarks>
    /// <param name="phoneNumber">Номер телефона, на который будет отправлен проверочный код.</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции отправки кода.</param>
    /// <param name="isTest">Параметр, который указывает в каком режиме идет запрос проверочного кода: в тестовом или в продакшине.</param>
    /// <param name="shell">Параметр типа <see cref="Shell"/></param>
    /// <returns>true, если запрос отправлен успешно, иначе - false</returns>
    /// <exception cref="Exception">Выбрасывается, если запрос завершился неудачей или ответ не содержит необходимой информации о сессии.</exception>
    public async Task<bool> RequestVerificationCodeAsync(string phoneNumber, long timeoutMilliseconds, bool isTest, Shell? shell = null)
    {
        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
        if (!IsValidPhoneNumber(normalizedPhoneNumber))
            throw new ArgumentException("Phone number is not valid. It should be in E.164 format, e.g., +1234567890.");

        string token = "test";

        if (!isTest)
        {
            if (shell is null) throw new ArgumentNullException("Shell is null in the FirebaseLoginService.RequestVerificationCodeAsync Method");
            var htmlSource = $"https://{authDomain}/recaptcha.html";
            token = await new RecaptchaService(this, shell, htmlSource, timeoutMilliseconds).GetRecaptchaTokenAsync()
                ?? throw new Exception("token is null in the FirebaseLoginService.RequestVerificationCodeAsync Method");
        }

        var request = new
        {
            phoneNumber = normalizedPhoneNumber,
            recaptchaToken = token
        };

        using var requestMessage = new HttpRequestMessage(
            method: HttpMethod.Post,
            requestUri: $"https://identitytoolkit.googleapis.com/v1/accounts:sendVerificationCode?key={apiKey}")
        {
            Content = JsonContent.Create(request)
        };

        return await CallWithTimeout(async () =>
        {
            using var response = await httpClient.Value.SendAsync(requestMessage).ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Phone auth code request failed: {json}");

            var result = JsonSerializer.Deserialize<PhoneCodeResponse>(json);
            sessionResult = new PhoneAuthSessionResult { SessionInfo = result?.SessionInfo ?? throw new Exception("Missing sessionInfo") };
            return !string.IsNullOrWhiteSpace(sessionResult.SessionInfo);
        },
        timeoutMilliseconds);
    }
    #endregion

    #region LoginWithVerificationCodeAsync Method
    /// <summary>
    /// Авторизация с помощью проверочного кода, полученного на номер телефона.
    /// </summary>
    /// <param name="smsCode">Код, полученный пользователем в СМС.</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns><see cref="AuthResult"/></returns>
    /// <exception cref="Exception">Выбрасывается, если запрос завершился неудачей или ответ не содержит необходимой информации о сессии.</exception>
    public async Task<AuthResult> LoginWithVerificationCodeAsync(string smsCode, long timeoutMilliseconds)
    {
        var request = new
        {
            sessionInfo = sessionResult?.SessionInfo,
            code = smsCode
        };

        using var requestMessage = new HttpRequestMessage(
            method: HttpMethod.Post,
            requestUri: $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPhoneNumber?key={apiKey}")
        {
            Content = JsonContent.Create(request)
        };

        return await CallWithTimeout(async () =>
        {
            using var response = await httpClient.Value.SendAsync(requestMessage).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Phone auth login failed: {json}");

            var phoneLoginResponse = JsonSerializer.Deserialize<PhoneLoginResponse>(json);

            if (string.IsNullOrEmpty(phoneLoginResponse?.IdToken))
                return AuthResult.Failed("Phone authentication failed.");

            // Получаем информацию о пользователе используя idToken
            var userData = await GetUserInfoFromIdTokenAsync(phoneLoginResponse.IdToken);
            var tokens = new AuthTokens
            {
                IdToken = phoneLoginResponse.IdToken,
                RefreshToken = await GetRefreshTokenFromIdTokenAsync(phoneLoginResponse.IdToken),
                ExpiresIn = "3600", // Обычно токен действителен 1 час
                TokenExpiry = DateTime.UtcNow.AddHours(1)
            };

            return AuthResult.Successful(userData, tokens);
        },
        timeoutMilliseconds);
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
            if (authClient is not null && authClient.User is not null)
                authClient.SignOut();
            else
            {
                throw new InvalidOperationException("No user is currently signed in.");
            }
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"Logout failed: {ex.Message}", ex);
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
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns><see cref="AuthResult"/></returns>
    /// <exception cref="FirebaseAuthException">Обработка ошибок аутентификации. Например, неверный пароль или адрес электронной почты.</exception>
    /// <exception cref="Exception">Обработка других ошибок.Например, невалидная почта или невалидный пароль.</exception>
    public async Task<AuthResult> RegisterWithEmailAsync(string login, string email, string password, long timeoutMilliseconds)
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

            return await CallWithTimeout(async () =>
                {
                    var userCredential = await authClient.CreateUserWithEmailAndPasswordAsync(email, password, login);
                    
                    if (string.IsNullOrEmpty(userCredential?.User?.Uid))
                        return AuthResult.Failed("Registration failed.");

                    return await GetAuthResult(userCredential, "email");
                },
                timeoutMilliseconds);
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
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции сброса пароля.</param>
    /// <returns>true в случае успеха; иначе - false.</returns>
    /// <exception cref="Exception">Ошибка сброса пароля. Например, неверный email или логин.</exception>
    public async Task SendPasswordResetEmailAsync(string loginOrEmail, long timeoutMilliseconds)
    {
        try
        {
            var email = IsValidEmail(loginOrEmail)
                ? loginOrEmail
                : await userStorageService.GetEmailByLoginAsync(loginOrEmail)
                    ?? throw new Exception("Login not found in the IUserStorageService.");

            _ = await CallWithTimeout(async () =>
                {
                    await authClient.ResetEmailPasswordAsync(loginOrEmail);
                    return true; // Возвращаем true, если сброс пароля прошел успешно
                },
                timeoutMilliseconds);

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
        return ValidPasswordRegex().IsMatch(password);
    }
    #endregion

    #region NormalizePhoneNumber Method
    /// <summary>
    /// Нормализует номер телефона, удаляя все нечисловые символы и добавляя знак "+".
    /// </summary>
    /// <param name="phoneNumber">Номер телефона для нормализации. Не должно быть null.</param>
    /// <returns>Нормализованная строка телефонного номера, начинающаяся с "+", после которого следуют цифры.</returns>
    private static string NormalizePhoneNumber(string phoneNumber) =>
        // Удаляем все символы, кроме цифр
        "+" + NormalizePhoneNumberRegex().Replace(input: phoneNumber, replacement: string.Empty);
    #endregion

    #region IsValidPhoneNumber Method
    /// <summary>
    /// Определяет, действительно ли указанный телефонный номер соответствует формату E.164.
    /// </summary>
    /// <remarks>Формат E.164 - это международный стандарт для телефонных номеров, требующий наличия знака "+" в начале и последующих 10-15 цифр. 
    /// Этот метод проверяет, что вводимые данные не являются нулевыми, пустыми или с пробелами, и соответствуют требуемому формату.</remarks>
    /// <param name="phoneNumber">Номер телефона для проверки.</param>
    /// <returns><see langword="true"/> если номер телефона действителен в соответствии с форматом E.164; иначе, <see langword="false"/>.</returns>
    private static bool IsValidPhoneNumber(string phoneNumber) =>
        // Проверяем, что номер телефона не пустой и соответствует формату E.164:
        // "+", за которым следуют от 10 до 15 цифр
        !string.IsNullOrWhiteSpace(phoneNumber) && PhoneNumberRegex().IsMatch(phoneNumber);
    #endregion

    #region Regex Methods
    [GeneratedRegex(@"^\+\d{10,15}$")]
    private static partial Regex PhoneNumberRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex NormalizePhoneNumberRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9~`!@#$%^&*()\-_+=|}\]{\["":;?\/>.<,\s]{6,}$")]
    private static partial Regex ValidPasswordRegex();
    #endregion

    #region CallWithTimeout Method
    /// <summary>
    /// Выполняет асинхронную операцию с таймаутом.
    /// </summary>
    /// <param name="asyncOperation">Асинхронная операция.</param>
    /// <param name="timeoutMilliseconds">Максимальное время выполнения операции.</param>
    /// <returns><see cref="Task"/> с результатом операции типа <see cref="object"/></returns>
    /// <exception cref="TimeoutException">Если операция не успела закончится в течение установленного времени.</exception>
    private async static Task<T> CallWithTimeout<T>(Func<Task<T>> asyncOperation, long timeoutMilliseconds)
    {
        var task = asyncOperation();
        var cancelTask = Task.Delay(TimeSpan.FromMilliseconds(timeoutMilliseconds));

        var completed = await Task.WhenAny(task, cancelTask); // что закончится быстрее, задача или таймаут

        if (completed == cancelTask)
            throw new TimeoutException("The operation was cancelled."); // отмена операции

        return await task; // Нормальное завершение задачи 
    }
    #endregion

    #region GetAuthTokenAsync Method
    /// <summary>
    /// Возвращает refresh и id токены пользователя Firebase
    /// </summary>
    /// <param name="credential"></param>
    /// <returns><see cref="AuthTokens"/></returns>
    private async Task<AuthTokens> GetAuthTokensAsync(UserCredential credential)
    {
        var idToken = await credential.User.GetIdTokenAsync();
        var refreshToken = credential.User.Credential.RefreshToken;

        return new()
        {
            IdToken = idToken,
            RefreshToken = refreshToken,
            ExpiresIn = "3600", // в Firebase Id токен выдается на 1 час - 3600 секунд
            TokenExpiry = DateTime.UtcNow.AddHours(1) // ID токен истечет через час от текущего момента
        };
    }
    #endregion

    #region CreateUserAuthData Method
    /// <summary>
    /// Возвращает данные пользователя, прошедшего аутентификацию.
    /// </summary>
    /// <param name="user"><see cref="User"/> user, который прошел аутентификацию.</param>
    /// <param name="provider">Провайдер, с помощью которого, пользователь прошел аутентификацию.</param>
    /// <returns></returns>
    private UserAuthData CreateUserAuthData(User user, string provider) =>
        new(
            IsEmailVerified: user.Info.IsEmailVerified,
            DisplayName: user.Info.DisplayName,
            PhotoUrl: user.Info.PhotoUrl,
            PhoneNumber: null,
            UserId: user.Uid,
            Email: user.Info.Email ?? string.Empty,
            Provider: provider);
    #endregion

    #region GetAuthResult Method
    /// <summary>
    /// Возвращает асинхронно результат с данными аутентифицированного пользователя.
    /// </summary>
    /// <param name="userCredential"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    private async Task<AuthResult> GetAuthResult(UserCredential userCredential, string provider)
    {
        var authData = CreateUserAuthData(userCredential.User, provider);
        var tokens = await GetAuthTokensAsync(userCredential);

        return AuthResult.Successful(authData, tokens);
    }
    #endregion

    #region GetUserInfoFromIdTokenAsync Method
    /// <summary>
    /// Возвращает информацию о пользователе по ID токену.
    /// </summary>
    /// <param name="idToken">ID токен</param>
    /// <returns><see cref="UserAuthData"/></returns>
    private async Task<UserAuthData?> GetUserInfoFromIdTokenAsync(string idToken)
    {
        // Используем Firebase REST API для получения информации о пользователе
        var request = new { idToken };

        using var requestMessage = new HttpRequestMessage(
            method: HttpMethod.Post,
            requestUri: $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await httpClient.Value.SendAsync(requestMessage);
        var json = await response.Content.ReadAsStringAsync();

        // Парсим ответ и возвращаем информацию о пользователе
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("users", out var users) && users.GetArrayLength() > 0)
        {
            var user = users[0];

            string? uid = user.TryGetProperty("localId", out var localIdProperty) ? localIdProperty.GetString() : null;
            string? email = user.TryGetProperty("email", out var emailProperty) ? emailProperty.GetString() : null;
            string? phoneNumber = user.TryGetProperty("phoneNumber", out var phoneProperty) ? phoneProperty.GetString() : null;
            bool isEmailVerified = user.TryGetProperty("emailVerified", out var emailVerifiedProperty) && emailVerifiedProperty.GetBoolean();

            return new(
                IsEmailVerified: isEmailVerified,
                DisplayName: null,
                PhotoUrl: null,
                PhoneNumber: phoneNumber,
                UserId: uid ?? string.Empty,
                Email: email ?? string.Empty,
                Provider: "phone");
        }

        return null;
    } 
    #endregion

    #region GetRefreshTokenFromIdTokenAsync Method
    /// <summary>
    /// Возвращаем refresh 
    /// </summary>
    /// <param name="idToken">ID токен Firebase</param>
    /// <returns>refresh токен</returns>
    private async Task<string> GetRefreshTokenFromIdTokenAsync(string idToken)
    {
        // Обмениваем idToken на refreshToken
        var request = new
        {
            grant_type = "authorization_code",
            code = idToken
        };

        using var requestMessage = new HttpRequestMessage(
            method: HttpMethod.Post,
            requestUri: $"https://securetoken.googleapis.com/v1/token?key={apiKey}")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await httpClient.Value.SendAsync(requestMessage);
        var json = await response.Content.ReadAsStringAsync();

        // Парсим JSON и извлекаем refresh_token
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("refresh_token", out var refreshTokenElement))
            return refreshTokenElement.GetString() ?? string.Empty;

        return string.Empty;
    }
    #endregion
}
