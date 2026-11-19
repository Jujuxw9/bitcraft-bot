using BitcraftBot.Core.Logging;
using BitcraftBot.GameData;
using BitcraftBot.Perception.Hud;

namespace BitcraftBot.App;

// Écrit dans le journal ce qui change sur le HUD (pas chaque lecture, pour garder un journal lisible),
// plus un résumé toutes les 30 secondes.
public sealed class HudChangeLogger(SessionLog log)
{
    private static readonly TimeSpan SummaryInterval = TimeSpan.FromSeconds(30);

    private HudState? _previous;
    private DateTime _lastSummary = DateTime.MinValue;
    private int _reads;
    private int _positionsRead;
    private int _positionsByTemplates;
    private readonly ConfirmedText _zone = new();
    private readonly ConfirmedText _region = new();
    private int _staminaDropsWithoutGauge;
    private readonly ConfirmedText _target = new();
    private const string NoTarget = "(aucune)";
    private DateTime _notificationUntil = DateTime.MinValue;
    private static readonly TimeSpan NotificationDuration = TimeSpan.FromSeconds(8);
    private DateTime _lastDiagnosticAt = DateTime.MinValue;
    private ProfessionGaugeReading? _lastGauge;
    private int _gaugeMissing;
    private int _positionFailStreak;
    private double _totalReadMs;
    private readonly HashSet<string> _checkedTargets = [];

    // Données du jeu (D-033), pour comparer ce qui est lu à l'écran avec ce que disent les fichiers.
    public GameDataStore? GameData { get; set; }

    public void Reset()
    {
        _previous = null;
        _lastSummary = DateTime.MinValue;
        _reads = 0;
        _positionsRead = 0;
        _positionsByTemplates = 0;
        _zone.Reset();
        _region.Reset();
        _staminaDropsWithoutGauge = 0;
        _lastDiagnosticAt = DateTime.MinValue;
        _target.Reset();
        _notificationUntil = DateTime.MinValue;
        _lastGauge = null;
        _gaugeMissing = 0;
        _positionFailStreak = 0;
        _totalReadMs = 0;
    }

    // Renvoie une raison quand une image de diagnostic serait utile (situation anormale à comprendre), sinon null.
    public string? OnHudRead(HudState hud, double captureFps)
    {
        string? diagnostic = null;
        _reads++;
        _totalReadMs += hud.ReadDuration.TotalMilliseconds;
        if (hud.Position is not null)
        {
            _positionsRead++;
            if (hud.PositionSource == PositionSource.Templates)
            {
                _positionsByTemplates++;
            }
            _positionFailStreak = 0;
        }
        else if (++_positionFailStreak == 10)
        {
            log.Alerte("Perception", $"Position illisible 10 fois de suite (dernier texte lu : « {hud.RawPosition} »).");
            diagnostic = "position illisible";
        }

        var previous = _previous;
        if (previous is null)
        {
            log.Info("Perception", $"Première lecture du HUD : {Describe(hud)}.");
        }
        else
        {
            LogChanges(previous, hud);
        }

        if (hud.ReadAt - _lastSummary >= SummaryInterval)
        {
            if (previous is not null)
            {
                var rate = 100.0 * _positionsRead / _reads;
                log.Info("Perception", $"Résumé : {Describe(hud)}. Lecture moyenne {_totalReadMs / _reads:0} ms, " +
                                       $"position lue {rate:0} % du temps (dont {100.0 * _positionsByTemplates / _reads:0} % par les modèles de chiffres), {captureFps:0.0} images/s.");
            }
            _lastSummary = hud.ReadAt;
        }
        // Endurance qui baisse sans jauge de métier pendant 3 s : action probable mais jauge non vue (cas du 16/11 à 19:47).
        if (hud.Profession is null && previous?.Stamina.Value is { } stBefore && hud.Stamina.Value is { } stNow && stNow < stBefore)
        {
            _staminaDropsWithoutGauge++;
        }
        else if (hud.Profession is not null)
        {
            _staminaDropsWithoutGauge = 0;
        }
        if (_staminaDropsWithoutGauge >= 3)
        {
            log.Alerte("Perception", "L'endurance baisse mais la jauge de métier n'est pas vue.");
            diagnostic ??= "endurance en baisse sans jauge de métier";
            _staminaDropsWithoutGauge = 0;
        }

        _previous = Merge(previous, hud);

        // Au plus une image de diagnostic par minute.
        if (diagnostic is not null && hud.ReadAt - _lastDiagnosticAt >= TimeSpan.FromMinutes(1))
        {
            _lastDiagnosticAt = hud.ReadAt;
            return diagnostic;
        }
        return null;
    }

