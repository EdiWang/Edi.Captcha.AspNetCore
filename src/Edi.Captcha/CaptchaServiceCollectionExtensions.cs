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
        string fontName = GetDefaultFontName();

        var option = new BasicLetterCaptchaOptions
        {
            Letters = "2346789ABCDGHKMNPRUVWXYZ",
            SessionName = "CaptchaCode",
            FontName = fontName,
            CodeLength = 4
        };

        options?.Invoke(option);

        services.AddTransient<ISessionBasedCaptcha>(sb => new BasicLetterCaptcha(option));
    }

    public static void AddStatelessCaptcha(this IServiceCollection services, Action<StatelessLetterCaptchaOptions> options = null)
    {
        services.AddDataProtection();

        string fontName = GetDefaultFontName();

        var option = new StatelessLetterCaptchaOptions
        {
            Letters = "2346789ABCDGHKMNPRUVWXYZ",
            FontName = fontName,
            CodeLength = 4,
            TokenExpiration = TimeSpan.FromMinutes(5)
        };

        options?.Invoke(option);

        services.AddSingleton(option);
        services.AddTransient<IStatelessCaptcha, StatelessLetterCaptcha>();
    }

public static IServiceCollection AddSharedKeyStatelessCaptcha(this IServiceCollection services, Action<SharedKeyStatelessCaptchaOptions> setupAction)
{
    if (setupAction == null)
    {
        throw new ArgumentNullException(nameof(setupAction));
    }

    var options = new SharedKeyStatelessCaptchaOptions();
    setupAction(options);

    services.AddSingleton(options);
    services.AddTransient<IStatelessCaptcha, SharedKeyStatelessLetterCaptcha>();

    return services;
}

    private static string GetDefaultFontName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "Arial";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetAvailableFontForLinux();
        }

        return "Arial";
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
