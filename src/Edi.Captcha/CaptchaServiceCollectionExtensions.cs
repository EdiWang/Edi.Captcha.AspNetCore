using Microsoft.Extensions.DependencyInjection;
using System;

namespace Edi.Captcha
{
    public static class CaptchaServiceCollectionExtensions
    {
        public static void AddSessionBasedCaptcha(this IServiceCollection services, Action<BasicLetterCaptchaOptions> options = null)
        {
            var option = new BasicLetterCaptchaOptions
            {
                Letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ",
                SessionName = "CaptchaCode",
                CodeLength = 4
            };

            options?.Invoke(option);

            services.AddTransient<ISessionBasedCaptcha>(sb => new BasicLetterCaptcha(option));
        }
    }
}
