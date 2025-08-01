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
            // ���������� ���������������� ����� URL -������
            e.Cancel = true; // ������� ���������, ����� ������������� ��������� WebView �� URL
            
            var token = HttpUtility.UrlDecode(e.Url["recaptcha://token?".Length..]); // �������� ����� �� URL
            // �� �� �����, ��� 
            // var token = e.Url.Substring("recaptcha://token?".Length);
            // , �� ������������ �������� - ����������� ���� "�����-�� ������ ��� ������ ��������"[3..6] ��� array[2..] ��� list[..7] � �.�.

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert("Error", "Token is empty or invalid.", "OK");
                RecaptchaVerifiedEvent?.Invoke(this, null); // �������� ������� � null, ���� �������� �� ������
                return;
            }

            bool isVerified = await _loginService.VerifyRecaptchaTokenAsync(token, _timeout); 

            if (!isVerified)
            {
                await DisplayAlert("Error", "Recaptcha verification failed.", "OK");
                RecaptchaVerifiedEvent?.Invoke(this, null); // �������� ������� � null, ���� �������� �� ������
                return;
            }

            RecaptchaVerifiedEvent?.Invoke(this, token); // �������� ������� � �������, ���� �������� ������ �������
        }
        else 
            RecaptchaVerifiedEvent?.Invoke(this, null); // �������� ������� � null, ���� �������� �� ������
    }
}