using TerraAvantura.Cli.Services;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie l'extraction des commentaires (texte + image) depuis une page de parcours (fixture HTML sauvegardee).
/// </summary>
public class CommentServiceTests
{
    [Fact]
    public void ExtractComments_ParsesEachCommentFromFixture()
    {
        string html = ReadFixture("comments-listing.html");

        var comments = CommentService.ExtractComments(html);

        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public void ExtractComments_CommentWithoutPictureHasNullImageUrl()
    {
        string html = ReadFixture("comments-listing.html");

        var comments = CommentService.ExtractComments(html);

        Assert.Contains(comments, c => c.Text.Contains("balade sympa") && c.ImageUrl is null);
    }

    [Fact]
    public void ExtractComments_CommentWithPictureHasImageUrl()
    {
        string html = ReadFixture("comments-listing.html");

        var comments = CommentService.ExtractComments(html);
        var withPicture = comments.Single(c => c.ImageUrl is not null);

        Assert.Contains("QR-code", withPicture.Text);
        Assert.StartsWith("https://cdn.terra-aventura.fr/", withPicture.ImageUrl);
    }

    [Fact]
    public void ExtractComments_ReturnsEmptyListWhenNoCommentsPresent()
    {
        var comments = CommentService.ExtractComments("<html><body>Aucun commentaire</body></html>");

        Assert.Empty(comments);
    }

    private static string ReadFixture(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        return File.ReadAllText(path);
    }
}
