using Firebase.Auth;

namespace AuthenticationMaui.Services;

public interface ILoginService
{
    #region LoginWithEmailAsync Method
    /// <summary>
    /// Вход с помощью логина (или электронной) почты и пароля.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <param name="password">Пароль аккаунта (не от почты).</param>
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    Task<bool> LoginWithEmailAsync(string loginOrEmail, string password);
    #endregion

    #region LoginWithGoogleAsync Method
    /// <summary>
    /// Вход с помощью Google аккаунта.
    /// </summary>
    /// <returns>В случае успеха возвращается true, иначе - false.</returns>
    Task<bool> LoginWithGoogleAsync(); 
    #endregion

    #region RegisterWithEmailAsync Method
    /// <summary>
    /// Регистрация с помощью электронной почты и пароля.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="email">Адрес электронной почты.</param>
    /// <param name="password">Пароль аккаунта (не от почты).</param>
    /// <returns>true случае успеха; иначе - false</returns>
    Task<bool> RegisterWithEmailAsync(string login, string email, string password);
    #endregion

    #region SendPasswordResetEmailAsync Method
    /// <summary>
    /// Восстановление пароля по логину или электронной почте.
    /// </summary>
    /// <param name="loginOrEmail">Логин или адрес электронной почты.</param>
    /// <returns>true в случае успеха; иначе - false.</returns>
    Task SendPasswordResetEmailAsync(string loginOrEmail);
    #endregion

    #region Logout Method
    /// <summary>
    /// Выход из аккаунта.
    /// </summary>
    void Logout(); 
    #endregion
}
