﻿using Firebase.Auth;
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
    private FirebaseAuthClient authClient;
    private IUserStorageService userStorageService;
    private string googleClientId;
    private string googleRedirectUri;
    private string callbackScheme;
    private string apiKey;
    private static readonly Lazy<HttpClient> httpClient = new();
    private static PhoneAuthSessionResult? sessionResult;
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
            ],
        };

        authClient = new FirebaseAuthClient(authConfig);
        apiKey = data.ApiKey ?? throw new ArgumentNullException(nameof(data.ApiKey), "API Key cannot be null.");
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
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>В случае успеха возвращается true, инача - false.</returns>
    /// <exception cref="FirebaseAuthException">Обработка ошибок аутентификации. Например, неверный пароль или адрес электронной почты.</exception>
    /// <exception cref="Exception">Обработка других ошибок. Например, в хранилище типа IUserStorageService не найден логин.</exception>
    public async Task<bool> LoginWithEmailAsync(string loginOrEmail, string password, long timeoutMilliseconds)
    {
        try
        {
            var email = IsValidEmail(loginOrEmail)
                ? loginOrEmail
                : await userStorageService.GetEmailByLoginAsync(loginOrEmail)
                    ?? throw new Exception("Login not found in the IUserStorageService.");

            if (!IsValidPassword(password))
                throw new ArgumentException("Password is not valid.");
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));
            return await CallWithTimeout(async () =>
                {
                    var userCredential = await authClient.SignInWithEmailAndPasswordAsync(email, password);
                    return !string.IsNullOrEmpty(userCredential?.User?.Info?.Email);
                },
                cts.Token);
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
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    /// <exception cref="Exception">Исключение в случае неудачи авторизации через Google.</exception>
    public async Task<bool> LoginWithGoogleAsync(long timeoutMilliseconds)
    {
        // WinUI не поддерживает WebAuthenticator.
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
            return false;

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));

            return await CallWithTimeout(async () =>
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
            },
            cts.Token);
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
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    public async Task<bool> LoginWithFacebookAsync(long timeoutMilliseconds)
    {
        // TODO: Not Implemented
        return false;
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
    /// <returns>true, если запрос отправлен успешно, иначе - false</returns>
    /// <exception cref="Exception">Выбрасывается, если запрос завершился неудачей или ответ не содержит необходимой информации о сессии.</exception>
    public async Task<bool> RequestVerificationCodeAsync(string phoneNumber, long timeoutMilliseconds)
    {
        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
        if (!IsValidPhoneNumber(normalizedPhoneNumber))
            throw new ArgumentException("Phone number is not valid. It should be in E.164 format, e.g., +1234567890.");

        var request = new
        {
            phoneNumber = normalizedPhoneNumber,
            recaptchaToken = "test"
        };

        using var requestMessage = new HttpRequestMessage(
            method: HttpMethod.Post,
            requestUri: $"https://identitytoolkit.googleapis.com/v1/accounts:sendVerificationCode?key={apiKey}")
        {
            Content = JsonContent.Create(request)
        };

        var sendTask = httpClient.Value.SendAsync(requestMessage);
        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(timeoutMilliseconds));

        var completedTask = await Task.WhenAny(sendTask, timeoutTask);

        if (completedTask == timeoutTask)
            throw new TimeoutException("The operation timed out while waiting for Firebase response.");

        using var response = await sendTask.ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        //Log
        Trace.WriteLine(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Phone auth code request failed: {json}");

        var result = JsonSerializer.Deserialize<PhoneCodeResponse>(json);
        sessionResult = new PhoneAuthSessionResult { SessionInfo = result?.SessionInfo ?? throw new Exception("Missing sessionInfo") };
        return !string.IsNullOrWhiteSpace(sessionResult.SessionInfo );
    }
    #endregion

    #region LoginWithVerificationCodeAsync Method
    /// <summary>
    /// Авторизация с помощью проверочного кода, полученного на номер телефона.
    /// </summary>
    /// <param name="code">Код, полученный пользователем в СМС.</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>true, если аутентификация прошла успешно, false - в противном случае.</returns>
    /// <exception cref="Exception">Выбрасывается, если запрос завершился неудачей или ответ не содержит необходимой информации о сессии.</exception>
    public async Task<bool> LoginWithVerificationCodeAsync(string code, long timeoutMilliseconds)
    {
        var request = new
        {
            sessionInfo = sessionResult?.SessionInfo,
            code = code
        };

        using var requestMessage = new HttpRequestMessage(
            method: HttpMethod.Post,
            requestUri: $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPhoneNumber?key={apiKey}")
        {
            Content = JsonContent.Create(request)
        };

        var sendTask = httpClient.Value.SendAsync(requestMessage);
        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(timeoutMilliseconds));

        var completedTask = await Task.WhenAny(sendTask, timeoutTask);

        if (completedTask == timeoutTask)
            throw new TimeoutException("The operation timed out while waiting for Firebase response.");

        using var response = await sendTask.ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        //Log
        Trace.WriteLine(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Phone auth login failed: {json}");

        var result = JsonSerializer.Deserialize<PhoneLoginResponse>(json);
        return !string.IsNullOrEmpty(result?.IdToken);
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
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>true случае успеха; иначе - false</returns>
    /// <exception cref="FirebaseAuthException">Обработка ошибок аутентификации. Например, неверный пароль или адрес электронной почты.</exception>
    /// <exception cref="Exception">Обработка других ошибок.Например, невалидная почта или невалидный пароль.</exception>
    public async Task<bool> RegisterWithEmailAsync(string login, string email, string password, long timeoutMilliseconds)
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

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));

            return await CallWithTimeout(async() =>
                {
                    var registrationResult = await authClient.CreateUserWithEmailAndPasswordAsync(email, password, login);
                    return !string.IsNullOrEmpty(registrationResult?.User?.Info?.Email);
                }, 
                cts.Token);
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

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds));

            _ = await CallWithTimeout(async () =>
                {
                    await authClient.ResetEmailPasswordAsync(loginOrEmail);
                    return true; // Возвращаем true, если сброс пароля прошел успешно
                },
                cts.Token);

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
    /// <param name="asyncOperation">Асинхронная операция</param>
    /// <param name="token">Токен сброса операции</param>
    /// <returns><see cref="Task"/> с результатом операции типа <see cref="bool"/></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task<bool> CallWithTimeout(Func<Task<bool>> asyncOperation, CancellationToken token)
    {
        var task = asyncOperation();
        var cancelTask = Task.Delay(Timeout.Infinite, token);

        var completed = await Task.WhenAny(task, cancelTask);

        if (completed == cancelTask)
            throw new OperationCanceledException(token); // отмена операции

        return await task; // Нормальное завершение задачи 
    }
    #endregion
}
