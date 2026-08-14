using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace TerraAvantura.Cli.Services;

/// <summary>
/// Gere l'authentification aupres de terra-aventura.fr (formulaire Drupal "user_login_form")
/// et le maintien de la session via les cookies du HttpClient fourni.
/// </summary>
public sealed class AuthService
{
    private const string LoginFormIdMarker = "user_login_form";
    private const string LoggedInMarker = "user/logout";

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public AuthService(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Authentifie l'utilisateur aupres du site. Retourne true en cas de succes.
    /// </summary>
    public async Task<bool> LoginAsync(string baseUrl, string username, string password)
    {
        string loginPageHtml = await FetchLoginPageAsync(baseUrl);
        string formBuildId = ExtractFormBuildId(loginPageHtml);

        using FormUrlEncodedContent requestBody = BuildLoginRequestBody(username, password, formBuildId);
        HttpResponseMessage response = await _httpClient.PostAsync(baseUrl, requestBody);
        string responseHtml = await response.Content.ReadAsStringAsync();

        bool success = IsLoginSuccessful(responseHtml);
        LogLoginResult(success);

        return success;
    }

    /// <summary>
    /// Recupere la page d'accueil, qui contient le formulaire de connexion et son form_build_id.
    /// </summary>
    private async Task<string> FetchLoginPageAsync(string baseUrl)
    {
        return await _httpClient.GetStringAsync(baseUrl);
    }

    /// <summary>
    /// Extrait le form_build_id genere par Drupal pour le formulaire de connexion (user_login_form).
    /// </summary>
    private static string ExtractFormBuildId(string html)
    {
        HtmlDocument document = new();
        document.LoadHtml(html);

        HtmlNode? formIdNode = document.DocumentNode
            .SelectSingleNode($"//input[@name='form_id' and @value='{LoginFormIdMarker}']");

        HtmlNode? formNode = formIdNode?.SelectSingleNode("ancestor::form");
        HtmlNode? buildIdNode = formNode?.SelectSingleNode(".//input[@name='form_build_id']");

        return buildIdNode?.GetAttributeValue("value", string.Empty)
            ?? throw new InvalidOperationException("form_build_id introuvable : la structure du formulaire de connexion a peut-etre change.");
    }

    /// <summary>
    /// Construit le corps de la requete POST reproduisant une soumission classique (non-AJAX) du formulaire.
    /// </summary>
    private static FormUrlEncodedContent BuildLoginRequestBody(string username, string password, string formBuildId)
    {
        return new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = username,
            ["pass"] = password,
            ["form_build_id"] = formBuildId,
            ["form_id"] = LoginFormIdMarker,
            ["op"] = "Se connecter",
        });
    }

    /// <summary>
    /// Determine si la connexion a reussi en recherchant le lien de deconnexion dans la reponse.
    /// </summary>
    private static bool IsLoginSuccessful(string responseHtml)
    {
        return responseHtml.Contains(LoggedInMarker, StringComparison.OrdinalIgnoreCase);
    }

    private void LogLoginResult(bool success)
    {
        if (success)
        {
            _logger.LogInformation("Connexion reussie aupres du site.");
        }
        else
        {
            _logger.LogError("Echec de connexion : identifiants invalides ou structure du site modifiee.");
        }
    }
}
