namespace TerraAvantura.Cli.Models;

/// <summary>
/// Etat persistant permettant de reprendre le traitement des parcours apres une interruption.
/// </summary>
public sealed class ResumeState
{
    /// <summary>
    /// Identifiants (NodeId) des parcours deja traites (avec ou sans QR-code trouve).
    /// </summary>
    public HashSet<int> ProcessedParcoursIds { get; init; } = new();

    /// <summary>
    /// Date/heure de la derniere sauvegarde de l'etat.
    /// </summary>
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