    private void LogChanges(HudState before, HudState now)
    {
        // Changements de zone et de région : notés seulement s'ils sont lus deux fois de suite
        // (évite « S A Pe Meadows » ou « R10 » au lieu de « R18 », lectures ratées isolées).
        if (_zone.Update(now.ZoneName) is { } zoneChange)
        {
            log.Info("Perception", $"Changement de zone : {zoneChange.Before} → {zoneChange.After}.");
        }
        var regionText = now.Region is { } r ? $"{r.Name} (R{r.Number})" : null;
        if (_region.Update(regionText) is { } regionChange)
        {
            log.Info("Perception", $"Changement de région : {regionChange.Before} → {regionChange.After}.");
        }

        // Jauge de métier : début et fin d'action, montée de niveau.
        // Fin d'action notée seulement après 4 lectures sans jauge (2 s) : une absence isolée n'est pas une fin.
        if (now.Profession is { } gauge)
        {
            if (_lastGauge is null)
            {
                log.Info("Perception", $"Action en cours : {gauge}.");
            }
            else if (gauge.Level is { } level && _lastGauge.Level is { } levelBefore && level > levelBefore)
            {
                log.Info("Perception", $"Niveau gagné : {gauge.Profession ?? "métier"} passe niveau {level}. Notification à l'écran : vie, faim et cible ignorées 8 s (Q-018).");
                _notificationUntil = now.ReadAt + NotificationDuration;
            }
            _lastGauge = gauge;
            _gaugeMissing = 0;
        }
        else if (_lastGauge is not null && ++_gaugeMissing >= 4)
        {
            log.Info("Perception", $"Fin d'action (jauge de métier disparue, dernière valeur : {_lastGauge}).");
            _lastGauge = null;
            _gaugeMissing = 0;
        }

        // Pendant une notification de montée de niveau, le bandeau recouvre la cible, la vie et la faim
        // (16/11 à 20:08 : faim lue à 0 %, cible « Lim MINING », santé 30/30000). Ces valeurs sont ignorées.
        var masked = now.ReadAt < _notificationUntil;

        // Vie qui baisse : signe d'attaque (Q-014).
        if (!masked && now.Health.Value is { } hp && before.Health.Value is { } hpBefore && hp < hpBefore)
        {
            log.Alerte("Perception", $"Vie en baisse : {hpBefore} → {hp}.");
        }

        // Endurance vide ou pleine (D-028).
        if (now.Stamina.Value is { } st && before.Stamina.Value is { } stBefore)
        {
            if (st == 0 && stBefore > 0)
            {
                log.Info("Perception", "Endurance à 0 : la récolte s'arrête (D-028).");
            }
            else if (st == now.Stamina.Max && stBefore < now.Stamina.Max)
            {
                log.Info("Perception", "Endurance pleine : la récolte peut reprendre.");
            }
        }

        // La faim n'est plus surveillée à l'écran (v0.3.16) : le serveur la donne exactement,
        // et le repas automatique s'en occupe (D-042).

        // Cible qui apparaît, change ou disparaît : notée seulement si lue 2 fois de suite, et pas pendant une notification.
        if (!masked && _target.Update(now.Target?.Name ?? NoTarget) is { } targetChange)
        {
            if (targetChange.After == NoTarget)
            {
                log.Info("Perception", $"Plus de cible (avant : {targetChange.Before}).");
            }
            else
            {
                log.Info("Perception", $"Cible : {now.Target}.");
                CheckAgainstGameData(now.Target!);
            }
        }
    }

    // « Faire confiance mais vérifier » : la santé max lue à l'écran doit être celle des données du jeu.
    private void CheckAgainstGameData(TargetReading target)
    {
        if (GameData is null || target.Name is null || target.MaxHealth is null || !_checkedTargets.Add(target.Name))
        {
            return;
        }
        var resource = GameData.FindResource(target.Name);
        if (resource is null)
        {
            log.Alerte("Données", $"Cible « {target.Name} » absente des données du jeu.");
        }
        else if (resource.MaxHealth != target.MaxHealth)
        {
            log.Alerte("Données", $"Désaccord : « {target.Name} » a {target.MaxHealth} points à l'écran, {resource.MaxHealth} dans les données du jeu.");
        }
        else
        {
            var extraction = GameData.Extractions.GetValueOrDefault(resource.Id);
            log.Info("Données", $"« {target.Name} » confirmé par les données du jeu : tier {resource.Tier}, {resource.MaxHealth} points" +
                                  (extraction is null ? "." : $", outil type {extraction.ToolType} niveau {extraction.ToolLevel}, {extraction.StaminaPerAction:0.##} d'endurance par coup."));
        }
    }

    // Garde les dernières valeurs connues quand une lecture échoue (pour comparer d'une lecture à l'autre).
    private static HudState Merge(HudState? previous, HudState now) =>
        previous is null
            ? now
            : now with
            {
                ZoneName = now.ZoneName ?? previous.ZoneName,
                Region = now.Region ?? previous.Region,
                Position = now.Position ?? previous.Position,
                Health = now.Health.IsTextRead ? now.Health : previous.Health,
                Stamina = now.Stamina.IsTextRead ? now.Stamina : previous.Stamina,
            };

    public static string Describe(HudState hud)
    {
        var position = hud.Position is { } p ? $"N {p.North}, E {p.East}" : "position ?";
        var region = hud.Region is { } r ? $"{r.Name} (R{r.Number})" : "région ?";
        var hunger = hud.HungerRatio is { } h ? $"≈{h * 100:0} %" : "?";
        var target = hud.Target is null ? "aucune" : hud.Target.ToString();
        var gauge = hud.Profession is null ? "aucune action" : hud.Profession.ToString();
        return $"{position}, {hud.ZoneName ?? "zone ?"}, {region}, vie {hud.Health}, endurance {hud.Stamina}, faim {hunger}, cible {target}, métier {gauge}";
    }
}
