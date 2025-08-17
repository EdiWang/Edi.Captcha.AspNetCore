using NUnit.Framework;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading.Tasks;

namespace Edi.Captcha.Tests;

[TestFixture]
public class CaptchaImageGeneratorTests
{
    private const string TestFontName = "Arial";
    private const string TestCaptchaCode = "ABC123";

    [Test]
    public void GetImage_WithValidParameters_ReturnsValidCaptchaResult()
    {
        // Arrange
        const int width = 200;
        const int height = 100;
        const string captchaCode = "TEST123";

        // Act
        var result = CaptchaImageGenerator.GetImage(width, height, captchaCode, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaCode, Is.EqualTo(captchaCode));
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
        Assert.That(result.Timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(result.Timestamp, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
    }

    [Test]
    public void GetImage_WithMinimumValidSize_ReturnsValidResult()
    {
        // Arrange
        const int width = 1;
        const int height = 1;

        // Act
        var result = CaptchaImageGenerator.GetImage(width, height, TestCaptchaCode, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithLargeSize_ReturnsValidResult()
    {
        // Arrange
        const int width = 1000;
        const int height = 500;

        // Act
        var result = CaptchaImageGenerator.GetImage(width, height, TestCaptchaCode, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithDifferentFontStyles_ReturnsValidResults()
    {
        // Arrange
        var fontStyles = new[] { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic };

        foreach (var fontStyle in fontStyles)
        {
            // Act
            var result = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName, fontStyle);

            // Assert
            Assert.That(result, Is.Not.Null, $"Failed for font style: {fontStyle}");
            Assert.That(result.CaptchaByteData, Is.Not.Null);
            Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
        }
    }

    [Test]
    public void GetImage_WithDrawLinesTrue_ReturnsValidResult()
    {
        // Act
        var result = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName, FontStyle.Regular, true);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithDrawLinesFalse_ReturnsValidResult()
    {
        // Act
        var result = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName, FontStyle.Regular, false);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithSingleCharacter_ReturnsValidResult()
    {
        // Arrange
        const string singleChar = "A";

        // Act
        var result = CaptchaImageGenerator.GetImage(100, 50, singleChar, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaCode, Is.EqualTo(singleChar));
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithLongCaptchaCode_ReturnsValidResult()
    {
        // Arrange
        const string longCode = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

        // Act
        var result = CaptchaImageGenerator.GetImage(800, 100, longCode, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaCode, Is.EqualTo(longCode));
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithSpecialCharacters_ReturnsValidResult()
    {
        // Arrange
        const string specialChars = "!@#$%^&*()";

        // Act
        var result = CaptchaImageGenerator.GetImage(300, 100, specialChars, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaCode, Is.EqualTo(specialChars));
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_WithNumbers_ReturnsValidResult()
    {
        // Arrange
        const string numbers = "1234567890";

        // Act
        var result = CaptchaImageGenerator.GetImage(300, 100, numbers, TestFontName);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CaptchaCode, Is.EqualTo(numbers));
        Assert.That(result.CaptchaByteData, Is.Not.Null);
        Assert.That(result.CaptchaByteData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GetImage_GeneratesValidPngImage()
    {
        // Act
        var result = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName);

        // Assert
        Assert.That(result.CaptchaByteData, Is.Not.Null);

        // Verify it's a valid PNG by loading it
        using var image = Image.Load<Rgba32>(result.CaptchaByteData);
        Assert.That(image.Width, Is.EqualTo(200));
        Assert.That(image.Height, Is.EqualTo(100));
    }

    [Test]
    public void GetImage_ConsecutiveCalls_GenerateDifferentImages()
    {
        // Act
        var result1 = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName);
        var result2 = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName);

        // Assert
        Assert.That(result1.CaptchaByteData, Is.Not.EqualTo(result2.CaptchaByteData),
            "Consecutive calls should generate different images due to randomization");
    }

    [Test]
    public void GetImage_DifferentSizes_GenerateCorrectDimensions()
    {
        // Arrange
        var sizes = new[] { (100, 50), (300, 150), (500, 200) };

        foreach (var (width, height) in sizes)
        {
            // Act
            var result = CaptchaImageGenerator.GetImage(width, height, TestCaptchaCode, TestFontName);

            // Assert
            using var image = Image.Load<Rgba32>(result.CaptchaByteData);
            Assert.That(image.Width, Is.EqualTo(width), $"Width mismatch for size {width}x{height}");
            Assert.That(image.Height, Is.EqualTo(height), $"Height mismatch for size {width}x{height}");
        }
    }

    #region Parameter Validation Tests

    [Test]
    public void GetImage_WithZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptchaImageGenerator.GetImage(0, 100, TestCaptchaCode, TestFontName));

        Assert.That(ex.ParamName, Is.EqualTo("width"));
    }

    [Test]
    public void GetImage_WithNegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptchaImageGenerator.GetImage(-1, 100, TestCaptchaCode, TestFontName));

        Assert.That(ex.ParamName, Is.EqualTo("width"));
    }

    [Test]
    public void GetImage_WithZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptchaImageGenerator.GetImage(100, 0, TestCaptchaCode, TestFontName));

        Assert.That(ex.ParamName, Is.EqualTo("height"));
    }

