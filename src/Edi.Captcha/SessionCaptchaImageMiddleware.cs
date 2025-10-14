using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Edi.Captcha;

public class SessionCaptchaImageMiddleware(RequestDelegate next)
{
    public static SessionCaptchaImageMiddlewareOptions Options { get; set; } = new();

    public async Task Invoke(HttpContext context, ISessionBasedCaptcha captcha)
    {
        if (context.Request.Path == Options.RequestPath)
        {
            var w = Options.ImageWidth;
            var h = Options.ImageHeight;

            // prevent crazy size
            if (w > 640) w = 640;
            if (h > 480) h = 480;

            var bytes = captcha.GenerateCaptchaImageBytes(context.Session, w, h);

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "image/png";

            if (Options.DisableCache)
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
    public static IApplicationBuilder UseSessionCaptcha(this IApplicationBuilder app, Action<SessionCaptchaImageMiddlewareOptions> options)
    {
        options(SessionCaptchaImageMiddleware.Options);
        return app.UseMiddleware<SessionCaptchaImageMiddleware>();
    }
}

public class SessionCaptchaImageMiddlewareOptions
{
    public PathString RequestPath { get; set; }

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public bool DisableCache { get; set; } = true;
}