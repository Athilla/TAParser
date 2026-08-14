using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using TerraAvantura.Cli;
using TerraAvantura.Cli.Configuration;
using TerraAvantura.Cli.Services;

// Point d'entree du CLI : delegue la construction de la configuration et du logger a Bootstrap,
// puis affiche un message de demarrage pour valider que tout est correctement cable.
IConfiguration configuration = Bootstrap.BuildConfiguration(args);
AppSettings settings = Bootstrap.BindAppSettings(configuration);
using ILoggerFactory loggerFactory = Bootstrap.CreateLoggerFactory(settings);
ILogger logger = loggerFactory.CreateLogger("TerraAvantura");

Bootstrap.LogStartup(logger, settings);

using HttpClient httpClient = CreateHttpClient();
ParcoursRunner runner = CreateRunner(httpClient, settings, logger);

return await runner.RunAsync(settings.BaseUrl, settings.Username, settings.Password, settings.RequestDelayMilliseconds);

/// <summary>
/// Cree le HttpClient partage par tous les services, avec un conteneur de cookies pour maintenir la session.
/// </summary>
static HttpClient CreateHttpClient()
{
    HttpClientHandler handler = new() { UseCookies = true, CookieContainer = new CookieContainer() };
    HttpClient client = new(handler);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; TerraAvanturaCli/1.0)");
    return client;
}

/// <summary>
/// Assemble le ParcoursRunner et l'ensemble de ses dependances.
/// </summary>
static ParcoursRunner CreateRunner(HttpClient httpClient, AppSettings settings, ILogger logger)
{
    AuthService authService = new(httpClient, logger);
    ParcoursService parcoursService = new(httpClient);
    CommentService commentService = new(httpClient);
    QrCommentMatcher qrCommentMatcher = new(new QrDecoderService(httpClient));
    StateService stateService = new(settings.StateFilePath, logger);
    ResultWriterService resultWriterService = new(settings.ResultsFilePath);

    return new ParcoursRunner(authService, parcoursService, commentService, qrCommentMatcher, stateService, resultWriterService, logger);
}
