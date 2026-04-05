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

        // Rejection sampling: discard bytes >= threshold to eliminate modulo bias.
        // threshold is the largest multiple of letters.Length that fits in [0, 256).
        var threshold = 256 - (256 % letters.Length);
        var sb = new StringBuilder(codeLength);
        var buf = new byte[1];

        while (sb.Length < codeLength)
        {
            RandomNumberGenerator.Fill(buf);
            if (buf[0] < threshold)
            {
                sb.Append(letters[buf[0] % letters.Length]);
            }
        }

        return sb.ToString().ToUpper();
    }
}