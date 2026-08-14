using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Ajoute chaque resultat trouve (ville, code, lien, commentaire) au fichier cumulatif de resultats.
/// </summary>
public sealed class ResultWriterService
{
    private readonly string _resultsFilePath;

    public ResultWriterService(string resultsFilePath)
    {
        _resultsFilePath = resultsFilePath;
    }

    /// <summary>
    /// Ajoute un resultat au fichier cumulatif. Cree le fichier et son dossier s'ils n'existent pas.
    /// </summary>
    public async Task AppendResultAsync(ParcoursResult result)
    {
        string? directory = Path.GetDirectoryName(_resultsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string block = FormatResultBlock(result);
        await File.AppendAllTextAsync(_resultsFilePath, block);
    }

    /// <summary>
    /// Met en forme un resultat en un bloc texte lisible.
    /// </summary>
    private static string FormatResultBlock(ParcoursResult result)
    {
        return $"""
            Ville : {result.City}
            Code : {result.Code}
            Lien : {result.ParcoursUrl}
            Commentaire : {result.CommentText}
            ---

            """;
    }
}
