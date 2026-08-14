using Microsoft.Extensions.Logging.Abstractions;
using TerraAvantura.Cli.Models;
using TerraAvantura.Cli.Services;

namespace TerraAvantura.Cli.Tests;

/// <summary>
/// Verifie le chargement/sauvegarde de l'etat de reprise, y compris les cas absent/corrompu.
/// </summary>
public class StateServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public StateServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "terra-avantura-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyState_WhenFileDoesNotExist()
    {
        string statePath = Path.Combine(_tempDirectory, "missing-state.json");
        StateService stateService = new(statePath, NullLogger.Instance);

        ResumeState state = await stateService.LoadAsync();

        Assert.Empty(state.ProcessedParcoursIds);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsProcessedParcoursIds()
    {
        string statePath = Path.Combine(_tempDirectory, "state.json");
        StateService stateService = new(statePath, NullLogger.Instance);
        ResumeState original = new();
        original.ProcessedParcoursIds.Add(47);
        original.ProcessedParcoursIds.Add(48);

        await stateService.SaveAsync(original);
        ResumeState reloaded = await stateService.LoadAsync();

        Assert.Equal(new HashSet<int> { 47, 48 }, reloaded.ProcessedParcoursIds);
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyState_WhenFileIsCorrupt()
    {
        string statePath = Path.Combine(_tempDirectory, "corrupt-state.json");
        await File.WriteAllTextAsync(statePath, "{ ceci n'est pas du json valide");
        StateService stateService = new(statePath, NullLogger.Instance);

        ResumeState state = await stateService.LoadAsync();

        Assert.Empty(state.ProcessedParcoursIds);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
