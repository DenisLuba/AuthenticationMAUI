namespace AuthenticationMaui.Services;

public interface IUserStorageService
{
    #region AddUserAsync Method
    /// <summary>
    /// Сохраняет логин и привязанный к нему email пользователя в хранилище.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    Task AddUserAsync(string login, string email);
    #endregion

    #region GetEmailByLoginAsync Method
    /// <summary>
    /// Получает email пользователя по его логину из хранилища.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <returns>Электронная почта пользователя.</returns>
    Task<string?> GetEmailByLoginAsync(string login);
    #endregion

    #region LoginExistsAsync Method
    /// <summary>
    /// Проверяет, существует ли пользователь с данным логином в хранилище.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <returns>true, если пользователь с данным логином сохранен в хранилище; иначе - false.</returns>
    Task<bool> LoginExistsAsync(string login); // Проверка существования пользователя в хранилище
    #endregion

    #region RemoveUserAsync Method
    /// <summary>
    /// Удаляет пользователя из хранилища по логину.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    void RemoveUserAsync(string login);
    #endregion

    #region ClearAllUsers Method
    /// <summary>
    /// Удаляет все записи пользователей 
    /// </summary>
    void ClearAllUsers();
    #endregion
}
