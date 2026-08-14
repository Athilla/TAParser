namespace TerraAvantura.Cli.Models;

/// <summary>
/// Reference legere a un parcours (issue du listing /parcours), avant l'exploration de ses commentaires.
/// </summary>
public sealed record ParcoursReference(int NodeId, string Title, string City, string Url);
