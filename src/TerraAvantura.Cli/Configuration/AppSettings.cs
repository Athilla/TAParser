namespace TerraAvantura.Cli.Configuration;

/// <summary>
/// Modele de configuration fortement type, lie a la section "TerraAvantura" de appsettings.json.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// URL de base du site terra-avantura.com.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Identifiant de connexion au site.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Mot de passe de connexion au site.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Chemin du fichier cumulatif de resultats (ville, code, lien, commentaire).
    /// </summary>
    public string ResultsFilePath { get; init; } = "results.txt";

    /// <summary>
    /// Chemin du fichier d'etat permettant la reprise apres interruption.
    /// </summary>
    public string StateFilePath { get; init; } = "state.json";

    /// <summary>
    /// Chemin du fichier de log.
    /// </summary>
    public string LogFilePath { get; init; } = "logs/terra-avantura-{Date}.log";

    /// <summary>
    /// Delai entre deux requetes HTTP, pour eviter d'etre bloque par le site.
    /// </summary>
    public int RequestDelayMilliseconds { get; init; } = 500;
}
