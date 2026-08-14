using System.Globalization;
using HtmlAgilityPack;
using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Recupere la liste complete des parcours (ville + lien) depuis la page /parcours.
/// Cette page expose tous les parcours en une seule reponse HTML, sans pagination.
/// </summary>
public sealed class ParcoursService
{
    private const string ParcoursListPath = "/parcours";
    private const string ArticleSelector = "//article[contains(concat(' ', normalize-space(@class), ' '), ' gc-caches-liste ')]";

    private readonly HttpClient _httpClient;

    public ParcoursService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Retourne la liste de tous les parcours disponibles sur le site.
    /// </summary>
    public async Task<IReadOnlyList<ParcoursReference>> GetAllParcoursAsync(string baseUrl)
    {
        string html = await _httpClient.GetStringAsync(CombineUrl(baseUrl, ParcoursListPath));
        return ExtractParcours(html, baseUrl);
    }

    /// <summary>
    /// Extrait chaque parcours (article.gc-caches-liste) de la page listing.
    /// </summary>
    internal static IReadOnlyList<ParcoursReference> ExtractParcours(string html, string baseUrl)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNodeCollection? articles = document.DocumentNode.SelectNodes(ArticleSelector);
        if (articles is null)
        {
            return Array.Empty<ParcoursReference>();
        }

        return articles
            .Select(article => TryExtractParcours(article, baseUrl))
            .Where(parcours => parcours is not null)
            .Select(parcours => parcours!)
            .ToList();
    }

    /// <summary>
    /// Extrait un seul parcours a partir de son noeud article ; retourne null si une donnee essentielle manque.
    /// </summary>
    private static ParcoursReference? TryExtractParcours(HtmlNode article, string baseUrl)
    {
        int? nodeId = ExtractNodeId(article);
        string title = ExtractTitle(article);
        string city = ExtractCity(article);
        string? relativeUrl = ExtractRelativeUrl(article);

        if (nodeId is null || relativeUrl is null)
        {
            return null;
        }

        return new ParcoursReference(nodeId.Value, title, city, CombineUrl(baseUrl, relativeUrl));
    }

    /// <summary>
    /// Extrait l'identifiant de noeud Drupal depuis la classe "js-gc-map-nid-{id}".
    /// </summary>
    private static int? ExtractNodeId(HtmlNode article)
    {
        string classAttribute = article.GetAttributeValue("class", string.Empty);
        string? marker = classAttribute
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(token => token.StartsWith("js-gc-map-nid-", StringComparison.Ordinal));

        if (marker is null)
        {
            return null;
        }

        string idText = marker["js-gc-map-nid-".Length..];
        return int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : null;
    }

    private static string ExtractTitle(HtmlNode article)
    {
        HtmlNode? titleNode = article.SelectSingleNode(".//span[contains(@class, 'gc-caches-liste-title')]");
        return HtmlEntity.DeEntitize(titleNode?.InnerText.Trim() ?? string.Empty);
    }

    private static string ExtractCity(HtmlNode article)
    {
        HtmlNode? cityNode = article.SelectSingleNode(".//div[contains(@class, 'field--name-field-city-postal')]//div[contains(@class, 'field__item')]");
        string rawText = HtmlEntity.DeEntitize(cityNode?.InnerText ?? string.Empty);
        return CleanCityText(rawText);
    }

    /// <summary>
    /// Nettoie le texte brut "Villejoubert\n(16)" pour ne garder que le nom de ville.
    /// </summary>
    private static string CleanCityText(string rawText)
    {
        string firstLine = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
        return firstLine.Trim();
    }

    private static string? ExtractRelativeUrl(HtmlNode article)
    {
        HtmlNode? linkNode = article.SelectSingleNode(".//a[contains(@class, 'gc-caches-liste-link')]");
        string? href = linkNode?.GetAttributeValue("href", string.Empty);
        return href?.Trim();
    }

    private static string CombineUrl(string baseUrl, string relativePath)
    {
        return $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}
