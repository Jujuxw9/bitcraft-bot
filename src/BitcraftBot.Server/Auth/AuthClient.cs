using System.Net.Http;
using System.Text.RegularExpressions;

namespace BitcraftBot.Server.Auth;

// Connexion au compte BitCraft par code reçu par e-mail (même méthode que les outils communautaires).
// 1) demande d'un code pour l'adresse e-mail du compte ; 2) échange du code contre un jeton.
// Le jeton n'est jamais écrit dans le journal.
public sealed partial class AuthClient(HttpClient http)
{
    public async Task<AuthResult> RequestCodeAsync(string email, CancellationToken token)
    {
        var url = $"{ServerEndpoints.AuthBase}/request-access-code?email={Uri.EscapeDataString(email.Trim())}";
        using var response = await http.PostAsync(url, content: null, token);
        var body = await response.Content.ReadAsStringAsync(token);
        return response.IsSuccessStatusCode
            ? AuthResult.Ok(null)
            : AuthResult.Fail($"le serveur a répondu {(int)response.StatusCode} ({Shorten(body)})");
    }

    public async Task<AuthResult> AuthenticateAsync(string email, string code, CancellationToken token)
    {
        var url = $"{ServerEndpoints.AuthBase}/authenticate?email={Uri.EscapeDataString(email.Trim())}&accessCode={Uri.EscapeDataString(code.Trim())}";
        using var response = await http.PostAsync(url, content: null, token);
        var body = await response.Content.ReadAsStringAsync(token);
        if (!response.IsSuccessStatusCode)
        {
            return AuthResult.Fail($"le serveur a répondu {(int)response.StatusCode} ({Shorten(body)})");
        }
        var jwt = ExtractJwt(body);
        return jwt is null
            ? AuthResult.Fail("réponse reçue, mais aucun jeton dedans (format inattendu)")
            : AuthResult.Ok(jwt);
    }

    // La forme exacte de la réponse n'est pas documentée : on cherche le jeton (format JWT) où qu'il soit.
    public static string? ExtractJwt(string body)
    {
        var match = JwtPattern().Match(body);
        return match.Success ? match.Value : null;
    }

    // Une réponse d'erreur peut contenir n'importe quoi : on la raccourcit, et on masque un éventuel jeton.
    private static string Shorten(string body)
    {
        var text = JwtPattern().Replace(body, "[jeton masqué]").Replace('\n', ' ').Trim();
        return text.Length <= 160 ? text : text[..160] + "…";
    }

    [GeneratedRegex(@"eyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+")]
    private static partial Regex JwtPattern();
}

public sealed record AuthResult(bool Success, string? Token, string? Error)
{
    public static AuthResult Ok(string? token) => new(true, token, null);

    public static AuthResult Fail(string error) => new(false, null, error);
}
