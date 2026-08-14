using TerraAvantura.Cli.Services;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie l'extraction des parcours depuis le JSON "drupalSettings.geocaching_map.markers"
/// embarque dans la page /parcours (fixture HTML sauvegardee).
/// </summary>
public class ParcoursServiceTests
{
    private const string BaseUrl = "https://www.terra-aventura.fr";

    [Fact]
    public void ExtractParcours_ParsesOnlyGeocachingCacheMarkers()
    {
        string html = ReadFixture("parcours-listing.html");

        var parcours = ParcoursService.ExtractParcours(html, BaseUrl);

        Assert.Equal(3, parcours.Count);
    }

    [Fact]
    public void ExtractParcours_ExtractsTitleCityAndNodeBasedUrl()
    {
        string html = ReadFixture("parcours-listing.html");

        var parcours = ParcoursService.ExtractParcours(html, BaseUrl);
        var first = parcours.Single(p => p.NodeId == 47);

        Assert.Equal("Le génie de la Macarine", first.Title);
        Assert.Equal("Villejoubert", first.City);
        Assert.Equal("https://www.terra-aventura.fr/node/47", first.Url);
    }

    [Fact]
    public void ExtractParcours_ExcludesPartnersPlacesMarkers()
    {
        string html = ReadFixture("parcours-listing.html");

        var parcours = ParcoursService.ExtractParcours(html, BaseUrl);

        Assert.DoesNotContain(parcours, p => p.NodeId == 2820);
    }

    [Fact]
    public void ExtractParcours_ReturnsEmptyListWhenSettingsScriptIsMissing()
    {
        var parcours = ParcoursService.ExtractParcours("<html><body>Rien ici</body></html>", BaseUrl);

        Assert.Empty(parcours);
    }

    private static string ReadFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
    }
}


