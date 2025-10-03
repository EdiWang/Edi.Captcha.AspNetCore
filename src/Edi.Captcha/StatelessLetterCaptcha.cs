using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text;

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
    private readonly StatelessLetterCaptchaOptions _options = options;

    public override string GenerateCaptchaCode() => 
        SecureCaptchaGenerator.GenerateSecureCaptchaCode(_options.Letters, _options.CodeLength);
}