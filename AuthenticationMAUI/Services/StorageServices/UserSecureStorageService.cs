namespace AuthenticationMaui.Services;

public class UserSecureStorageService : IUserStorageService
{
    #region Private Variables
    private const string KeyPrefix = "user_"; // Префикс для пользовательских ключей 
    #endregion

    #region AddUserAsync Method
    /// <summary>
    /// Сохраняет логин и привязанный к нему email пользователя в SecureStorage.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <param name="email">Электронная почта пользователя.</param>
    /// <exception cref="ArgumentException">Логин или электронная почта равна null или пустой строке.</exception>
    public async Task AddUserAsync(string login, string email)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Login and email cannot be null or empty.");

        // Сохраняем логин и email в SecureStorage с префиксом
        await SecureStorage.SetAsync($"{KeyPrefix}{login.ToLower()}", email);
    }
    #endregion

    #region ClearAllUsers Method
    /// <summary>
    /// Удаляет все записи пользователей (осторожно!)
    /// </summary>
    public void ClearAllUsers()
    {
        // WARNING: Удаляет все данные в SecureStorage, используйте с осторожностью!
        SecureStorage.RemoveAll();
    }
    #endregion

    #region GetEmailByLoginAsync Method
    /// <summary>
    /// Получает email пользователя по его логину из SecureStorage.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <returns>Электронная почта пользователя.</returns>
    public async Task<string?> GetEmailByLoginAsync(string login)
    {
        if (string.IsNullOrWhiteSpace(login)) return null;
        return await SecureStorage.GetAsync($"{KeyPrefix}{login.ToLower()}");
    }
    #endregion

    #region LoginExistsAsync Method
    /// <summary>
    /// Проверяет, существует ли пользователь с данным логином в SecureStorage.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    /// <returns>true, если пользователь с данным логином сохранен в SecureStorage; иначе - false.</returns>
    public async Task<bool> LoginExistsAsync(string login)
    {
        return await GetEmailByLoginAsync(login) is not null;
    }
    #endregion

    #region RemoveUserAsync Method
    /// <summary>
    /// Удаляет пользователя из SecureStorage по логину.
    /// </summary>
    /// <param name="login">Логин пользователя.</param>
    public void RemoveUserAsync(string login)
    {
        SecureStorage.Remove($"{KeyPrefix}{login.ToLower()}");
    } 
    #endregion
}
