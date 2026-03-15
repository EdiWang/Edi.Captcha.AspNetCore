using Microsoft.Extensions.DependencyInjection;
using System;

namespace Edi.Captcha;

public static class CaptchaServiceCollectionExtensions
{
    public static void AddSessionBasedCaptcha(this IServiceCollection services, Action<BasicLetterCaptchaOptions> options = null)
    {
        var option = new BasicLetterCaptchaOptions
        {
            Letters = "2346789ABCDGHKMNPRUVWXYZ",
            SessionName = "CaptchaCode",
            CodeLength = 4
        };

        options?.Invoke(option);

        services.AddTransient<ISessionBasedCaptcha>(sb => new BasicLetterCaptcha(option));
    }

    public static void AddStatelessCaptcha(this IServiceCollection services, Action<StatelessLetterCaptchaOptions> options = null)
    {
        services.AddDataProtection();

        var option = new StatelessLetterCaptchaOptions
        {
            Letters = "2346789ABCDGHKMNPRUVWXYZ",
            CodeLength = 4,
            TokenExpiration = TimeSpan.FromMinutes(5)
        };

        options?.Invoke(option);

        services.AddSingleton(option);
        services.AddTransient<IStatelessCaptcha, StatelessLetterCaptcha>();
    }

    public static IServiceCollection AddSharedKeyStatelessCaptcha(this IServiceCollection services, Action<SharedKeyStatelessLetterCaptchaOptions> options = null)
    {
        var option = new SharedKeyStatelessLetterCaptchaOptions
        {
            Letters = "2346789ABCDGHKMNPRUVWXYZ",
            CodeLength = 4,
            TokenExpiration = TimeSpan.FromMinutes(5)
        };

        options?.Invoke(option);

        services.AddSingleton(option);
        services.AddTransient<IStatelessCaptcha, SharedKeyStatelessLetterCaptcha>();

        return services;
    }
}
