using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace Edi.Captcha;

public static class CaptchaImageGenerator
{
    private static readonly Random Rand = new();

    public static CaptchaResult GetImage(
        int width, int height, string captchaCode, string fontName, FontStyle fontStyle = FontStyle.Regular, bool drawLines = true)
    {
        using var ms = new MemoryStream();

        var imgText = new Image<Rgba32>(width, height);
        DrawCaptchaText(imgText, captchaCode, fontName, fontStyle);

        using var img = new Image<Rgba32>(width, height);
        DrawBackground(img);

        if (drawLines)
        {
            DrawRandomLines(img, width, height);
        }

        // merge layers
        img.Mutate(ctx => ctx.DrawImage(imgText, 1f));
        img.SaveAsPng(ms);

        return new CaptchaResult
        {
            CaptchaCode = captchaCode,
            CaptchaByteData = ms.ToArray(),
            Timestamp = DateTime.UtcNow
        };
    }

    private static void DrawCaptchaText(Image<Rgba32> imgText, string captchaCode, string fontName, FontStyle fontStyle)
    {
        float position = 0;
        var averageSize = imgText.Width / captchaCode.Length;
        var fontSize = Convert.ToInt32(averageSize);
        var font = SystemFonts.CreateFont(fontName, fontSize, fontStyle);

        foreach (var c in captchaCode)
        {
            var x = Rand.Next(5, 10);
            var y = Rand.Next(6, 13);
            var degrees = Rand.Next(-10, 10);
            var location = new PointF(x + position, y);

            imgText.Mutate(ctx =>
                ctx.SetDrawingTransform(Matrix3x2Extensions.CreateRotationDegrees(degrees, new PointF(0, 0)))
                    .DrawText(c.ToString(), font, GetRandomDeepColor(), location));

            position += TextMeasurer.MeasureBounds(c.ToString(), new(font)).Width;
        }
    }

    private static void DrawBackground(Image<Rgba32> img)
    {
        var backColor = Color.FromRgb(
            (byte)Rand.Next(180, 256),
            (byte)Rand.Next(180, 256),
            (byte)Rand.Next(180, 256));
        img.Mutate(ctx => ctx.BackgroundColor(backColor));
    }

    private static void DrawRandomLines(Image<Rgba32> img, int width, int height)
    {
        for (var i = 0; i < Rand.Next(3, 7); i++)
        {
            var color = GetRandomDeepColor();
            var startPoint = new PointF(Rand.Next(0, width), Rand.Next(0, height));
            var endPoint = new PointF(Rand.Next(0, width), Rand.Next(0, height));
            img.Mutate(ctx => ctx.DrawLine(color, 1, startPoint, endPoint));
        }
    }

    private static Color GetRandomDeepColor()
    {
        int redlow = 160, greenLow = 100, blueLow = 160;
        return Color.FromRgb((byte)Rand.Next(redlow), (byte)Rand.Next(greenLow), (byte)Rand.Next(blueLow));
    }
}