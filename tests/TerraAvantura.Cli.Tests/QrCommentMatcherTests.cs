using System.Net;
using SkiaSharp;
using TerraAvantura.Cli.Models;
using TerraAvantura.Cli.Services;
using ZXing;
using ZXing.QrCode;
using ZXing.SkiaSharp;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie que QrCommentMatcher s'arrete au premier commentaire dont l'image contient un QR-code exploitable.
/// </summary>
public class QrCommentMatcherTests
{
    [Fact]
    public async Task FindFirstQrMatchAsync_ReturnsFirstSuccessfulDecode_AndSkipsEarlierNonQrImages()
    {
        var comments = new List<CommentInfo>
        {
            new("Pas de photo", null),
            new("Une photo sans QR-code", "https://example.test/plain.jpg"),
            new("Une photo avec le QR-code", "https://example.test/qr.jpg"),
            new("Un autre commentaire apres, ne doit pas etre atteint", "https://example.test/should-not-be-called.jpg"),
        };

        FakeImageHandler fakeHandler = new(new Dictionary<string, byte[]>
        {
            ["https://example.test/plain.jpg"] = GeneratePlainImagePng(),
            ["https://example.test/qr.jpg"] = GenerateQrCodePng("MOT-SECRET"),
        });
        HttpClient httpClient = new(fakeHandler);
        QrCommentMatcher matcher = new(new QrDecoderService(httpClient));

        QrMatch? match = await matcher.FindFirstQrMatchAsync(comments);

        Assert.NotNull(match);
        Assert.Equal("MOT-SECRET", match!.DecodedText);
        Assert.Equal("Une photo avec le QR-code", match.Comment.Text);
        Assert.False(fakeHandler.WasRequested("https://example.test/should-not-be-called.jpg"));
    }

    [Fact]
    public async Task FindFirstQrMatchAsync_ReturnsNull_WhenNoCommentHasAQrCode()
    {
        var comments = new List<CommentInfo>
        {
            new("Sans photo", null),
            new("Photo sans QR-code", "https://example.test/plain.jpg"),
        };

        FakeImageHandler fakeHandler = new(new Dictionary<string, byte[]>
        {
            ["https://example.test/plain.jpg"] = GeneratePlainImagePng(),
        });
        QrCommentMatcher matcher = new(new QrDecoderService(new HttpClient(fakeHandler)));

        QrMatch? match = await matcher.FindFirstQrMatchAsync(comments);

        Assert.Null(match);
    }

    private static byte[] GenerateQrCodePng(string content)
    {
        BarcodeWriter writer = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Width = 200, Height = 200, Margin = 1 },
        };

        using SKBitmap bitmap = writer.Write(content);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] GeneratePlainImagePng()
    {
        using SKBitmap bitmap = new(200, 200);
        bitmap.Erase(SKColors.White);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Faux handler HTTP retournant des octets d'image predefinis selon l'URL demandee, sans reseau reel.
    /// </summary>
    private sealed class FakeImageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, byte[]> _imagesByUrl;
        private readonly HashSet<string> _requestedUrls = new();

        public FakeImageHandler(Dictionary<string, byte[]> imagesByUrl)
        {
            _imagesByUrl = imagesByUrl;
        }

        public bool WasRequested(string url) => _requestedUrls.Contains(url);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri!.ToString();
            _requestedUrls.Add(url);

            byte[] bytes = _imagesByUrl.TryGetValue(url, out byte[]? found) ? found : throw new InvalidOperationException($"URL inattendue : {url}");
            HttpResponseMessage response = new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            return Task.FromResult(response);
        }
    }
}
