using System.Diagnostics;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;

namespace BitcraftBot.Perception.Hud;

// Valeur lue sur une barre : texte « 65/100 » si l'OCR l'a lu, sinon estimation par la longueur de la barre.
public sealed record GaugeReading(int? Value, int? Max, double? FillRatio, string RawText)
{
    public bool IsTextRead => Value is not null && Max is not null;

    // Pourcentage retenu : le texte s'il est lu, sinon le remplissage.
    public double? Percent => IsTextRead ? 100.0 * Value!.Value / Max!.Value : FillRatio * 100;

    public override string ToString() => IsTextRead
        ? $"{Value}/{Max}"
        : FillRatio is { } f ? $"≈{f * 100:0} % (barre)" : "?";
}

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

// Tout ce que la v0.2 lit sur l'écran.
public sealed record HudState
{
    public required DateTime ReadAt { get; init; }
    public required TimeSpan ReadDuration { get; init; }
    public (int North, int East)? Position { get; init; }
    public string RawPosition { get; init; } = string.Empty;
    public PositionSource PositionSource { get; init; } = PositionSource.None;
    public string? ZoneName { get; init; }
    public (string Name, int Number)? Region { get; init; }
    public required GaugeReading Health { get; init; }
    public required GaugeReading Stamina { get; init; }
    public double? HungerRatio { get; init; }
    public TargetReading? Target { get; init; }

    // Jauge de métier (null si absente : aucune action en cours).
    public ProfessionGaugeReading? Profession { get; init; }

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

        var health = await ReadGaugeAsync(frame, HudLayout.HealthText, HudLayout.HealthBarStart, HudLayout.HealthBarEnd, BarReader.IsHealthGreen);
        if (health.IsTextRead) zonesRead.Add("Vie");

        var stamina = await ReadGaugeAsync(frame, HudLayout.StaminaText, HudLayout.StaminaBarStart, HudLayout.StaminaBarEnd, BarReader.IsStaminaYellow);
        if (stamina.IsTextRead) zonesRead.Add("Endurance");

        var hunger = BarReader.RightAnchoredFill(frame, HudLayout.HungerRow, HudLayout.HungerLineStart, HudLayout.HungerLineEnd, BarReader.IsWhite);

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

        var profession = ProfessionGaugeReader.Read(frame);
        if (profession is not null) zonesRead.Add("Métier");

        return new HudState
        {
            Profession = profession,
            ReadAt = frame.CapturedAt,
            ReadDuration = watch.Elapsed,
            Position = position,
            RawPosition = rawPosition,
            PositionSource = positionSource,
            ZoneName = zone,
            Region = region,
            Health = health,
            Stamina = stamina,
            HungerRatio = hunger,
            Target = target,
            ZonesRead = zonesRead,
        };
    }

    private async Task<GaugeReading> ReadGaugeAsync(Frame frame, ScreenRect textZone, int barStart, int barEnd,
        Func<byte, byte, byte, bool> isFilled)
    {
        var raw = await ocrReader.ReadAsync(frame, textZone, TextStyle.BrightText, 215);
        var parsed = HudParser.ParseFraction(raw);
        var fill = BarReader.LeftAnchoredFill(frame, HudLayout.BarsRow, barStart, barEnd, isFilled);
        if (parsed is not null && fill is not null && !IsConsistent(parsed.Value.Value, parsed.Value.Max, fill.Value))
        {
            // Le texte lu contredit la longueur de la barre : lecture rejetée, on garde l'estimation par la barre.
            return new GaugeReading(null, null, fill, $"{raw} (rejeté : barre à {fill.Value * 100:0} %)");
        }
        return new GaugeReading(parsed?.Value, parsed?.Max, fill, raw);
    }

    // Texte et barre doivent concorder à 15 % près.
    public static bool IsConsistent(int value, int max, double fill) =>
        Math.Abs((double)value / max - fill) <= MaxGaugeMismatch;

    private const double MaxGaugeMismatch = 0.15;
}
