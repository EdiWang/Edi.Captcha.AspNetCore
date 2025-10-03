using SixLabors.Fonts;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace Edi.Captcha;

public class SharedKeyStatelessCaptchaOptions
{
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;
    public string FontName { get; set; }
    public bool DrawLines { get; set; } = true;
    public string[] BlockedCodes { get; set; } = [];
    public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public string SharedKey { get; set; } // Base64 encoded 256-bit key
}

public abstract class SharedKeyStatelessCaptcha : IStatelessCaptcha
{
    private readonly byte[] _sharedKey;
    private readonly SharedKeyStatelessCaptchaOptions _options;

    protected SharedKeyStatelessCaptcha(SharedKeyStatelessCaptchaOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.SharedKey))
        {
            throw new ArgumentException("SharedKey is required for cluster deployments", nameof(options));
        }

        try
        {
            _sharedKey = Convert.FromBase64String(options.SharedKey);
            if (_sharedKey.Length != 32) // 256 bits
            {
                throw new ArgumentException("SharedKey must be a 256-bit (32-byte) key encoded as Base64");
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException("SharedKey must be a valid Base64 encoded string");
        }
    }

    public abstract string GenerateCaptchaCode();

    public StatelessCaptchaResult GenerateCaptcha(int width = 100, int height = 36)
    {
        var captchaCode = GenerateCaptchaCode();
        while (_options.BlockedCodes.Contains(captchaCode))
        {
            captchaCode = GenerateCaptchaCode();
        }

        var result = CaptchaImageGenerator.GetImage(width, height, captchaCode, _options.FontName, _options.FontStyle, _options.DrawLines);

        var tokenData = new CaptchaTokenData
        {
            Code = captchaCode,
            ExpirationTime = DateTimeOffset.UtcNow.Add(_options.TokenExpiration)
        };

        var serializedData = JsonSerializer.Serialize(tokenData);
        var encryptedToken = EncryptData(serializedData);

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
            var decryptedData = DecryptData(captchaToken);
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

    private string EncryptData(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _sharedKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();

        // Write IV first
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    private string DecryptData(string cipherText)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _sharedKey;

        // Extract IV from the beginning
        var iv = new byte[16];
        Array.Copy(cipherBytes, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        return reader.ReadToEnd();
    }
}