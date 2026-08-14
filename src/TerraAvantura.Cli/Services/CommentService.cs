using HtmlAgilityPack;
using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Recupere les commentaires recents d'un parcours (page par defaut, sans pagination profonde :
/// le QR-code recherche est un commentaire recent, la pagination complete n'apporte pas de valeur
/// pour ~600 parcours et coute tres cher en requetes).
/// </summary>
public sealed class CommentService
{
    private const string CommentSelector = "//article[contains(concat(' ', normalize-space(@class), ' '), ' js-comment ')]";

    private readonly HttpClient _httpClient;

    public CommentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Recupere les commentaires affiches par defaut sur la page d'un parcours.
    /// </summary>
    public async Task<IReadOnlyList<CommentInfo>> GetCommentsAsync(string parcoursUrl)
    {
        string html = await _httpClient.GetStringAsync(parcoursUrl);
        return ExtractComments(html);
    }

    /// <summary>
    /// Extrait chaque commentaire (texte + image eventuelle) depuis la page HTML d'un parcours.
    /// </summary>
    internal static IReadOnlyList<CommentInfo> ExtractComments(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNodeCollection? commentNodes = document.DocumentNode.SelectNodes(CommentSelector);
        if (commentNodes is null)
        {
            return Array.Empty<CommentInfo>();
        }

        return commentNodes.Select(ExtractComment).ToList();
    }

    private static CommentInfo ExtractComment(HtmlNode commentNode)
    {
        string text = ExtractText(commentNode);
        string? imageUrl = ExtractImageUrl(commentNode);
        return new CommentInfo(text, imageUrl);
    }

    private static string ExtractText(HtmlNode commentNode)
    {
        HtmlNode? bodyNode = commentNode.SelectSingleNode(".//div[contains(@class, 'js-body')]");
        return HtmlEntity.DeEntitize(bodyNode?.InnerText.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Extrait l'URL complete de la photo jointe au commentaire, si presente.
    /// </summary>
    private static string? ExtractImageUrl(HtmlNode commentNode)
    {
        HtmlNode? imageNode = commentNode.SelectSingleNode(".//div[contains(@class, 'node--cache-comment-picture')]//img");
        return imageNode?.GetAttributeValue("src", string.Empty);
    }
}
