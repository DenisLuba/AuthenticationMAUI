namespace AuthenticationMaui.Services;

public interface ILoginService
{
    #region LoginWithEmailAsync Method
    /// <summary>
    /// Вход с помощью логина (или электронной) почты и пароля.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <param name="password">Пароль аккаунта (не от почты).</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    Task<bool> LoginWithEmailAsync(string loginOrEmail, string password, long timeoutMilliseconds);
    #endregion

    #region LoginWithGoogleAsync Method
    /// <summary>
    /// Вход с помощью аккаунта Google.
    /// </summary>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    Task<bool> LoginWithGoogleAsync(long timeoutMilliseconds);
    #endregion

    #region LoginWithFacebookAsync Method
    /// <summary>
    /// Вход с помощью аккаунта Facebook.
    /// </summary>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    Task<bool> LoginWithFacebookAsync(long timeoutMilliseconds);
    #endregion

    #region VerifyRecaptchaTokenAsync Method
    /// <summary>
    /// Проверяет токен reCAPTCHA, отправленный с клиентского приложения, с помощью Google reCAPTCHA API.
    /// </summary>
    /// <param name="token">Это одноразовый код, который подтверждает, что пользователь прошёл проверку reCAPTCHA.</param>
    /// <param name="timeoutMilliseconds"></param>
    /// <returns>Максимальное время ожидания в миллисекундах для выполнения операции отправки кода.</returns>
    /// <exception cref="InvalidOperationException">Если не был передан siteKey или secretKey.</exception>
    Task<bool> VerifyRecaptchaTokenAsync(string token, long timeoutMilliseconds);
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
    Task<bool> RequestVerificationCodeAsync(string phoneNumber, long timeoutMilliseconds, bool isTest, Shell? shell);
    #endregion

    #region LoginWithVerificationCodeAsync Method
    /// <summary>
    /// Авторизация с помощью проверочного кода, полученного на номер телефона.
    /// </summary>
    /// <param name="code">Код, полученный пользователем в СМС.</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции входа.</param>
    /// <returns>true, если аутентификация прошла успешно, false - в противном случае.</returns>
    /// <exception cref="Exception">Выбрасывается, если запрос завершился неудачей или ответ не содержит необходимой информации о сессии.</exception>
    Task<bool> LoginWithVerificationCodeAsync(string code, long timeoutMilliseconds);
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
    Task<bool> RegisterWithEmailAsync(string login, string email, string password, long timeoutMilliseconds);
    #endregion

    #region SendPasswordResetEmailAsync Method
    /// <summary>
    /// Восстановление пароля по логину или электронной почте.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <param name="timeoutMilliseconds">Максимальное время ожидания в миллисекундах для выполнения операции сброса пароля.</param>
    /// <returns>true в случае успеха; иначе - false.</returns>
    Task SendPasswordResetEmailAsync(string loginOrEmail, long timeoutMilliseconds);
    #endregion

    #region Logout Method
    /// <summary>
    /// Выход из аккаунта.
    /// </summary>
    void Logout(); 
    #endregion
}
