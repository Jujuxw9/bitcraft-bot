using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BitcraftBot.Server.Auth;

// Garde le jeton sur le PC, chiffré par Windows (DPAPI) : seul le compte Windows de Julien peut le relire.
// Emplacement : %LOCALAPPDATA%\BitcraftBot\jeton.bin, hors du dépôt Git et hors du dossier du .exe.
public sealed class TokenStore(string path)
{
    // Ajouté au chiffrement : un autre programme qui utiliserait DPAPI ne peut pas relire ce fichier par erreur.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BitcraftBot.Server.Token.v1");

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BitcraftBot", "jeton.bin");

    public string FilePath => path;

    public bool Exists => File.Exists(path);

    public void Save(string token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, data);
    }

    public string? Load()
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            var data = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch (CryptographicException)
        {
            // Fichier abîmé ou créé par un autre compte Windows : on le considère comme absent.
            return null;
        }
    }

    public void Delete()
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

// Ce qu'on peut lire dans un jeton sans le secret : l'identité du compte et les dates. Rien n'est vérifié ici.
public sealed record TokenInfo(string? HexIdentity, DateTimeOffset? IssuedAt, DateTimeOffset? ExpiresAt)
{
    public static TokenInfo? TryRead(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }
        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            var root = doc.RootElement;
            string? identity = root.TryGetProperty("hex_identity", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null;
            return new TokenInfo(identity, ReadDate(root, "iat"), ReadDate(root, "exp"));
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }

    // Identité raccourcie pour l'affichage : « c200…b435 ».
    public static string ShortIdentity(string? hex) =>
        hex is { Length: > 12 } ? $"{hex[..4]}…{hex[^4..]}" : hex ?? "?";

    private static DateTimeOffset? ReadDate(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;
}
