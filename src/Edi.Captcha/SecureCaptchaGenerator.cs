using System;
using System.Security.Cryptography;
using System.Text;

namespace Edi.Captcha;

public static class SecureCaptchaGenerator
{
    public static string GenerateSecureCaptchaCode(string letters, int codeLength)
    {
        if (codeLength < 1 || codeLength > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(codeLength),
                $"codeLength must range within 1-32, current value is {codeLength}");
        }

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[codeLength];
        rng.GetBytes(bytes);

        var sb = new StringBuilder(codeLength);
        for (var i = 0; i < codeLength; i++)
        {
            var index = bytes[i] % letters.Length;
            sb.Append(letters[index]);
        }

        return sb.ToString().ToUpper();
    }
}