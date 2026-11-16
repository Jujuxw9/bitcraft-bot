using System.Text.Json;

namespace BitcraftBot.GameData;

// Résultat d'une mise à jour des données du jeu, pour le journal.
public sealed record GameDataUpdateResult(bool Changed, GameDataVersion? Version, string Message);

// Télécharge les tables utiles de BitCraft_GameData (D-033) dans un dossier local, à côté du .exe.
// - Le dépôt est mis à jour chaque jour par ses auteurs : on ne télécharge que si le commit a changé.
// - Les fichiers sont d'abord mis dans un dossier temporaire, vérifiés (GameDataStore.Validate),
//   puis seulement remplacent l'ancienne version. En cas de problème, l'ancienne version est gardée.
// - Données publiques sur GitHub : aucune connexion au serveur du jeu, rien n'est republié.
public sealed class GameDataUpdater(HttpClient http)
{
    public const string Repository = "BitCraftToolBox/BitCraft_GameData";
    public const string Branch = "cereal/cs";

    // Nom générique : on n'envoie rien qui identifie Julien.
    public const string UserAgent = "BitcraftBot-donnees";

    private const string TempFolderName = ".telechargement";

    public async Task<GameDataUpdateResult> UpdateAsync(string folder, CancellationToken token)
    {
        Directory.CreateDirectory(folder);
        var current = GameDataStore.ReadVersion(folder);

        var (commit, updatedAt) = await GetLatestCommitAsync(token);
        if (current is not null && current.Commit == commit && AllTablesPresent(folder))
        {
            return new GameDataUpdateResult(false, current, "déjà à jour");
        }

        var temp = Path.Combine(folder, TempFolderName);
        if (Directory.Exists(temp))
        {
            Directory.Delete(temp, recursive: true);
        }
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var table in GameDataStore.Tables)
            {
                // Adresse figée sur le commit : toutes les tables viennent de la même mise à jour.
                var url = $"https://raw.githubusercontent.com/{Repository}/{commit}/static/{table}.json";
                var bytes = await http.GetByteArrayAsync(url, token);
                await File.WriteAllBytesAsync(Path.Combine(temp, table + ".json"), bytes, token);
            }

            var problems = GameDataStore.Load(temp).Validate();
            if (problems.Count > 0)
            {
                return new GameDataUpdateResult(false, current,
                    $"nouvelle version refusée ({string.Join(" ; ", problems)}), ancienne version gardée");
            }

            foreach (var table in GameDataStore.Tables)
            {
                File.Move(Path.Combine(temp, table + ".json"), Path.Combine(folder, table + ".json"), overwrite: true);
            }
            var version = new GameDataVersion(commit, updatedAt, DateTimeOffset.Now);
            GameDataStore.WriteVersion(folder, version);
            return new GameDataUpdateResult(true, version, "nouvelle version téléchargée et vérifiée");
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    public static bool AllTablesPresent(string folder) =>
        GameDataStore.Tables.All(t => File.Exists(Path.Combine(folder, t + ".json")));

    // Dernier commit de la branche : identifiant et date (API publique GitHub, sans compte).
    private async Task<(string Commit, DateTimeOffset UpdatedAt)> GetLatestCommitAsync(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/commits/{Uri.EscapeDataString(Branch)}");
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await http.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var root = document.RootElement;
        var sha = root.GetProperty("sha").GetString() ?? throw new InvalidDataException("Commit sans identifiant.");
        var date = root.GetProperty("commit").GetProperty("committer").GetProperty("date").GetDateTimeOffset();
        return (sha, date);
    }
}
