namespace Edi.Captcha;

public class BasicLetterCaptchaOptions : SessionBasedCaptchaOptions
{
    public string Letters { get; set; }

    public int CodeLength { get; set; }
}

public class BasicLetterCaptcha : SessionBasedCaptcha
{
    private readonly BasicLetterCaptchaOptions _options;

    public BasicLetterCaptcha(BasicLetterCaptchaOptions options)
    {
        Options = options;
        _options = options;
    }

    public override string GenerateCaptchaCode() =>
        SecureCaptchaGenerator.GenerateSecureCaptchaCode(_options.Letters, _options.CodeLength);
}