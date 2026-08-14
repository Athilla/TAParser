using System.Text.Json;
using Microsoft.Extensions.Logging;
using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Charge et sauvegarde l'etat de reprise (parcours deja traites) dans un fichier JSON.
/// </summary>
public sealed class StateService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _stateFilePath;
    private readonly ILogger _logger;

    public StateService(string stateFilePath, ILogger logger)
    {
        _stateFilePath = stateFilePath;
        _logger = logger;
    }

    /// <summary>
    /// Charge l'etat depuis le disque. Retourne un etat vide si le fichier est absent ou corrompu.
    /// </summary>
    public async Task<ResumeState> LoadAsync()
    {
        if (!File.Exists(_stateFilePath))
        {
            _logger.LogInformation("Aucun fichier d'etat trouve ({StateFilePath}), demarrage a zero.", _stateFilePath);
            return new ResumeState();
        }

        try
        {
            await using FileStream stream = File.OpenRead(_stateFilePath);
            ResumeState? state = await JsonSerializer.DeserializeAsync<ResumeState>(stream);
            return state ?? new ResumeState();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Fichier d'etat corrompu ({StateFilePath}), redemarrage a zero.", _stateFilePath);
            return new ResumeState();
        }
    }

    /// <summary>
    /// Sauvegarde l'etat courant sur le disque (appele apres chaque parcours traite).
    /// </summary>
    public async Task SaveAsync(ResumeState state)
    {
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;

        string? directory = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = File.Create(_stateFilePath);
        await JsonSerializer.SerializeAsync(stream, state, SerializerOptions);
    }
}
