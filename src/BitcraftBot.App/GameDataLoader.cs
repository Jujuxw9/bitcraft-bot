using System.IO;
using System.Net.Http;
using BitcraftBot.Core.Logging;
using BitcraftBot.GameData;

namespace BitcraftBot.App;

// Au lancement du bot : charge les données du jeu déjà présentes (dossier « donnees-jeu » à côté du .exe),
// puis vérifie en arrière-plan s'il existe une version plus récente de BitCraft_GameData (D-033).
// Sans Internet, le bot continue avec la version déjà là, ou avec la petite liste de noms intégrée.
public sealed class GameDataLoader(SessionLog log)
{
    // Au-delà, on signale que les données ne suivent peut-être plus le jeu (patch récent).
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    public static string Folder => Path.Combine(AppContext.BaseDirectory, "donnees-jeu");

    public GameDataStore? Current { get; private set; }

    // Appelé à chaque nouvelle version chargée (pour donner les noms au lecteur du HUD, etc.).
    public event Action<GameDataStore>? Loaded;

    public async Task StartAsync()
    {
        if (GameDataUpdater.AllTablesPresent(Folder))
        {
            TryLoad("version déjà présente");
        }
        else
        {
            log.Info("Données", "Aucune donnée du jeu sur ce PC : premier téléchargement.");
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(GameDataUpdater.UserAgent);
            var result = await Task.Run(() => new GameDataUpdater(http).UpdateAsync(Folder, CancellationToken.None));
            if (result.Changed)
            {
                TryLoad(result.Message);
            }
            else if (result.Message.StartsWith("nouvelle version refusée", StringComparison.Ordinal))
            {
                log.Alerte("Données", $"Données du jeu : {result.Message}.");
            }
            else
            {
                log.Info("Données", $"Données du jeu {result.Message} ({Describe(result.Version)}).");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            log.Alerte("Données", $"Mise à jour des données du jeu impossible ({ex.Message}). " +
                                  (Current is null ? "Le bot utilise sa petite liste de noms intégrée." : $"Le bot garde la version présente ({Describe(Current.Version)})."));
        }

        if (Current?.Version is { } version && DateTimeOffset.Now - version.UpdatedAt > MaxAge)
        {
            log.Alerte("Données", $"Données du jeu vieilles de {(DateTimeOffset.Now - version.UpdatedAt).TotalDays:0} jours : elles peuvent ne plus correspondre au jeu après un patch.");
        }
    }

    private void TryLoad(string reason)
    {
        try
        {
            var store = GameDataStore.Load(Folder);
            var problems = store.Validate();
            if (problems.Count > 0)
            {
                log.Alerte("Données", $"Données du jeu présentes mais incohérentes ({string.Join(" ; ", problems)}) : non utilisées.");
                return;
            }
            Current = store;
            log.Info("Données", $"Données du jeu chargées, {reason} ({Describe(store.Version)}) : {store.Resources.Count} ressources, " +
                                $"{store.Items.Count} objets, {store.Biomes.Count} biomes. Source : BitCraft_GameData, jamais republiée.");
            Loaded?.Invoke(store);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            log.Alerte("Données", $"Lecture des données du jeu impossible : {ex.Message}");
        }
    }

    private static string Describe(GameDataVersion? version) =>
        version is null ? "version inconnue" : $"mise à jour du {version.UpdatedAt.LocalDateTime:yyyy-MM-dd HH:mm}, commit {version.Commit[..Math.Min(7, version.Commit.Length)]}";
}
