using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.Fonts;

namespace Edi.Captcha;

public static class CaptchaServiceCollectionExtensions
{
    public static void AddSessionBasedCaptcha(this IServiceCollection services, Action<BasicLetterCaptchaOptions> options = null)
    {
        string fontName = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            fontName = "Arial";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            fontName = GetAvailableFontForLinux();
        }

        var option = new BasicLetterCaptchaOptions
        {
            Letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ",
            SessionName = "CaptchaCode",
            FontName = fontName,
            CodeLength = 4
        };

        options?.Invoke(option);

        services.AddTransient<ISessionBasedCaptcha>(sb => new BasicLetterCaptcha(option));
    }

    private static string GetAvailableFontForLinux()
    {
        var fontList = new[]
        {
            "Arial", 
            "Verdana", 
            "Helvetica", 
            "Tahoma", 
            "Terminal", 
            "Open Sans", 
            "Monospace", 
            "Ubuntu Mono",
            "DejaVu Sans", 
            "DejaVu Sans Mono"
        };
        return fontList.FirstOrDefault(fontName => SystemFonts.Collection.TryGet(fontName, out _));
    }
}