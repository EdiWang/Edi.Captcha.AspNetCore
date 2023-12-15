using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using SixLabors.Fonts;

namespace Edi.Captcha.SampleApp;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
        });

        services.AddMvc();

        //services.AddSessionBasedCaptcha();
        services.AddSessionBasedCaptcha(option =>
        {
            option.Letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ";
            option.SessionName = "CaptchaCode";
            option.FontStyle = FontStyle.Bold;
            //option.FontName = "Arial";
            option.CodeLength = 4;
            //option.DrawLines = false;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();
        app.UseSession().UseCaptchaImage(options =>
        {
            options.RequestPath = "/captcha-image";
            options.ImageHeight = 36;
            options.ImageWidth = 100;
        });

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();
        });
    }
}