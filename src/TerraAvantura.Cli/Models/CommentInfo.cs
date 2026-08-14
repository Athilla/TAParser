namespace TerraAvantura.Cli.Models;

/// <summary>
/// Represente un commentaire d'un parcours, avec son texte et l'URL de sa photo eventuelle.
/// </summary>
public sealed record CommentInfo(string Text, string? ImageUrl);
