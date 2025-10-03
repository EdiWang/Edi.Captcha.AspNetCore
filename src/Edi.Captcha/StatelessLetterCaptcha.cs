using Microsoft.AspNetCore.DataProtection;

namespace Edi.Captcha;

public class StatelessLetterCaptchaOptions : StatelessCaptchaOptions
{
    public string Letters { get; set; } = "2346789ABCDGHKMNPRUVWXYZ";
    public int CodeLength { get; set; } = 4;
}

public class StatelessLetterCaptcha(
    IDataProtectionProvider dataProtectionProvider,
    StatelessLetterCaptchaOptions options) : StatelessCaptcha(dataProtectionProvider, options)
{
    public override string GenerateCaptchaCode() => 
        SecureCaptchaGenerator.GenerateSecureCaptchaCode(options.Letters, options.CodeLength);
}