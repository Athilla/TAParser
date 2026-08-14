using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Combine la liste des commentaires d'un parcours avec le decodeur QR : recherche le premier
/// commentaire dont l'image contient un QR-code exploitable, puis s'arrete (un seul resultat par parcours).
/// </summary>
public sealed class QrCommentMatcher
{
    private readonly QrDecoderService _qrDecoderService;

    public QrCommentMatcher(QrDecoderService qrDecoderService)
    {
        _qrDecoderService = qrDecoderService;
    }

    /// <summary>
    /// Parcourt les commentaires ayant une image et retourne le premier QR-code decode avec succes.
    /// Retourne null si aucun des commentaires ne contient de QR-code exploitable.
    /// </summary>
    public async Task<QrMatch?> FindFirstQrMatchAsync(IReadOnlyList<CommentInfo> comments)
    {
        foreach (CommentInfo comment in comments.Where(c => c.ImageUrl is not null))
        {
            string? decodedText = await _qrDecoderService.TryDecodeAsync(comment.ImageUrl!);
            if (decodedText is not null)
            {
                return new QrMatch(comment, decodedText);
            }
        }

        return null;
    }
}

/// <summary>
/// Resultat d'une recherche de QR-code reussie : le commentaire source et le texte decode.
/// </summary>
public sealed record QrMatch(CommentInfo Comment, string DecodedText);
