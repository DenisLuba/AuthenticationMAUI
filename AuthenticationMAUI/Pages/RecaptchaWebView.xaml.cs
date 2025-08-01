using AuthenticationMaui.Services;
using System.Web;

namespace AuthenticationMAUI.Pages;

public partial class RecaptchaWebView : ContentPage
{
    private readonly ILoginService _loginService;
    long _timeout;

    public EventHandler<string?>? RecaptchaVerifiedEvent { get; set; }

    public RecaptchaWebView(
        ILoginService loginService, 
        Shell shell,
        string htmlSource, 
        long timeout)
	{
		InitializeComponent();


        _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
        _timeout = timeout;

        Recaptcha.Source = htmlSource;
    }

    private async void RecaptchaWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("recaptcha://token?"))
        {
            // Обработаем пользовательскую схему URL -адреса
            e.Cancel = true; // Отменим навигацию, чтобы предотвратить навигацию WebView на URL
            
            var token = HttpUtility.UrlDecode(e.Url["recaptcha://token?".Length..]); // Извлечем токен из URL
            // то же самое, что 
            // var token = e.Url.Substring("recaptcha://token?".Length);
            // , но используется диапазон - конструкция типа "какая-то строка как массив символов"[3..6] или array[2..] или list[..7] и т.д.

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert("Error", "Token is empty or invalid.", "OK");
                RecaptchaVerifiedEvent?.Invoke(this, null); // Вызываем событие с null, если проверка не прошла
                return;
            }

            bool isVerified = await _loginService.VerifyRecaptchaTokenAsync(token, _timeout); 

            if (!isVerified)
            {
                await DisplayAlert("Error", "Recaptcha verification failed.", "OK");
                RecaptchaVerifiedEvent?.Invoke(this, null); // Вызываем событие с null, если проверка не прошла
                return;
            }

            RecaptchaVerifiedEvent?.Invoke(this, token); // Вызываем событие с токеном, если проверка прошла успешно
        }
        else 
            RecaptchaVerifiedEvent?.Invoke(this, null); // Вызываем событие с null, если проверка не прошла
    }
}