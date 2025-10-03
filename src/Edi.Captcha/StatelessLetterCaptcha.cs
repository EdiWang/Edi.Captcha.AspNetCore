using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text;

namespace Edi.Captcha;

public class StatelessLetterCaptchaOptions : StatelessCaptchaOptions
{
    public string Letters { get; set; } = "2346789ABCDGHKMNPRUVWXYZ";
    public int CodeLength { get; set; } = 4;
}

public class StatelessLetterCaptcha : StatelessCaptcha
{
    private readonly StatelessLetterCaptchaOptions _options;

    public StatelessLetterCaptcha(IDataProtectionProvider dataProtectionProvider, StatelessLetterCaptchaOptions options)
        : base(dataProtectionProvider, options)
    {
        _options = options;
    }

    public override string GenerateCaptchaCode()
    {
        if (_options.CodeLength < 1 || _options.CodeLength > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(_options.CodeLength),
                $"codeLength must range within 1-32, current value is {_options.CodeLength}");
        }

        var rand = new Random();
        var maxRand = _options.Letters.Length - 1;

        var sb = new StringBuilder();
        for (var i = 0; i < _options.CodeLength; i++)
        {
            var index = rand.Next(maxRand);
            sb.Append(_options.Letters[index]);
        }

        return sb.ToString().ToUpper();
    }
}