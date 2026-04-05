using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Edi.Captcha;

public class SessionCaptchaImageMiddleware(RequestDelegate next, SessionCaptchaImageMiddlewareOptions options)
{
    public async Task Invoke(HttpContext context, ISessionBasedCaptcha captcha)
    {
        if (context.Request.Path == options.RequestPath)
        {
            var w = options.ImageWidth;
            var h = options.ImageHeight;

            // prevent crazy size
            if (w > 640) w = 640;
            if (h > 480) h = 480;

            var bytes = captcha.GenerateCaptchaImageBytes(context.Session, w, h);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "image/png";

            if (options.DisableCache)
            {
                context.Response.Headers.CacheControl = "no-cache,no-store";
            }

            await context.Response.Body.WriteAsync(bytes.AsMemory(0, bytes.Length), context.RequestAborted);
        }
        else
        {
            await next(context);
        }
    }
}

public static class CaptchaImageMiddlewareOptionsExtensions
{
    public static IApplicationBuilder UseSessionCaptcha(this IApplicationBuilder app, Action<SessionCaptchaImageMiddlewareOptions> configure)
    {
        var options = new SessionCaptchaImageMiddlewareOptions();
        configure(options);

        if (!options.RequestPath.HasValue ||
            string.IsNullOrWhiteSpace(options.RequestPath.Value))
        {
            throw new ArgumentException("RequestPath must be set in SessionCaptchaImageMiddlewareOptions.", nameof(configure));
        }

        return app.UseMiddleware<SessionCaptchaImageMiddleware>(options);
    }
}

public class SessionCaptchaImageMiddlewareOptions
{
    public PathString RequestPath { get; set; }

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public bool DisableCache { get; set; } = true;
}