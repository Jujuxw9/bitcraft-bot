using System.Text.Json;

namespace BitcraftBot.GameData;

// Un objet ou une cargaison du jeu (item_desc / cargo_desc).
// Icon : chemin de l'image du jeu (icon_asset_name), ex. « Items/CookedBerries ».
public sealed record GameEntry(int Id, bool IsCargo, string Name, string Description, int Tier, string Tag, string Rarity, string Icon = "")
{
    public string Key => (IsCargo ? "C" : "I") + Id;
}

// Une quantité d'un objet (Probability : chance d'obtention, 1 = toujours).
public sealed record EntryAmount(string Key, int Quantity, double Probability = 1);

// Un besoin : métier ou outil, avec son niveau.
public sealed record Requirement(string Name, int Level);

// Une façon d'obtenir un objet : fabrication (atelier) ou récolte (ressource du monde).
// Kind : « Fabrication » ou « Récolte ». Place : atelier (et palier) ou ressource à récolter.
public sealed record Recipe(
    string Kind,
    string Name,
    string Place,
    IReadOnlyList<Requirement> Skills,
    IReadOnlyList<Requirement> Tools,
    IReadOnlyList<EntryAmount> Inputs,
    IReadOnlyList<EntryAmount> Outputs,
    int Actions,
    double SecondsPerAction,
    double StaminaPerAction,
    bool NeedsKnowledge,
    bool NeedsClaimTech,
    bool HandsAllowed);

// Livre des recettes : pour chaque objet, comment l'obtenir et à quoi il sert.
// Données publiques BitCraft_GameData (D-033), lues sur le disque, jamais republiées.
public sealed class RecipeBook
{
    public static IReadOnlyList<string> Tables { get; } = ["crafting_recipe_desc", "building_type_desc"];

    public required IReadOnlyDictionary<string, GameEntry> Entries { get; init; }
    public required IReadOnlyList<Recipe> Recipes { get; init; }

    private Dictionary<string, List<Recipe>>? _producing;
    private Dictionary<string, List<Recipe>>? _using;

    public static bool CanLoad(string folder) =>
        GameDataStore.Tables.Concat(Tables).All(t => File.Exists(Path.Combine(folder, t + ".json")));

    // Recherche par morceau de nom (sans tenir compte des majuscules). Les noms exacts et courts d'abord.
    public IReadOnlyList<GameEntry> Search(string text, int max = 200)
    {
        var words = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [];
        }
        return Entries.Values
            .Where(e => words.All(w => e.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(e => !e.Name.Equals(text.Trim(), StringComparison.OrdinalIgnoreCase))
            .ThenBy(e => !e.Name.StartsWith(words[0], StringComparison.OrdinalIgnoreCase))
            .ThenBy(e => e.Tier)
            .ThenBy(e => e.Name.Length)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();
    }

    public IReadOnlyList<Recipe> Producing(string key)
    {
        Index();
        return _producing!.GetValueOrDefault(key) ?? [];
    }

    public IReadOnlyList<Recipe> Using(string key)
    {
        Index();
        return _using!.GetValueOrDefault(key) ?? [];
    }

    public string NameOf(string key) => Entries.TryGetValue(key, out var e) ? e.Name : $"objet inconnu ({key})";

    // Recette « paquet » (v0.4.12, demande de Julien : les paquets ne servent à rien) : emballer, déballer,
    // ou tout ce qui consomme ou produit un objet de catégorie « Package ».
    public bool IsPackage(Recipe r) =>
        r.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)
        || r.Inputs.Concat(r.Outputs).Any(i => Entries.TryGetValue(i.Key, out var e) && e.Tag == "Package");

    // Les façons d'obtenir un objet, les plus simples d'abord : récoltes, puis fabrications avec peu d'ingrédients et des
    // métiers bas. Les recettes « paquet » et celles qui consomment l'objet lui-même (convertir) sont écartées (v0.4.12).
    public IReadOnlyList<Recipe> Simplest(string key, int max = int.MaxValue) =>
        Producing(key)
            .Where(r => !IsPackage(r) && r.Inputs.All(i => i.Key != key))
            .OrderBy(r => r.NeedsKnowledge || r.NeedsClaimTech ? 1 : 0)
            .ThenBy(r => r.Kind == "Récolte" ? 0 : 1)
            .ThenBy(r => r.Inputs.Count)
            .ThenBy(r => r.Skills.Sum(s => s.Level))
            .Take(max)
            .ToList();

