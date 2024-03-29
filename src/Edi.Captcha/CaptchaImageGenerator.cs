﻿using SixLabors.Fonts;
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
    public static CaptchaResult GetImage(
        int width, int height, string captchaCode, string fontName, FontStyle fontStyle = FontStyle.Regular, bool drawLines = true)
    {
        using var ms = new MemoryStream();
        var rand = new Random();

        using (var imgText = new Image<Rgba32>(width, height))
        {
            // characters layer
            float position = 0;
            var averageSize = width / captchaCode.Length;
            var fontSize = Convert.ToInt32(averageSize);

            Font font = SystemFonts.CreateFont(fontName, fontSize, fontStyle);

            Random random = new Random();

            foreach (char c in captchaCode)
            {
                var x = rand.Next(5, 10);
                var y = rand.Next(6, 13);

                var degrees = random.Next(0, 10) * (random.Next(-10, 10) > 0 ? 1 : -1);

                var location = new PointF(x + position, y);
                imgText.Mutate(ctx =>
                    ctx.SetDrawingTransform(Matrix3x2Extensions.CreateRotationDegrees(degrees, new(0, 0)))
                        .DrawText(c.ToString(), font, GetRandomDeepColor(), location));
                position += TextMeasurer.MeasureBounds(c.ToString(), new(font)).Width;
            }

            // background layer
            int low = 180, high = 255;
            var nRend = rand.Next(high) % (high - low) + low;
            var nGreen = rand.Next(high) % (high - low) + low;
            var nBlue = rand.Next(high) % (high - low) + low;
            var backColor = Color.FromRgb((byte)nRend, (byte)nGreen, (byte)nBlue);
            var img = new Image<Rgba32>(width, height);
            img.Mutate(ctx => ctx.BackgroundColor(backColor));

            if (drawLines)
            {
                // lines
                for (var i = 0; i < rand.Next(3, 7); i++)
                {
                    var color = GetRandomDeepColor();
                    var startPoint = new PointF(rand.Next(0, width), rand.Next(0, height));
                    var endPoint = new PointF(rand.Next(0, width), rand.Next(0, height));
                    img.Mutate(ctx => ctx.DrawLine(color, 1, startPoint, endPoint));
                }
            }

            // merge layers
            img.Mutate(ctx => ctx.DrawImage(imgText, 1f));
            img.SaveAsPng(ms);
        }

        return new()
        {
            CaptchaCode = captchaCode,
            CaptchaByteData = ms.ToArray(),
            Timestamp = DateTime.UtcNow
        };

        Color GetRandomDeepColor()
        {
            int redlow = 160, greenLow = 100, blueLow = 160;
            return Color.FromRgb((byte)rand.Next(redlow), (byte)rand.Next(greenLow), (byte)rand.Next(blueLow));
        }
    }
}