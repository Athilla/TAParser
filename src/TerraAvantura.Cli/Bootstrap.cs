using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TerraAvantura.Cli.Configuration;

namespace TerraAvantura.Cli;

/// <summary>
/// Regroupe les etapes de demarrage du CLI : configuration, logging, message initial.
/// Chaque methode reste courte et mono-responsabilite.
/// </summary>
public static class Bootstrap
{
    private const string ConfigurationSectionName = "TerraAvantura";

    /// <summary>
    /// Construit la configuration a partir de appsettings.json et d'un override local optionnel.
    /// </summary>
    public static IConfiguration BuildConfiguration(string[] args)
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    /// <summary>
    /// Lie la section "TerraAvantura" de la configuration vers un objet AppSettings type.
    /// </summary>
    public static AppSettings BindAppSettings(IConfiguration configuration)
    {
        return configuration.GetSection(ConfigurationSectionName).Get<AppSettings>()
            ?? throw new InvalidOperationException($"Section de configuration manquante : {ConfigurationSectionName}");
    }

    /// <summary>
    /// Cree une fabrique de logger ecrivant a la fois sur la console et dans un fichier de log.
    /// </summary>
    public static ILoggerFactory CreateLoggerFactory(AppSettings settings)
    {
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
        });

        loggerFactory.AddFile(settings.LogFilePath);

        return loggerFactory;
    }

    /// <summary>
    /// Journalise un message de demarrage sans exposer les identifiants sensibles.
    /// </summary>
    public static void LogStartup(ILogger logger, AppSettings settings)
    {
        logger.LogInformation(
            "Terra-Avantura CLI demarre. Site cible : {BaseUrl}. Fichier de resultats : {ResultsFilePath}. Fichier d'etat : {StateFilePath}.",
            settings.BaseUrl,
            settings.ResultsFilePath,
            settings.StateFilePath);
    }
}
