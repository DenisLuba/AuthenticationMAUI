using AuthenticationMAUI.Pages;

namespace AuthenticationMaui.Services;

public class RecaptchaService(

        ILoginService loginService,
        Shell shell,
        string htmlSource,
        long timeout = 5000)
{
    #region GetRecaptchaTokenAsync Method
    /// <summary>
    /// Показывает страницу с reCAPTCHA и возвращает токен после прохождения теста.
    /// </summary>
    /// <returns>reCAPTCHA token или null, если пользователь прервал процесс или token не был получен.</returns>
    public async Task<string?> GetRecaptchaTokenAsync()
    {
        var tcs = new TaskCompletionSource<string?>();

        var page = new RecaptchaWebView(loginService, shell, htmlSource, timeout);

        page.RecaptchaVerifiedEvent += async (s, token) =>
        {
            await shell.Navigation.PopModalAsync();
            tcs.TrySetResult(token);
        };

        await shell.Navigation.PushModalAsync(page);
        return await tcs.Task;
    } 
    #endregion
}
