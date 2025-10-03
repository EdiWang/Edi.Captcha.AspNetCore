using Microsoft.AspNetCore.DataProtection;
using SixLabors.Fonts;
using System;
using System.Linq;
using System.Text.Json;

namespace Edi.Captcha;

public class StatelessCaptchaOptions
{
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;
    public string FontName { get; set; }
    public bool DrawLines { get; set; } = true;
    public string[] BlockedCodes { get; set; } = [];
    public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromMinutes(5);
}

public abstract class StatelessCaptcha(IDataProtectionProvider dataProtectionProvider, StatelessCaptchaOptions options) : IStatelessCaptcha
{
    private readonly IDataProtector _dataProtector = dataProtectionProvider.CreateProtector("Edi.Captcha.Stateless");

    public abstract string GenerateCaptchaCode();

    public StatelessCaptchaResult GenerateCaptcha(int width = 100, int height = 36)
    {
        var captchaCode = GenerateCaptchaCode();
        while (options.BlockedCodes.Contains(captchaCode))
        {
            captchaCode = GenerateCaptchaCode();
        }

        var result = CaptchaImageGenerator.GetImage(width, height, captchaCode, options.FontName, options.FontStyle, options.DrawLines);

        var tokenData = new CaptchaTokenData
        {
            Code = captchaCode,
            ExpirationTime = DateTimeOffset.UtcNow.Add(options.TokenExpiration)
        };

        var serializedData = JsonSerializer.Serialize(tokenData);
        var encryptedToken = _dataProtector.Protect(serializedData);

        return new StatelessCaptchaResult
        {
            ImageBytes = result.CaptchaByteData,
            Token = encryptedToken
        };
    }

    public bool Validate(string userInputCaptcha, string captchaToken, bool ignoreCase = true)
    {
        if (string.IsNullOrWhiteSpace(userInputCaptcha) || string.IsNullOrWhiteSpace(captchaToken))
        {
            return false;
        }

        try
        {
            var decryptedData = _dataProtector.Unprotect(captchaToken);
            var tokenData = JsonSerializer.Deserialize<CaptchaTokenData>(decryptedData);

            if (DateTimeOffset.UtcNow > tokenData.ExpirationTime)
            {
                return false; // Token expired
            }

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(userInputCaptcha, tokenData.Code, comparison);
        }
        catch
        {
            return false; // Invalid or corrupted token
        }
    }
}

internal class CaptchaTokenData
{
    public string Code { get; set; }
    public DateTimeOffset ExpirationTime { get; set; }
}