using TerraAvantura.Cli.Services;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie l'extraction des parcours depuis une page /parcours (fixture HTML sauvegardee).
/// </summary>
public class ParcoursServiceTests
{
    private const string BaseUrl = "https://www.terra-aventura.fr";

    [Fact]
    public void ExtractParcours_ParsesEachArticleFromFixture()
    {
        string html = ReadFixture("parcours-listing.html");

        var parcours = ParcoursService.ExtractParcours(html, BaseUrl);

        Assert.Equal(4, parcours.Count);
    }

    [Fact]
    public void ExtractParcours_ExtractsTitleCityAndUrl()
    {
        string html = ReadFixture("parcours-listing.html");

        var parcours = ParcoursService.ExtractParcours(html, BaseUrl);
        var first = parcours.Single(p => p.NodeId == 47);

        Assert.Equal("Le génie de la Macarine", first.Title);
        Assert.Equal("Villejoubert", first.City);
        Assert.Equal("https://www.terra-aventura.fr/caches/genie-de-macarine", first.Url);
    }

    [Fact]
    public void ExtractParcours_TrimsLeadingSpaceInLinkHref()
    {
        string html = ReadFixture("parcours-listing.html");

        var parcours = ParcoursService.ExtractParcours(html, BaseUrl);

        Assert.All(parcours, p => Assert.DoesNotContain(" ", p.Url));
    }

    [Fact]
    public void ExtractParcours_ReturnsEmptyListWhenNoArticlesPresent()
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
