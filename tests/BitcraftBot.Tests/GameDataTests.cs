using System.Net;
using System.Text;
using BitcraftBot.GameData;

namespace BitcraftBot.Tests;

// D-033 : données du jeu lues depuis BitCraft_GameData. Tests sans réseau, avec de petits fichiers fabriqués
// (on ne recopie pas les vrais fichiers du dépôt, qui n'a pas de licence).
public class GameDataTests
{
    // Petit jeu de données au format du dépôt (mêmes noms de champs), assez grand pour passer les contrôles.
    internal static Dictionary<string, string> FakeTables(int oakHealth = 12000)
    {
        var resources = new StringBuilder("[");
        resources.Append($"{{\"id\":21,\"name\":\"Mature Oak Tree\",\"max_health\":{oakHealth},\"tier\":1,\"tag\":\"Tree\"}},");
        resources.Append("{\"id\":22,\"name\":\"Young Oak Tree\",\"max_health\":3000,\"tier\":1,\"tag\":\"Tree\"}");
        for (var i = 100; i < 450; i++)
        {
            resources.Append($",{{\"id\":{i},\"name\":\"Resource {i}\",\"max_health\":100,\"tier\":1,\"tag\":\"Rock\"}}");
        }
        resources.Append(']');
        var items = "[" + string.Join(",", Enumerable.Range(1, 600).Select(i => $"{{\"id\":{i},\"name\":\"Item {i}\"}}")) + "]";
        return new Dictionary<string, string>
        {
            ["resource_desc"] = resources.ToString(),
            ["extraction_recipe_desc"] = "[{\"id\":21,\"resource_id\":21,\"time_requirement\":1.6,\"stamina_requirement\":0.75," +
                                         "\"tool_requirements\":[{\"tool_type\":1,\"level\":1,\"power\":1}],\"level_requirements\":[{\"skill_id\":2,\"level\":1}]}]",
            ["biome_desc"] = "[{\"name\":\"Safe Meadows\"},{\"name\":\"Calm Forest\"}]",
            ["item_desc"] = items,
            ["cargo_desc"] = "[{\"id\":1000,\"name\":\"Rough Wood Trunk\"}]",
            ["food_desc"] = "[{\"item_id\":10000}]",
            ["tool_type_desc"] = "[{\"id\":1,\"name\":\"Axe\",\"skill_id\":2}]",
            ["skill_desc"] = "[{\"id\":2,\"name\":\"Forestry\"}]",
            ["crafting_recipe_desc"] = "[]",
            ["building_type_desc"] = "[]",
            ["buff_desc"] = "[]",
            ["buff_type_desc"] = "[]",
        };
    }

    internal static string WriteTables(Dictionary<string, string> tables)
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraftbot-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        foreach (var (name, json) in tables)
        {
            File.WriteAllText(Path.Combine(folder, name + ".json"), json);
        }
        return folder;
    }

    [Fact]
    public void LoadsAndValidatesTables()
    {
        var store = GameDataStore.Load(WriteTables(FakeTables()));

        Assert.Empty(store.Validate());
        var oak = store.FindResource("mature oak tree");
        Assert.NotNull(oak);
        Assert.Equal(12000, oak!.MaxHealth);
        var extraction = store.Extractions[oak.Id];
        Assert.Equal(1, extraction.ToolType);
        Assert.Equal(2, extraction.SkillId);
        Assert.Equal(0.75, extraction.StaminaPerAction);
        Assert.Equal("Rough Wood Trunk", store.Cargos[1000]);
        Assert.Contains("Mature Oak Tree", store.ResourceNames());
        // Les noms vus à la main restent connus.
        Assert.Contains("Bush", store.ResourceNames());
    }

    // Données en désaccord avec l'écran : refusées.
    [Fact]
    public void RejectsDataThatContradictsTheScreen()
    {
        var store = GameDataStore.Load(WriteTables(FakeTables(oakHealth: 999)));
        Assert.Contains(store.Validate(), p => p.Contains("Mature Oak Tree"));
    }

    [Fact]
    public void RejectsTruncatedData()
    {
        var tables = FakeTables();
        tables["resource_desc"] = "[{\"id\":21,\"name\":\"Mature Oak Tree\",\"max_health\":12000}]";
        Assert.Contains(GameDataStore.Load(WriteTables(tables)).Validate(), p => p.Contains("trop peu de ressources"));
    }

    [Fact]
    public async Task DownloadsOnlyWhenCommitChanges()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraftbot-tests", Guid.NewGuid().ToString("N"));
        var handler = new FakeGitHub("abc1234", FakeTables());
        var updater = new GameDataUpdater(new HttpClient(handler));

        var first = await updater.UpdateAsync(folder, CancellationToken.None);
        Assert.True(first.Changed, first.Message);
        Assert.Equal("abc1234", GameDataStore.ReadVersion(folder)!.Commit);
        var downloads = handler.TableRequests;

        var second = await updater.UpdateAsync(folder, CancellationToken.None);
        Assert.False(second.Changed);
        Assert.Equal(downloads, handler.TableRequests);
        // Le dossier temporaire ne reste pas.
        Assert.False(Directory.Exists(Path.Combine(folder, ".telechargement")));
    }

    // Une nouvelle version incohérente ne remplace pas l'ancienne.
    [Fact]
    public async Task KeepsOldVersionWhenNewOneIsBad()
    {
        var folder = Path.Combine(Path.GetTempPath(), "bitcraftbot-tests", Guid.NewGuid().ToString("N"));
        await new GameDataUpdater(new HttpClient(new FakeGitHub("v1", FakeTables()))).UpdateAsync(folder, CancellationToken.None);

        var result = await new GameDataUpdater(new HttpClient(new FakeGitHub("v2", FakeTables(oakHealth: 5)))).UpdateAsync(folder, CancellationToken.None);

        Assert.False(result.Changed);
        Assert.Contains("refusée", result.Message);
        Assert.Equal("v1", GameDataStore.ReadVersion(folder)!.Commit);
        Assert.Equal(12000, GameDataStore.Load(folder).FindResource("Mature Oak Tree")!.MaxHealth);
    }

    // Faux GitHub : répond au « dernier commit » et aux fichiers des tables.
    private sealed class FakeGitHub(string commit, Dictionary<string, string> tables) : HttpMessageHandler
    {
        public int TableRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            string body;
            if (url.StartsWith("https://api.github.com/", StringComparison.Ordinal))
            {
                body = $"{{\"sha\":\"{commit}\",\"commit\":{{\"committer\":{{\"date\":\"2026-11-16T13:59:31Z\"}}}}}}";
            }
            else
            {
                Assert.Contains($"/{commit}/static/", url);
                TableRequests++;
                body = tables[Path.GetFileNameWithoutExtension(new Uri(url).AbsolutePath)];
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
