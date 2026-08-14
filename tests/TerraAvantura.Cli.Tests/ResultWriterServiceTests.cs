using TerraAvantura.Cli.Models;
using TerraAvantura.Cli.Services;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie que les resultats sont ajoutes (append) au fichier cumulatif avec tous les champs attendus.
/// </summary>
public class ResultWriterServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public ResultWriterServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "terra-avantura-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task AppendResultAsync_WritesAllFields()
    {
        string resultsPath = Path.Combine(_tempDirectory, "results.txt");
        ResultWriterService writer = new(resultsPath);
        ParcoursResult result = new("Villejoubert", "MOT-SECRET", "https://www.terra-aventura.fr/caches/genie-de-macarine", "Un joli QR-code dans la boite !");

        await writer.AppendResultAsync(result);
        string content = await File.ReadAllTextAsync(resultsPath);

        Assert.Contains("Villejoubert", content);
        Assert.Contains("MOT-SECRET", content);
        Assert.Contains("https://www.terra-aventura.fr/caches/genie-de-macarine", content);
        Assert.Contains("Un joli QR-code dans la boite !", content);
    }

    [Fact]
    public async Task AppendResultAsync_AppendsWithoutOverwritingPreviousResults()
    {
        string resultsPath = Path.Combine(_tempDirectory, "results.txt");
        ResultWriterService writer = new(resultsPath);

        await writer.AppendResultAsync(new ParcoursResult("Ville A", "CODE-A", "https://example.test/a", "Commentaire A"));
        await writer.AppendResultAsync(new ParcoursResult("Ville B", "CODE-B", "https://example.test/b", "Commentaire B"));
        string content = await File.ReadAllTextAsync(resultsPath);

        Assert.Contains("CODE-A", content);
        Assert.Contains("CODE-B", content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
