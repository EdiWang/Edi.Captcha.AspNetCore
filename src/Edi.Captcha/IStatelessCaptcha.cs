namespace Edi.Captcha;

public interface IStatelessCaptcha
{
    StatelessCaptchaResult GenerateCaptcha(int width = 100, int height = 36);
    bool Validate(string userInputCaptcha, string captchaToken, bool ignoreCase = true);
}

public class StatelessCaptchaResult
{
    public byte[] ImageBytes { get; set; }
    public string Token { get; set; }
}