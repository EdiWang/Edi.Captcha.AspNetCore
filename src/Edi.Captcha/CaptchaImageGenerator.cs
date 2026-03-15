using System;

namespace Edi.Captcha;

public static class CaptchaImageGenerator
{
    private static readonly Random Rand = Random.Shared;

    private const int MinRotationDegrees = -10;
    private const int MaxRotationDegrees = 10;
    private const int TextPadding = 5;

    public static CaptchaResult GetImage(
        int width,
        int height,
        string captchaCode,
        string fontName,
        CaptchaFontStyle fontStyle = CaptchaFontStyle.Regular,
        bool drawLines = true)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentException.ThrowIfNullOrWhiteSpace(captchaCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        using var img = new CaptchaImage(width, height);

        DrawBackground(img);

        if (drawLines)
        {
            DrawRandomLines(img, width, height);
        }

        DrawCaptchaText(img, captchaCode, fontStyle, width, height);

        return new CaptchaResult
        {
            CaptchaCode = captchaCode,
            CaptchaByteData = img.EncodePng(),
            Timestamp = DateTime.UtcNow
        };
    }

    private static void DrawCaptchaText(
        CaptchaImage img,
        string captchaCode,
        CaptchaFontStyle fontStyle,
        int width,
        int height)
    {
        var availableWidth = width - (TextPadding * 2);
        var availableHeight = height - (TextPadding * 2);

        // Calculate scale factor based on available space
        var charSlotWidth = (float)availableWidth / captchaCode.Length;
        var scaleByWidth = charSlotWidth / CaptchaFont.GlyphWidth;
        var scaleByHeight = availableHeight * 0.75f / CaptchaFont.GlyphHeight;
        var scale = Math.Max(1, (int)Math.Min(scaleByWidth, scaleByHeight));

        var currentX = TextPadding;

        for (var i = 0; i < captchaCode.Length; i++)
        {
            var character = captchaCode[i];

            var charW = img.MeasureCharWidth(character, scale, fontStyle);
            var charH = img.MeasureCharHeight(scale);

            var maxX = Math.Max(currentX, width - TextPadding - charW);
            var maxY = Math.Max(TextPadding, height - TextPadding - charH);

            var x = Math.Min(currentX + Rand.Next(0, 3), maxX);
            var baseY = (height - charH) / 2;
            var y = Math.Max(TextPadding, Math.Min(baseY + Rand.Next(-4, 5), maxY));

            var degrees = Rand.Next(MinRotationDegrees, MaxRotationDegrees);

            GetRandomDeepColor(out var r, out var g, out var b);
            img.DrawCharacter(character, x, y, scale, r, g, b, fontStyle, degrees);

            currentX += charW + Rand.Next(1, 4);
        }
    }

    private static void DrawBackground(CaptchaImage img)
    {
        img.Fill(
            (byte)Rand.Next(220, 256),
            (byte)Rand.Next(220, 256),
            (byte)Rand.Next(220, 256));
    }

    private static void DrawRandomLines(CaptchaImage img, int width, int height)
    {
        var lineCount = Rand.Next(2, 5);

        for (var i = 0; i < lineCount; i++)
        {
            GetRandomLightColor(out var r, out var g, out var b);
            var thickness = Rand.NextSingle() * 2f + 0.5f;
            img.DrawLine(
                Rand.Next(0, width), Rand.Next(0, height),
                Rand.Next(0, width), Rand.Next(0, height),
                r, g, b, thickness);
        }
    }

    private static void GetRandomDeepColor(out byte r, out byte g, out byte b)
    {
        const int maxColorValue = 120;
        r = (byte)Rand.Next(0, maxColorValue);
        g = (byte)Rand.Next(0, maxColorValue);
        b = (byte)Rand.Next(0, maxColorValue);
    }

    private static void GetRandomLightColor(out byte r, out byte g, out byte b)
    {
        const int minColorValue = 150;
        const int maxColorValue = 200;
        r = (byte)Rand.Next(minColorValue, maxColorValue);
        g = (byte)Rand.Next(minColorValue, maxColorValue);
        b = (byte)Rand.Next(minColorValue, maxColorValue);
    }
}