    // Une façon d'obtenir un objet, en une ligne (info-bulle des recettes, v0.4.6).
    // Ex. « Récolte sur « Oak Tree » · Forestry 1 · Axe 1 » ou « Fabrication (Carpentry Station, palier 1 minimum) : 4 × Oak Log · Carpentry 3 ».
    public string Summary(Recipe r)
    {
        var text = r.Kind == "Récolte"
            ? $"Récolte sur « {r.Place} »"
            : r.Inputs.Count == 0
                ? $"Fabrication ({r.Place})"
                : $"Fabrication ({r.Place}) : {string.Join(", ", r.Inputs.Select(i => $"{i.Quantity} × {NameOf(i.Key)}"))}";
        if (r.Kind == "Récolte" && r.Inputs.Count > 0)
        {
            text += $", consomme {string.Join(", ", r.Inputs.Select(i => $"{i.Quantity} × {NameOf(i.Key)}"))}";
        }
        var needs = r.Skills.Select(s => $"{s.Name} {s.Level}").Concat(r.Tools.Select(t => $"{t.Name} {t.Level}")).ToList();
        if (needs.Count > 0)
        {
            text += " · " + string.Join(" · ", needs);
        }
        if (r.NeedsKnowledge || r.NeedsClaimTech)
        {
            text += " · à débloquer";
        }
        return text;
    }

    private void Index()
    {
        if (_producing is not null)
        {
            return;
        }
        var producing = new Dictionary<string, List<Recipe>>();
        var usingIt = new Dictionary<string, List<Recipe>>();
        foreach (var r in Recipes)
        {
            foreach (var o in r.Outputs.Select(o => o.Key).Distinct())
            {
                (producing.TryGetValue(o, out var l) ? l : producing[o] = []).Add(r);
            }
            foreach (var i in r.Inputs.Select(i => i.Key).Distinct())
            {
                (usingIt.TryGetValue(i, out var l) ? l : usingIt[i] = []).Add(r);
            }
        }
        _using = usingIt;
        _producing = producing;
    }

