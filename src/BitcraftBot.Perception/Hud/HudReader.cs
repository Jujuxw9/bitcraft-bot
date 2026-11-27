using System.Diagnostics;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;

namespace BitcraftBot.Perception.Hud;

public sealed record TargetReading(string? Name, string? Category, int? Health, int? MaxHealth, string RawHealth)
{
    public override string ToString() =>
        $"{Name ?? "?"} ({Category ?? "?"}) {(Health is not null ? $"{Health}/{MaxHealth}" : "?")}";
}

// Ce que le bot lit sur l'écran (v0.3.20) : seulement la cible sélectionnée (nom, catégorie, santé).
// Le serveur ne la connaît qu'au premier coup ; tout le reste (position, zone, région, vie, endurance, faim,
// niveaux) vient du serveur (D-044, D-048).
public sealed record HudState
{
    public required DateTime ReadAt { get; init; }
    public required TimeSpan ReadDuration { get; init; }
    public TargetReading? Target { get; init; }

    // Zones dont la lecture a réussi (pour l'affichage en vert dans la fenêtre).
    public required IReadOnlySet<string> ZonesRead { get; init; }
}

// Lit la cible sur une image complète du jeu (1920×1080).
// knownTargetNames : noms de ressources connus, pour corriger les erreurs de lecture.
public sealed class HudReader(OcrReader ocrReader, IReadOnlyList<string>? knownTargetNames = null)
{
    // Remplacée quand les données du jeu (D-033) sont chargées.
    public IReadOnlyList<string>? KnownTargetNames { get; set; } = knownTargetNames;

    private static readonly System.Text.RegularExpressions.Regex TierOne = new(@"\bTier [Il|](?=\s|$)");

    public async Task<HudState> ReadAsync(Frame frame)
    {
        var watch = Stopwatch.StartNew();
        var zonesRead = new HashSet<string>();
        TargetReading? target = null;
        // Panneau de la cible absent : aucune lecture de texte (rien ne coûte).
        var panelStyle = BarReader.GetTargetPanelStyle(frame);
        if (panelStyle != TargetPanelStyle.None)
        {
            // Panneau crème : texte foncé ; panneau sombre (depuis le 23/11/2026) : texte clair.
            var textStyle = panelStyle == TargetPanelStyle.Cream ? TextStyle.DarkText : TextStyle.BrightText;
            var name = HudParser.CleanName(await ocrReader.ReadAsync(frame, HudLayout.TargetName, textStyle, 150), titleCase: false);
            if (name is not null && KnownTargetNames is { } knownNames)
            {
                // Correction par le nom connu le plus proche ; sinon on garde le texte lu tel quel.
                name = NameMatcher.BestMatch(name, knownNames) ?? name;
            }
            var category = HudParser.CleanName(await ocrReader.ReadAsync(frame, HudLayout.TargetCategory, textStyle, 150), titleCase: false);
            category = category?.Replace('—', '-').Replace('–', '-');
            // Le « 1 » de la police claire est parfois lu comme un « I » ou un « l ».
            category = category is null ? null : TierOne.Replace(category, "Tier 1");
            var rawHealth = await ocrReader.ReadAsync(frame, HudLayout.TargetHealth, TextStyle.BrightText, 225);
            var targetHealth = HudParser.ParseFraction(rawHealth);
            target = new TargetReading(name, category, targetHealth?.Value, targetHealth?.Max, rawHealth);
            if (name is not null) zonesRead.Add("Cible");
            if (category is not null) zonesRead.Add("Catégorie");
            if (targetHealth is not null) zonesRead.Add("Santé cible");
        }

        return new HudState
        {
            ReadAt = frame.CapturedAt,
            ReadDuration = watch.Elapsed,
            Target = target,
            ZonesRead = zonesRead,
        };
    }
}
