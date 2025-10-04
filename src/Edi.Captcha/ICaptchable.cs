namespace Edi.Captcha;

public interface ICaptchable
{
    string CaptchaCode { get; set; }
}

public interface ICaptchableWithToken : ICaptchable
{
    string CaptchaToken { get; set; }
}