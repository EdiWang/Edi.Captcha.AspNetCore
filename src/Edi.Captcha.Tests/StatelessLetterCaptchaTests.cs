using Microsoft.AspNetCore.DataProtection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Edi.Captcha.Tests;

[TestFixture]
public class StatelessLetterCaptchaTests
{
    private Mock<IDataProtectionProvider> _mockDataProtectionProvider;
    private Mock<IDataProtector> _mockDataProtector;
    private StatelessLetterCaptchaOptions _defaultOptions;
    private StatelessLetterCaptcha _captcha;

    [SetUp]
    public void SetUp()
    {
        _mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
        _mockDataProtector = new Mock<IDataProtector>();

        _mockDataProtectionProvider
            .Setup(x => x.CreateProtector("Edi.Captcha.Stateless"))
            .Returns(_mockDataProtector.Object);

        _defaultOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "2346789ABCDGHKMNPRUVWXYZ",
            CodeLength = 4
        };

        _captcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, _defaultOptions);
    }

    [Test]
    public void GenerateCaptchaCode_WithDefaultOptions_ReturnsCorrectLength()
    {
        // Act
        var result = _captcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(4));
    }

    [Test]
    public void GenerateCaptchaCode_WithDefaultOptions_ReturnsUpperCaseString()
    {
        // Act
        var result = _captcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(result.ToUpper()));
    }

    [Test]
    public void GenerateCaptchaCode_WithDefaultOptions_ContainsOnlyValidCharacters()
    {
        // Act
        var result = _captcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        foreach (char c in result)
        {
            Assert.That(_defaultOptions.Letters.Contains(c), Is.True,
                $"Generated code contains invalid character: {c}");
        }
    }

    [Test]
    public void GenerateCaptchaCode_WithCustomCodeLength_ReturnsCorrectLength()
    {
        // Arrange
        var customOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CodeLength = 6
        };
        var customCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, customOptions);

        // Act
        var result = customCaptcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(6));
    }

    [Test]
    public void GenerateCaptchaCode_WithCustomLetters_ContainsOnlyCustomCharacters()
    {
        // Arrange
        var customOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABC123",
            CodeLength = 4
        };
        var customCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, customOptions);

        // Act
        var result = customCaptcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        foreach (char c in result)
        {
            Assert.That("ABC123".Contains(c), Is.True,
                $"Generated code contains invalid character: {c}");
        }
    }

    [Test]
    public void GenerateCaptchaCode_WithMinimumCodeLength_ReturnsOneCharacter()
    {
        // Arrange
        var minOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CodeLength = 1
        };
        var minCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, minOptions);

        // Act
        var result = minCaptcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(1));
    }

    [Test]
    public void GenerateCaptchaCode_WithMaximumCodeLength_Returns32Characters()
    {
        // Arrange
        var maxOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CodeLength = 32
        };
        var maxCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, maxOptions);

        // Act
        var result = maxCaptcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(32));
    }

    [Test]
    public void GenerateCaptchaCode_WithCodeLengthZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CodeLength = 0
        };
        var invalidCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, invalidOptions);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => invalidCaptcha.GenerateCaptchaCode());
        Assert.That(ex.ParamName?.ToLower(), Does.Contain("codelength"));
        Assert.That(ex.Message, Does.Contain("codeLength must range within 1-32"));
    }

    [Test]
    public void GenerateCaptchaCode_WithCodeLengthNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CodeLength = -1
        };
        var invalidCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, invalidOptions);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => invalidCaptcha.GenerateCaptchaCode());
        Assert.That(ex.ParamName?.ToLower(), Does.Contain("codelength"));
        Assert.That(ex.Message, Does.Contain("current value is -1"));
    }

    [Test]
    public void GenerateCaptchaCode_WithCodeLengthGreaterThan32_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            CodeLength = 33
        };
        var invalidCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, invalidOptions);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => invalidCaptcha.GenerateCaptchaCode());
        Assert.That(ex.ParamName?.ToLower(), Does.Contain("codelength"));
        Assert.That(ex.Message, Does.Contain("current value is 33"));
    }

    [Test]
    public void GenerateCaptchaCode_CallMultipleTimes_ReturnsVariousResults()
    {
        // Arrange
        var results = new HashSet<string>();
        const int numberOfCalls = 100;

        // Act
        for (int i = 0; i < numberOfCalls; i++)
        {
            results.Add(_captcha.GenerateCaptchaCode());
        }

        // Assert
        // We expect some variation in results (though theoretically all could be the same)
        // With 4 characters from 23 possible letters, probability of all identical is extremely low
        Assert.That(results.Count, Is.GreaterThan(1),
            "Expected some variation in generated captcha codes");
    }

    [Test]
    public void GenerateCaptchaCode_WithSingleLetter_ReturnsOnlyThatLetter()
    {
        // Arrange
        var singleLetterOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "X",
            CodeLength = 4
        };
        var singleLetterCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, singleLetterOptions);

        // Act
        var result = singleLetterCaptcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("XXXX"));
    }

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.That(_captcha, Is.Not.Null);
        Assert.That(_captcha, Is.InstanceOf<StatelessLetterCaptcha>());
        Assert.That(_captcha, Is.InstanceOf<StatelessCaptcha>());
        Assert.That(_captcha, Is.InstanceOf<IStatelessCaptcha>());
    }

    [Test]
    public void GenerateCaptchaCode_ConsistentBehavior_AlwaysReturnsUpperCase()
    {
        // Arrange
        var mixedCaseOptions = new StatelessLetterCaptchaOptions
        {
            Letters = "abcDEF123",  // Mixed case input
            CodeLength = 6
        };
        var mixedCaseCaptcha = new StatelessLetterCaptcha(_mockDataProtectionProvider.Object, mixedCaseOptions);

        // Act
        var result = mixedCaseCaptcha.GenerateCaptchaCode();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(result.ToUpper()));
        // Verify all characters are from the expected set (after upper case conversion)
        foreach (char c in result)
        {
            Assert.That("ABCDEF123".Contains(c), Is.True);
        }
    }
}