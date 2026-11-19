using System.Diagnostics;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;

namespace BitcraftBot.Perception.Hud;

public sealed record TargetReading(string? Name, string? Category, int? Health, int? MaxHealth, string RawHealth)
{
    public override string ToString() =>
        $"{Name ?? "?"} ({Category ?? "?"}) {(Health is not null ? $"{Health}/{MaxHealth}" : "?")}";
}

// Méthode qui a permis de lire la position.
public enum PositionSource
{
    None,
    Templates,
    Ocr,
}

// Ce que le bot lit sur l'écran (v0.3.19) : position (contrôle croisé avec le serveur), zone, région et cible.
// Vie, endurance, faim et jauge de métier ne sont plus lues : le serveur les donne exactement (D-044, D-047).
public sealed record HudState
{
    public required DateTime ReadAt { get; init; }
    public required TimeSpan ReadDuration { get; init; }
    public (int North, int East)? Position { get; init; }
    public string RawPosition { get; init; } = string.Empty;
    public PositionSource PositionSource { get; init; } = PositionSource.None;
    public string? ZoneName { get; init; }
    public (string Name, int Number)? Region { get; init; }
    public TargetReading? Target { get; init; }

    // Zones dont la lecture a réussi (pour l'affichage en vert dans la fenêtre).
    public required IReadOnlySet<string> ZonesRead { get; init; }
}

// Lit le HUD sur une image complète du jeu (1920×1080).
// knownTargetNames : noms de ressources connus, pour corriger les erreurs de lecture.
public sealed class HudReader(OcrReader ocrReader, IReadOnlyList<string>? knownTargetNames = null)
{
    // Remplacée quand les données du jeu (D-033) sont chargées.
    public IReadOnlyList<string>? KnownTargetNames { get; set; } = knownTargetNames;

    // Réglages trouvés par essais sur une vraie capture (2026-11-16).
    private const int HudTextThreshold = 220;

    private readonly PositionFilter _positionFilter = new();

    // À appeler au début de chaque session (le personnage a pu bouger entre deux sessions).
    public void ResetPosition() => _positionFilter.Reset();

    public async Task<HudState> ReadAsync(Frame frame)
    {
        var watch = Stopwatch.StartNew();
        var zonesRead = new HashSet<string>();

        // Position : d'abord par les modèles de caractères (Q-016, fiable et < 1 ms),
        // l'OCR ne sert qu'en secours si les modèles ne reconnaissent pas la ligne.
        var position = GlyphReader.ReadCoordinates(frame, HudLayout.CoordinatesSearch, out var rawPosition);
        var positionSource = PositionSource.Templates;
        if (position is null)
        {
            positionSource = PositionSource.Ocr;
            // OCR lu deux fois avec deux agrandissements différents :
            // on ne garde la position que si les deux lectures sont identiques (une erreur 8 → 6 serait grave).
            var ocrPosition = await ocrReader.ReadAsync(frame, HudLayout.Coordinates, TextStyle.BrightText, HudTextThreshold, scale: 2);
            var ocrCheck = await ocrReader.ReadAsync(frame, HudLayout.Coordinates, TextStyle.BrightText, HudTextThreshold, scale: 4);
            position = HudParser.ParseCoordinates(ocrPosition);
            rawPosition = $"modèles « {rawPosition} » | OCR « {ocrPosition} » / « {ocrCheck} »";
            if (position is null || position != HudParser.ParseCoordinates(ocrCheck))
            {
                position = null;
                positionSource = PositionSource.None;
            }
        }
        // Garde-fou : un saut impossible est refusé tant qu'il n'est pas confirmé.
        position = _positionFilter.Accept(position, frame.CapturedAt, out var jumpRejected);
        if (jumpRejected)
        {
            rawPosition = $"{rawPosition} (saut refusé, à confirmer)";
            positionSource = PositionSource.None;
        }
        if (position is not null) zonesRead.Add("Position");

        var zone = HudParser.CleanName(await ocrReader.ReadAsync(frame, HudLayout.ZoneName, TextStyle.BrightText, HudTextThreshold, scale: 2), titleCase: true);
        if (zone is not null) zonesRead.Add("Zone");

        var region = HudParser.ParseRegion(await ocrReader.ReadAsync(frame, HudLayout.Region, TextStyle.BrightText, HudTextThreshold, scale: 2));
        if (region is not null) zonesRead.Add("Région");

        TargetReading? target = null;
        if (BarReader.IsTargetPanelVisible(frame))
        {
            var name = HudParser.CleanName(await ocrReader.ReadAsync(frame, HudLayout.TargetName, TextStyle.DarkText, 150), titleCase: false);
            if (name is not null && KnownTargetNames is { } knownNames)
            {
                // Correction par le nom connu le plus proche ; sinon on garde le texte lu tel quel.
                name = NameMatcher.BestMatch(name, knownNames) ?? name;
            }
            var category = HudParser.CleanName(await ocrReader.ReadAsync(frame, HudLayout.TargetCategory, TextStyle.DarkText, 150), titleCase: false);
            category = category?.Replace('—', '-').Replace('–', '-');
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
            Position = position,
            RawPosition = rawPosition,
            PositionSource = positionSource,
            ZoneName = zone,
            Region = region,
            Target = target,
            ZonesRead = zonesRead,
        };
    }
}