    [Test]
    public void GetImage_WithNegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptchaImageGenerator.GetImage(100, -1, TestCaptchaCode, TestFontName));

        Assert.That(ex.ParamName, Is.EqualTo("height"));
    }

    [Test]
    public void GetImage_WithEmptyCaptchaCode_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CaptchaImageGenerator.GetImage(100, 100, "", TestFontName));

        Assert.That(ex.ParamName, Is.EqualTo("captchaCode"));
    }

    [Test]
    public void GetImage_WithWhitespaceCaptchaCode_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CaptchaImageGenerator.GetImage(100, 100, "   ", TestFontName));

        Assert.That(ex.ParamName, Is.EqualTo("captchaCode"));
    }

    [Test]
    public void GetImage_WithEmptyFontName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CaptchaImageGenerator.GetImage(100, 100, TestCaptchaCode, ""));

        Assert.That(ex.ParamName, Is.EqualTo("fontName"));
    }

    [Test]
    public void GetImage_WithWhitespaceFontName_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            CaptchaImageGenerator.GetImage(100, 100, TestCaptchaCode, "   "));

        Assert.That(ex.ParamName, Is.EqualTo("fontName"));
    }

    #endregion

    #region Edge Cases and Robustness Tests

    [Test]
    public void GetImage_WithVerySmallSize_HandlesGracefully()
    {
        // Act & Assert - Should not throw, even if image quality is poor
        Assert.DoesNotThrow(() =>
        {
            var result = CaptchaImageGenerator.GetImage(10, 10, "A", TestFontName);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CaptchaByteData, Is.Not.Null);
        });
    }

    [Test]
    public void GetImage_WithExtremeAspectRatio_HandlesGracefully()
    {
        // Test very wide image
        Assert.DoesNotThrow(() =>
        {
            var result = CaptchaImageGenerator.GetImage(1000, 10, TestCaptchaCode, TestFontName);
            Assert.That(result, Is.Not.Null);
        });

        // Test very tall image
        Assert.DoesNotThrow(() =>
        {
            var result = CaptchaImageGenerator.GetImage(10, 1000, TestCaptchaCode, TestFontName);
            Assert.That(result, Is.Not.Null);
        });
    }

    [Test]
    public void GetImage_WithUnicodeCharacters_HandlesGracefully()
    {
        // Arrange
        const string unicodeText = "¦Á¦Â¦Ã¦Ä¦ÅÖÐÎÄ²âÊÔ";

        // Act & Assert - Should handle gracefully even if font doesn't support all characters
        Assert.DoesNotThrow(() =>
        {
            var result = CaptchaImageGenerator.GetImage(300, 100, unicodeText, TestFontName);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CaptchaCode, Is.EqualTo(unicodeText));
        });
    }

    [Test]
    public void GetImage_MultipleThreadsSimultaneously_WorksCorrectly()
    {
        // Arrange
        const int threadCount = 10;
        var results = new CaptchaResult[threadCount];
        var tasks = new Task[threadCount];

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                results[index] = CaptchaImageGenerator.GetImage(200, 100, $"TEST{index}", TestFontName);
            });
        }

        Task.WaitAll(tasks);

        // Assert
        for (int i = 0; i < threadCount; i++)
        {
            Assert.That(results[i], Is.Not.Null, $"Result {i} should not be null");
            Assert.That(results[i].CaptchaCode, Is.EqualTo($"TEST{i}"), $"Result {i} captcha code mismatch");
            Assert.That(results[i].CaptchaByteData, Is.Not.Null, $"Result {i} byte data should not be null");
            Assert.That(results[i].CaptchaByteData.Length, Is.GreaterThan(0), $"Result {i} should have byte data");
        }

        // Verify all results are different (due to randomization)
        for (int i = 0; i < threadCount - 1; i++)
        {
            for (int j = i + 1; j < threadCount; j++)
            {
                Assert.That(results[i].CaptchaByteData, Is.Not.EqualTo(results[j].CaptchaByteData),
                    $"Results {i} and {j} should be different due to randomization");
            }
        }
    }

    #endregion

    #region Performance Tests

    [Test]
    public void GetImage_PerformanceTest_CompletesInReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 100; i++)
        {
            CaptchaImageGenerator.GetImage(200, 100, $"TEST{i}", TestFontName);
        }

        stopwatch.Stop();

        // Assert - Should complete 100 captchas in under 10 seconds
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10000),
            "Generating 100 captchas should complete in under 10 seconds");
    }

    [Test]
    public void GetImage_MemoryUsage_DoesNotLeak()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Generate many captchas
        for (int i = 0; i < 1000; i++)
        {
            var result = CaptchaImageGenerator.GetImage(200, 100, TestCaptchaCode, TestFontName);
            // Intentionally not keeping references to allow GC
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert - Memory should not have grown significantly (allow 10MB growth)
        var memoryGrowth = finalMemory - initialMemory;
        Assert.That(memoryGrowth, Is.LessThan(10 * 1024 * 1024),
            "Memory growth should be minimal after generating many captchas");
    }

    #endregion
}