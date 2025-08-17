using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Numerics;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace Edi.Captcha;

public static class CaptchaImageGenerator
{
    private static readonly Random Rand = Random.Shared;
    
    private const int MinRotationDegrees = -10;
    private const int MaxRotationDegrees = 10;
    private const int TextPadding = 5; // Reduced padding to allow more space for text

    public static CaptchaResult GetImage(
        int width, 
        int height, 
        string captchaCode, 
        string fontName, 
        FontStyle fontStyle = FontStyle.Regular, 
        bool drawLines = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentException.ThrowIfNullOrWhiteSpace(captchaCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        using var ms = new MemoryStream();
        using var img = new Image<Rgba32>(width, height);
        
        DrawBackground(img);
        
        if (drawLines)
        {
            DrawRandomLines(img, width, height);
        }
        
        DrawCaptchaText(img, captchaCode, fontName, fontStyle, width, height);
        
        img.SaveAsPng(ms);

        return new CaptchaResult
        {
            CaptchaCode = captchaCode,
            CaptchaByteData = ms.ToArray(),
            Timestamp = DateTime.UtcNow
        };
    }

    private static void DrawCaptchaText(
        Image<Rgba32> img, 
        string captchaCode, 
        string fontName, 
        FontStyle fontStyle,
        int width,
        int height)
    {
        // Calculate safe drawing area
        var availableWidth = width - (TextPadding * 2);
        var availableHeight = height - (TextPadding * 2);
        
        // Increase font size calculation - make it more generous
        var charWidth = (float)availableWidth / captchaCode.Length;
        var heightBasedSize = availableHeight * 0.75f; // Increased from 0.6f to 0.75f
        var widthBasedSize = charWidth * 1.1f; // Increased from 0.8f to 1.1f
        var maxFontSize = Math.Min(widthBasedSize, heightBasedSize);
        
        // Set a higher minimum font size and use more of the available space
        var baseFontSize = Math.Max(16, (int)maxFontSize); // Increased minimum from 12 to 16
        
        var currentX = TextPadding;
        
        for (var i = 0; i < captchaCode.Length; i++)
        {
            var character = captchaCode[i];
            
            // Reduce font size variation to keep fonts larger
            var fontSize = baseFontSize + Rand.Next(-2, 3); // Reduced variation from (-3, 4) to (-2, 3)
            fontSize = Math.Max(14, Math.Min(fontSize, (int)maxFontSize)); // Increased minimum from 10 to 14
            var font = SystemFonts.CreateFont(fontName, fontSize, fontStyle);
            
            // Measure the character to ensure it fits
            var charBounds = TextMeasurer.MeasureBounds(character.ToString(), new TextOptions(font));
            
            // Calculate safe position within bounds
            var maxX = width - TextPadding - charBounds.Width;
            var maxY = height - TextPadding - charBounds.Height;
            
            var x = Math.Min(currentX + Rand.Next(0, 3), maxX); // Reduced random offset
            var baseY = (height - charBounds.Height) / 2;
            var y = Math.Max(TextPadding, Math.Min(baseY + Rand.Next(-8, 9), maxY)); // Reduced vertical variation
            
            var degrees = Rand.Next(MinRotationDegrees, MaxRotationDegrees);
            var location = new PointF(x, y);
            
            // Use the character center as rotation point
            var rotationCenter = new PointF(
                x + charBounds.Width / 2, 
                y + charBounds.Height / 2);
            
            img.Mutate(ctx =>
            {
                var transform = Matrix3x2.CreateRotation(
                    MathF.PI * degrees / 180f, 
                    rotationCenter);
                
                ctx.SetDrawingTransform(transform)
                   .DrawText(character.ToString(), font, GetRandomDeepColor(), location);
                
                // Reset transform for next character
                ctx.SetDrawingTransform(Matrix3x2.Identity);
            });
            
            // Move to next character position with tighter spacing
            currentX += (int)(charBounds.Width + Rand.Next(1, 4)); // Reduced spacing variation
        }
    }

    private static void DrawBackground(Image<Rgba32> img)
    {
        var backColor = Color.FromRgb(
            (byte)Rand.Next(220, 256),
            (byte)Rand.Next(220, 256),
            (byte)Rand.Next(220, 256));
        
        img.Mutate(ctx => ctx.BackgroundColor(backColor));
    }

    private static void DrawRandomLines(Image<Rgba32> img, int width, int height)
    {
        var lineCount = Rand.Next(2, 5);
        
        for (var i = 0; i < lineCount; i++)
        {
            var color = GetRandomLightColor();
            var thickness = Rand.NextSingle() * 2f + 0.5f;
            var startPoint = new PointF(Rand.Next(0, width), Rand.Next(0, height));
            var endPoint = new PointF(Rand.Next(0, width), Rand.Next(0, height));
            
            img.Mutate(ctx => ctx.DrawLine(color, thickness, startPoint, endPoint));
        }
    }

    private static Color GetRandomDeepColor()
    {
        const int maxColorValue = 120;
        return Color.FromRgb(
            (byte)Rand.Next(0, maxColorValue),
            (byte)Rand.Next(0, maxColorValue),
            (byte)Rand.Next(0, maxColorValue));
    }
    
    private static Color GetRandomLightColor()
    {
        const int minColorValue = 150;
        const int maxColorValue = 200;
        return Color.FromRgb(
            (byte)Rand.Next(minColorValue, maxColorValue),
            (byte)Rand.Next(minColorValue, maxColorValue),
            (byte)Rand.Next(minColorValue, maxColorValue));
    }
}