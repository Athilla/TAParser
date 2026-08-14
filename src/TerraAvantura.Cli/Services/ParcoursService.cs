using System.Text.Json.Serialization;
using HtmlAgilityPack;
using System.Text.Json;
using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Recupere la liste complete des parcours (ville + lien) depuis la page /parcours.
/// La liste visible dans le navigateur est generee cote client par JavaScript ; elle n'existe jamais
/// dans le HTML brut. Les donnees reelles (nid, titre, ville) sont en revanche presentes des le depart,
/// embarquees par Drupal dans le bloc "drupalSettings.geocaching_map.markers" (script JSON standard).
/// </summary>
public sealed class ParcoursService
{
    private const string ParcoursListPath = "/parcours";
    private const string CacheMarkerType = "geocaching_cache";
    private const string DrupalSettingsSelector = "//script[@type='application/json' and @data-drupal-selector='drupal-settings-json']";

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
    /// Extrait chaque parcours a partir du JSON "drupalSettings" embarque dans la page.
    /// </summary>
    internal static IReadOnlyList<ParcoursReference> ExtractParcours(string html, string baseUrl)
    {
        string? settingsJson = ExtractDrupalSettingsJson(html);
        if (settingsJson is null)
        {
            return Array.Empty<ParcoursReference>();
        }

        IReadOnlyList<GeocachingMarker> markers = ParseMarkers(settingsJson);

        return markers
            .Where(marker => marker.Type == CacheMarkerType)
            .Select(marker => ToParcoursReference(marker, baseUrl))
            .Where(parcours => parcours is not null)
            .Select(parcours => parcours!)
            .ToList();
    }

    /// <summary>
    /// Extrait le contenu brut du script "drupal-settings-json" contenant toutes les donnees de la carte.
    /// </summary>
    private static string? ExtractDrupalSettingsJson(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNode? scriptNode = document.DocumentNode.SelectSingleNode(DrupalSettingsSelector);
        return scriptNode?.InnerHtml;
    }

    /// <summary>
    /// Parse le JSON des reglages Drupal et retourne la liste des marqueurs de la carte (parcours + partenaires).
    /// </summary>
    private static IReadOnlyList<GeocachingMarker> ParseMarkers(string settingsJson)
    {
        DrupalSettingsRoot? settings = JsonSerializer.Deserialize<DrupalSettingsRoot>(settingsJson);
        return (IReadOnlyList<GeocachingMarker>?)settings?.GeocachingMap?.Markers ?? Array.Empty<GeocachingMarker>();
    }

    /// <summary>
    /// Convertit un marqueur "geocaching_cache" en reference de parcours ; ignore les entrees sans identifiant valide.
    /// L'URL utilise /node/{id}, que Drupal redirige automatiquement vers l'alias canonique (/caches/...).
    /// </summary>
    private static ParcoursReference? ToParcoursReference(GeocachingMarker marker, string baseUrl)
    {
        if (!int.TryParse(marker.NodeId, out int nodeId))
        {
            return null;
        }

        return new ParcoursReference(nodeId, marker.Title, marker.City ?? string.Empty, CombineUrl(baseUrl, $"/node/{nodeId}"));
    }

    private static string CombineUrl(string baseUrl, string relativePath)
    {
        return $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }

    /// <summary>
    /// Racine minimale du JSON "drupalSettings" : seul le bloc "geocaching_map" nous interesse.
    /// </summary>
    private sealed record DrupalSettingsRoot(
        [property: JsonPropertyName("geocaching_map")] GeocachingMapSettings? GeocachingMap);

    /// <summary>
    /// Bloc "geocaching_map" : contient la liste des marqueurs affiches sur la carte des parcours.
    /// </summary>
    private sealed record GeocachingMapSettings(
        [property: JsonPropertyName("markers")] List<GeocachingMarker>? Markers);

    /// <summary>
    /// Un marqueur de la carte : soit un parcours ("geocaching_cache"), soit un lieu partenaire ("partners_places").
    /// </summary>
    private sealed record GeocachingMarker(
        [property: JsonPropertyName("nid")] string NodeId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("field_city")] string? City);
}