    public static RecipeBook Load(string folder)
    {
        var entries = new Dictionary<string, GameEntry>();
        foreach (var e in Read(folder, "item_desc"))
        {
            var entry = new GameEntry(Int(e, "id"), false, Str(e, "name"), Str(e, "description"), Int(e, "tier"), Str(e, "tag"), Str(e, "rarity"), Str(e, "icon_asset_name"));
            if (entry.Name.Length > 0)
            {
                entries.TryAdd(entry.Key, entry);
            }
        }
        foreach (var e in Read(folder, "cargo_desc"))
        {
            var entry = new GameEntry(Int(e, "id"), true, Str(e, "name"), Str(e, "description"), Int(e, "tier"), Str(e, "tag"), Str(e, "rarity"), Str(e, "icon_asset_name"));
            if (entry.Name.Length > 0)
            {
                entries.TryAdd(entry.Key, entry);
            }
        }
        var skills = Read(folder, "skill_desc").ToDictionary(e => Int(e, "id"), e => Str(e, "name"));
        var tools = Read(folder, "tool_type_desc").ToDictionary(e => Int(e, "id"), e => Str(e, "name"));
        var buildings = Read(folder, "building_type_desc").ToDictionary(e => Int(e, "id"), e => Str(e, "name"));
        var resources = Read(folder, "resource_desc").GroupBy(e => Int(e, "id")).ToDictionary(g => g.Key, g => Str(g.First(), "name"));

        List<Requirement> Skills(JsonElement e) => Array(e, "level_requirements")
            .Select(s => new Requirement(skills.GetValueOrDefault(Int(s, "skill_id")) ?? $"métier {Int(s, "skill_id")}", Int(s, "level")))
            .Where(s => s.Name != "ANY" || s.Level > 1).ToList();
        List<Requirement> Tools(JsonElement e) => Array(e, "tool_requirements")
            .Select(t => new Requirement(tools.GetValueOrDefault(Int(t, "tool_type")) ?? $"outil {Int(t, "tool_type")}", Int(t, "level"))).ToList();
        static EntryAmount Stack(JsonElement s, double probability = 1) =>
            new((Str(s, "item_type") == "Cargo" ? "C" : "I") + Int(s, "item_id"), Int(s, "quantity"), probability);

        var recipes = new List<Recipe>();
        foreach (var e in Read(folder, "crafting_recipe_desc"))
        {
            var outputs = Array(e, "crafted_item_stacks").Select(s => Stack(s)).ToList();
            var name = Str(e, "name");
            if (name.Contains("{0}") && outputs.Count > 0)
            {
                name = name.Replace("{0}", entries.TryGetValue(outputs[0].Key, out var o) ? o.Name : "?");
            }
            var consumed = Array(e, "consumed_item_stacks").Select(s => Stack(s)).ToList();
            // « {1} » : le premier ingrédient (ex. « Unpack {1} », « Package {1} into … »).
            if (name.Contains("{1}") && consumed.Count > 0)
            {
                name = name.Replace("{1}", entries.TryGetValue(consumed[0].Key, out var c) ? c.Name : "?");
            }
            var place = "à la main";
            if (e.TryGetProperty("building_requirement", out var b) && b.ValueKind == JsonValueKind.Object && Int(b, "building_type") != 0)
            {
                place = $"{buildings.GetValueOrDefault(Int(b, "building_type")) ?? "atelier " + Int(b, "building_type")}, palier {Int(b, "tier")} minimum";
            }
            recipes.Add(new Recipe("Fabrication", name, place, Skills(e), Tools(e),
                consumed, outputs,
                Int(e, "actions_required"), Dbl(e, "time_requirement"), Dbl(e, "stamina_requirement"),
                Array(e, "required_knowledges").Any(), Int(e, "required_claim_tech_id") != 0, Bool(e, "allow_use_hands")));
        }
        foreach (var e in Read(folder, "extraction_recipe_desc"))
        {
            var outputs = Array(e, "extracted_item_stacks")
                .Where(s => s.TryGetProperty("item_stack", out _))
                .Select(s => Stack(s.GetProperty("item_stack"), Dbl(s, "probability"))).ToList();
            var resource = resources.GetValueOrDefault(Int(e, "resource_id")) ?? $"ressource {Int(e, "resource_id")}";
            var verb = Str(e, "verb_phrase");
            recipes.Add(new Recipe("Récolte", $"{(verb.Length > 0 ? verb : "Récolter")} : {resource}", resource, Skills(e), Tools(e),
                Array(e, "consumed_item_stacks").Select(s => Stack(s)).ToList(), outputs,
                0, Dbl(e, "time_requirement"), Dbl(e, "stamina_requirement"),
                Array(e, "required_knowledges").Any(), false, Bool(e, "allow_use_hands")));
        }
        // Conversions d'objets (v0.4.13) : un objet transformé en un autre (« Fill Bucket Of Water »…). Les lignes
        // « Resolve Item » (l'objet se transforme en lui-même) ne sont pas de vraies recettes.
        if (File.Exists(Path.Combine(folder, "item_conversion_recipe_desc.json")))
        {
            foreach (var e in Read(folder, "item_conversion_recipe_desc"))
            {
                if (!e.TryGetProperty("output_item", out var o) || o.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var output = Stack(o);
                var inputs = Array(e, "input_items").Select(s => Stack(s)).ToList();
                if (output.Quantity <= 0 || inputs.Count == 0 || inputs.Any(i => i.Key == output.Key) || Str(e, "name").StartsWith("Resolve Item", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var context = Str(e, "string_context");
                recipes.Add(new Recipe("Fabrication", Str(e, "name"), context.Length > 0 ? context : "conversion", [], [], inputs, [output],
                    0, Dbl(e, "time_cost"), Dbl(e, "stamina_cost"), false, false, Bool(e, "allow_use_hands")));
            }
        }
        return new RecipeBook { Entries = entries, Recipes = recipes };
    }

    private static List<JsonElement> Read(string folder, string table)
    {
        using var stream = File.OpenRead(Path.Combine(folder, table + ".json"));
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement.EnumerateArray().Select(e => e.Clone()).ToList()
            : throw new InvalidDataException($"{table}.json : liste attendue.");
    }

    private static IEnumerable<JsonElement> Array(JsonElement e, string property) =>
        e.TryGetProperty(property, out var a) && a.ValueKind == JsonValueKind.Array ? a.EnumerateArray() : [];

    private static string Str(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : string.Empty;

    private static int Int(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)
            ? (int)Math.Clamp(n, int.MinValue, int.MaxValue)
            : 0;

    private static double Dbl(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static bool Bool(JsonElement e, string property) =>
        e.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.True;
}
