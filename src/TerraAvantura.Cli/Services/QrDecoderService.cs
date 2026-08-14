using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Telecharge une image et tente d'y decoder un QR-code exploitable.
/// </summary>
public sealed class QrDecoderService
{
    private readonly HttpClient _httpClient;
    private readonly BarcodeReader _barcodeReader;

    public QrDecoderService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = { PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE } },
        };
    }

    /// <summary>
    /// Telecharge l'image a l'URL donnee et tente d'y decoder un QR-code. Retourne null si aucun QR n'est trouve.
    /// </summary>
    public async Task<string?> TryDecodeAsync(string imageUrl)
    {
        byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
        return TryDecode(imageBytes);
    }

    /// <summary>
    /// Tente de decoder un QR-code depuis des octets d'image deja en memoire (utilise par les tests).
    /// </summary>
    internal string? TryDecode(byte[] imageBytes)
    {
        using SKBitmap bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap is null)
        {
            return null;
        }

        Result? result = _barcodeReader.Decode(bitmap);
        return result?.Text;
    }
}
