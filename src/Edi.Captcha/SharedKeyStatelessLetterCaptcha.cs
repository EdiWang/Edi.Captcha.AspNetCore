namespace Edi.Captcha;

public class SharedKeyStatelessLetterCaptchaOptions : SharedKeyStatelessCaptchaOptions
{
    public string Letters { get; set; } = "2346789ABCDGHKMNPRUVWXYZ";
    public int CodeLength { get; set; } = 4;
}

public class SharedKeyStatelessLetterCaptcha(SharedKeyStatelessLetterCaptchaOptions options) : SharedKeyStatelessCaptcha(options)
{
    public override string GenerateCaptchaCode() =>
        SecureCaptchaGenerator.GenerateSecureCaptchaCode(options.Letters, options.CodeLength);
}