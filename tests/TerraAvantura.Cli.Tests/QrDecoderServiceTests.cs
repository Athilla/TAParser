using SkiaSharp;
using TerraAvantura.Cli.Services;
using ZXing;
using ZXing.QrCode;
using ZXing.SkiaSharp;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie le decodage de QR-code (generation en memoire via ZXing pour un test autonome, sans reseau).
/// </summary>
public class QrDecoderServiceTests
{
    [Fact]
    public void TryDecode_ReturnsEncodedText_WhenImageContainsQrCode()
    {
        byte[] qrImageBytes = GenerateQrCodePng("BADGE-Z123");
        var decoder = new QrDecoderService(new HttpClient());

        string? decoded = decoder.TryDecode(qrImageBytes);

        Assert.Equal("BADGE-Z123", decoded);
    }

    [Fact]
    public void TryDecode_ReturnsNull_WhenImageHasNoQrCode()
    {
        byte[] plainImageBytes = GeneratePlainImagePng();
        var decoder = new QrDecoderService(new HttpClient());

        string? decoded = decoder.TryDecode(plainImageBytes);

        Assert.Null(decoded);
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
}
