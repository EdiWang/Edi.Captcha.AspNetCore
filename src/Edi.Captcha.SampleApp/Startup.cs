using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SixLabors.Fonts;
using System;
using System.Security.Cryptography;

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

        var magic1 = Convert.ToBase64String(SHA256.HashData(BitConverter.GetBytes(0x7DB14)))[21..25];
        var magic2 = Convert.ToBase64String(SHA256.HashData(BitConverter.GetBytes(0x78E10)))[13..17];

        //services.AddSessionBasedCaptcha();
        services.AddSessionBasedCaptcha(option =>
        {
            option.Letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ";
            option.SessionName = "CaptchaCode";
            option.FontStyle = FontStyle.Bold;
            //option.FontName = "Arial";
            option.CodeLength = 4;
            //option.DrawLines = false;
            option.BlockedCodes = [magic1, magic2];
        });

        // For stateless approach (recommended)
        services.AddStatelessCaptcha(options =>
        {
            options.Letters = "2346789ABCDGHKMNPRUVWXYZ";
            options.CodeLength = 4;
            options.TokenExpiration = TimeSpan.FromMinutes(5);
        });

        services.AddSharedKeyStatelessCaptcha(options =>
        {
            // Generate this key once and store it securely (Azure Key Vault, etc.)
            options.SharedKey = Configuration["CaptchaSharedKey"]; // Base64 encoded 256-bit key
            options.FontStyle = FontStyle.Bold;
            options.DrawLines = true;
            options.TokenExpiration = TimeSpan.FromMinutes(10);
        });


        // Helper method to generate a new shared key (run once)
        // public static string GenerateSharedKey()
        // {
        //     using var rng = RandomNumberGenerator.Create();
        //     var keyBytes = new byte[32]; // 256 bits
        //     rng.GetBytes(keyBytes);
        //     return Convert.ToBase64String(keyBytes);
        // }
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
        app.UseSession().UseSessionCaptcha(options =>
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