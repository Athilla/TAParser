namespace TerraAvantura.Cli.Models;

/// <summary>
/// Resultat final pour un parcours : la ville, le code QR decode, le lien vers le parcours et le commentaire source.
/// </summary>
public sealed record ParcoursResult(string City, string Code, string ParcoursUrl, string CommentText);
