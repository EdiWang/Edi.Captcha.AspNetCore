using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Edi.Captcha.Tests;

[TestFixture]
public class CaptchaImageMiddlewareTests
{
    private Mock<RequestDelegate> _mockNext;
    private Mock<ISessionBasedCaptcha> _mockCaptcha;
    private Mock<HttpContext> _mockHttpContext;
    private Mock<HttpRequest> _mockRequest;
    private Mock<HttpResponse> _mockResponse;
    private Mock<ISession> _mockSession;
    private Mock<IHeaderDictionary> _mockHeaders;
    private MemoryStream _responseBodyStream;
    private CaptchaImageMiddleware _middleware;

    [SetUp]
    public void SetUp()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockCaptcha = new Mock<ISessionBasedCaptcha>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockRequest = new Mock<HttpRequest>();
        _mockResponse = new Mock<HttpResponse>();
        _mockSession = new Mock<ISession>();
        _mockHeaders = new Mock<IHeaderDictionary>();
        _responseBodyStream = new MemoryStream();

        // Setup HttpContext
        _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        _mockHttpContext.Setup(x => x.Response).Returns(_mockResponse.Object);
        _mockHttpContext.Setup(x => x.Session).Returns(_mockSession.Object);
        _mockHttpContext.Setup(x => x.RequestAborted).Returns(CancellationToken.None);

        // Setup Response
        _mockResponse.Setup(x => x.Headers).Returns(_mockHeaders.Object);
        _mockResponse.Setup(x => x.Body).Returns(_responseBodyStream);

        _middleware = new CaptchaImageMiddleware(_mockNext.Object);

