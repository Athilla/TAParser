using Microsoft.Extensions.Logging;
using TerraAvantura.Cli.Models;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Orchestre le parcours complet : connexion, decouverte des parcours, recherche de QR-code par parcours,
/// ecriture des resultats et sauvegarde de l'etat de reprise apres chaque parcours traite.
/// </summary>
public sealed class ParcoursRunner
{
    private readonly AuthService _authService;
    private readonly ParcoursService _parcoursService;
    private readonly CommentService _commentService;
    private readonly QrCommentMatcher _qrCommentMatcher;
    private readonly StateService _stateService;
    private readonly ResultWriterService _resultWriterService;
    private readonly ILogger _logger;

    public ParcoursRunner(
        AuthService authService,
        ParcoursService parcoursService,
        CommentService commentService,
        QrCommentMatcher qrCommentMatcher,
        StateService stateService,
        ResultWriterService resultWriterService,
        ILogger logger)
    {
        _authService = authService;
        _parcoursService = parcoursService;
        _commentService = commentService;
        _qrCommentMatcher = qrCommentMatcher;
        _stateService = stateService;
        _resultWriterService = resultWriterService;
        _logger = logger;
    }

    /// <summary>
    /// Execute le run complet. Retourne le code de sortie du processus (0 = succes, 1 = echec de connexion).
    /// </summary>
    public async Task<int> RunAsync(string baseUrl, string username, string password, int requestDelayMilliseconds)
    {
        if (!await _authService.LoginAsync(baseUrl, username, password))
        {
            _logger.LogError("Arret : connexion impossible aupres du site.");
            return 1;
        }

        ResumeState state = await _stateService.LoadAsync();
        IReadOnlyList<ParcoursReference> allParcours = await _parcoursService.GetAllParcoursAsync(baseUrl);
        IReadOnlyList<ParcoursReference> remainingParcours = FilterUnprocessed(allParcours, state);

        _logger.LogInformation(
            "Reprise : {ProcessedCount} parcours deja traites, {RemainingCount} restants sur {TotalCount}.",
            state.ProcessedParcoursIds.Count, remainingParcours.Count, allParcours.Count);

        int qrFoundCount = 0;
        foreach (ParcoursReference parcours in remainingParcours)
        {
            qrFoundCount += await ProcessOneParcoursAsync(parcours, state) ? 1 : 0;
            await _stateService.SaveAsync(state);
            await Task.Delay(requestDelayMilliseconds);
        }

        _logger.LogInformation(
            "Termine : {ProcessedCount} parcours traites, {QrFoundCount} QR-code(s) trouve(s).",
            allParcours.Count, qrFoundCount);

        return 0;
    }

    /// <summary>
    /// Traite un seul parcours : recherche un QR-code dans ses commentaires, ecrit le resultat si trouve,
    /// puis le marque comme traite dans l'etat (que le QR ait ete trouve ou non).
    /// </summary>
    private async Task<bool> ProcessOneParcoursAsync(ParcoursReference parcours, ResumeState state)
    {
        bool qrFound = false;

        try
        {
            IReadOnlyList<CommentInfo> comments = await _commentService.GetCommentsAsync(parcours.Url);
            QrMatch? match = await _qrCommentMatcher.FindFirstQrMatchAsync(comments);

            if (match is not null)
            {
                await _resultWriterService.AppendResultAsync(new ParcoursResult(parcours.City, match.DecodedText, parcours.Url, match.Comment.Text));
                _logger.LogInformation("QR-code trouve pour le parcours {Title} ({City}).", parcours.Title, parcours.City);
                qrFound = true;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Echec du traitement du parcours {Title} (node {NodeId}), passage au suivant.", parcours.Title, parcours.NodeId);
        }

        state.ProcessedParcoursIds.Add(parcours.NodeId);
        return qrFound;
    }

    private static IReadOnlyList<ParcoursReference> FilterUnprocessed(IReadOnlyList<ParcoursReference> allParcours, ResumeState state)
    {
        return allParcours.Where(p => !state.ProcessedParcoursIds.Contains(p.NodeId)).ToList();
    }
}