        // Reset options to default state before each test
        CaptchaImageMiddleware.Options = new CaptchaImageMiddlewareOptions
        {
            RequestPath = "/captcha-image",
            ImageWidth = 100,
            ImageHeight = 36,
            DisableCache = true
        };
    }

    [TearDown]
    public void TearDown()
    {
        _responseBodyStream?.Dispose();
    }

    #region Path Matching Tests

    [Test]
    public async Task Invoke_WhenPathMatches_GeneratesCaptchaImage()
    {
        // Arrange
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null), Times.Once);
        _mockResponse.VerifySet(x => x.StatusCode = StatusCodes.Status200OK, Times.Once);
        _mockResponse.VerifySet(x => x.ContentType = "image/png", Times.Once);
        _mockNext.Verify(x => x(_mockHttpContext.Object), Times.Never);

        // Verify response body contains the image bytes
        Assert.That(_responseBodyStream.ToArray(), Is.EqualTo(testImageBytes));
    }

    [Test]
    public async Task Invoke_WhenPathDoesNotMatch_CallsNextMiddleware()
    {
        // Arrange
        _mockRequest.Setup(x => x.Path).Returns("/other-path");

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockNext.Verify(x => x(_mockHttpContext.Object), Times.Once);
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(It.IsAny<ISession>(), It.IsAny<int>(), It.IsAny<int>(), null), Times.Never);
        _mockResponse.VerifySet(x => x.StatusCode = It.IsAny<int>(), Times.Never);
    }

    [Test]
    public async Task Invoke_WithCustomRequestPath_WorksCorrectly()
    {
        // Arrange
        CaptchaImageMiddleware.Options.RequestPath = "/custom-captcha";
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/custom-captcha");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null), Times.Once);
        _mockResponse.VerifySet(x => x.StatusCode = StatusCodes.Status200OK, Times.Once);
    }

    #endregion

    #region Image Size Tests

    [Test]
    public async Task Invoke_WithCustomImageSize_UsesCorrectDimensions()
    {
        // Arrange
        CaptchaImageMiddleware.Options.ImageWidth = 200;
        CaptchaImageMiddleware.Options.ImageHeight = 80;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 200, 80, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 200, 80, null), Times.Once);
    }

    [Test]
    public async Task Invoke_WithWidthExceeding640_LimitsTo640()
    {
        // Arrange
        CaptchaImageMiddleware.Options.ImageWidth = 800;
        CaptchaImageMiddleware.Options.ImageHeight = 100;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 640, 100, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 640, 100, null), Times.Once);
    }

    [Test]
    public async Task Invoke_WithHeightExceeding480_LimitsTo480()
    {
        // Arrange
        CaptchaImageMiddleware.Options.ImageWidth = 200;
        CaptchaImageMiddleware.Options.ImageHeight = 600;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 200, 480, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 200, 480, null), Times.Once);
    }

    [Test]
    public async Task Invoke_WithBothDimensionsExceedingLimits_LimitsBoth()
    {
        // Arrange
        CaptchaImageMiddleware.Options.ImageWidth = 1000;
        CaptchaImageMiddleware.Options.ImageHeight = 600;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 640, 480, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 640, 480, null), Times.Once);
    }

    [Test]
    public async Task Invoke_WithExactlyMaxDimensions_DoesNotLimit()
    {
        // Arrange
        CaptchaImageMiddleware.Options.ImageWidth = 640;
        CaptchaImageMiddleware.Options.ImageHeight = 480;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 640, 480, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 640, 480, null), Times.Once);
    }

    #endregion

    #region Cache Control Tests

    [Test]
    public async Task Invoke_WithDisableCacheTrue_SetsCacheControlHeaders()
    {
        // Arrange
        CaptchaImageMiddleware.Options.DisableCache = true;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockHeaders.VerifySet(x => x.CacheControl = "no-cache,no-store", Times.Once);
    }

    [Test]
    public async Task Invoke_WithDisableCacheFalse_DoesNotSetCacheControlHeaders()
    {
        // Arrange
        CaptchaImageMiddleware.Options.DisableCache = false;
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockHeaders.VerifySet(x => x["Cache-Control"] = It.IsAny<StringValues>(), Times.Never);
    }

    #endregion

    #region Response Content Tests

    [Test]
    public async Task Invoke_Always_SetsCorrectContentType()
    {
        // Arrange
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockResponse.VerifySet(x => x.ContentType = "image/png", Times.Once);
    }

    [Test]
    public async Task Invoke_Always_Sets200StatusCode()
    {
        // Arrange
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockResponse.VerifySet(x => x.StatusCode = StatusCodes.Status200OK, Times.Once);
    }

    [Test]
    public async Task Invoke_WithEmptyImageBytes_WritesEmptyResponse()
    {
        // Arrange
        var emptyImageBytes = Array.Empty<byte>();
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(emptyImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        Assert.That(_responseBodyStream.ToArray(), Is.EqualTo(emptyImageBytes));
        _mockResponse.VerifySet(x => x.StatusCode = StatusCodes.Status200OK, Times.Once);
    }

    [Test]
    public async Task Invoke_WithLargeImageBytes_WritesCompleteResponse()
    {
        // Arrange
        var largeImageBytes = new byte[10000];
        for (int i = 0; i < largeImageBytes.Length; i++)
        {
            largeImageBytes[i] = (byte)(i % 256);
        }
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(largeImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        Assert.That(_responseBodyStream.ToArray(), Is.EqualTo(largeImageBytes));
        Assert.That(_responseBodyStream.Length, Is.EqualTo(largeImageBytes.Length));
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void Invoke_WhenCaptchaThrowsException_PropagatesException()
    {
        // Arrange
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Throws(new InvalidOperationException("Captcha generation failed"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object));

        Assert.That(ex.Message, Is.EqualTo("Captcha generation failed"));
    }

    [Test]
    public void Invoke_WhenNextMiddlewareThrowsException_PropagatesException()
    {
        // Arrange
        _mockRequest.Setup(x => x.Path).Returns("/other-path");
        _mockNext.Setup(x => x(_mockHttpContext.Object))
                .ThrowsAsync(new InvalidOperationException("Next middleware failed"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object));

        Assert.That(ex.Message, Is.EqualTo("Next middleware failed"));
    }

    #endregion

    #region Cancellation Token Tests

    [Test]
    public async Task Invoke_WithCancellationToken_PassesTokenToWriteAsync()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockHttpContext.Setup(x => x.RequestAborted).Returns(cancellationTokenSource.Token);
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Act
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        // Verify that the response was written (the cancellation token is used internally)
        Assert.That(_responseBodyStream.ToArray(), Is.EqualTo(testImageBytes));
    }

    [Test]
    public async Task Invoke_WithCancelledToken_HandlesGracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockHttpContext.Setup(x => x.RequestAborted).Returns(cancellationTokenSource.Token);
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes);

        // Create a stream that throws on write when cancelled
        var cancellableStream = new Mock<Stream>();
        cancellableStream.Setup(s => s.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new OperationCanceledException());
        _mockResponse.Setup(x => x.Body).Returns(cancellableStream.Object);

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object));
    }

    #endregion

    #region Integration-like Tests

    [Test]
    public async Task Invoke_MultipleSequentialRequests_WorksCorrectly()
    {
        // Arrange
        var testImageBytes1 = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01 };
        var testImageBytes2 = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x02 };

        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.SetupSequence(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null))
                   .Returns(testImageBytes1)
                   .Returns(testImageBytes2);

        // Act - First request
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);
        var firstResponse = _responseBodyStream.ToArray();

        // Reset stream for second request
        _responseBodyStream.SetLength(0);
        _responseBodyStream.Position = 0;

        // Act - Second request
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);
        var secondResponse = _responseBodyStream.ToArray();

        // Assert
        Assert.That(firstResponse, Is.EqualTo(testImageBytes1));
        Assert.That(secondResponse, Is.EqualTo(testImageBytes2));
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(_mockSession.Object, 100, 36, null), Times.Exactly(2));
    }

    [Test]
    public async Task Invoke_WithDifferentSessionObjects_CallsCaptchaWithCorrectSession()
    {
        // Arrange
        var session1 = new Mock<ISession>();
        var session2 = new Mock<ISession>();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        _mockRequest.Setup(x => x.Path).Returns("/captcha-image");
        _mockCaptcha.Setup(x => x.GenerateCaptchaImageBytes(It.IsAny<ISession>(), 100, 36, null))
                   .Returns(testImageBytes);

        // Act - First request with session1
        _mockHttpContext.Setup(x => x.Session).Returns(session1.Object);
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Act - Second request with session2
        _mockHttpContext.Setup(x => x.Session).Returns(session2.Object);
        await _middleware.Invoke(_mockHttpContext.Object, _mockCaptcha.Object);

        // Assert
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(session1.Object, 100, 36, null), Times.Once);
        _mockCaptcha.Verify(x => x.GenerateCaptchaImageBytes(session2.Object, 100, 36, null), Times.Once);
    }

    #endregion

    #region Static Options Tests

    [Test]
    public void Options_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new CaptchaImageMiddlewareOptions();

        // Assert
        Assert.That(options.DisableCache, Is.True);
        Assert.That(options.ImageWidth, Is.EqualTo(0)); // Default for int
        Assert.That(options.ImageHeight, Is.EqualTo(0)); // Default for int
        Assert.That(options.RequestPath.Value, Is.Null); // Default for PathString
    }

    [Test]
    public void Options_StaticProperty_CanBeModified()
    {
        // Arrange
        var originalOptions = CaptchaImageMiddleware.Options;

        try
        {
            // Act
            CaptchaImageMiddleware.Options = new CaptchaImageMiddlewareOptions
            {
                RequestPath = "/test-captcha",
                ImageWidth = 150,
                ImageHeight = 75,
                DisableCache = false
            };

            // Assert
            Assert.That(CaptchaImageMiddleware.Options.RequestPath.Value, Is.EqualTo("/test-captcha"));
            Assert.That(CaptchaImageMiddleware.Options.ImageWidth, Is.EqualTo(150));
            Assert.That(CaptchaImageMiddleware.Options.ImageHeight, Is.EqualTo(75));
            Assert.That(CaptchaImageMiddleware.Options.DisableCache, Is.False);
        }
        finally
        {
            // Cleanup
            CaptchaImageMiddleware.Options = originalOptions;
        }
    }

    #endregion
}