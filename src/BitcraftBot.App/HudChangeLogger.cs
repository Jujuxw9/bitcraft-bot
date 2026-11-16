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
    private string? _zoneCandidate;
    private string? _confirmedZone;
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
        _zoneCandidate = null;
        _confirmedZone = null;
        _positionFailStreak = 0;
        _totalReadMs = 0;
    }

    public void OnHudRead(HudState hud, double captureFps)
    {
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
        _previous = Merge(previous, hud);
    }

    private void LogChanges(HudState before, HudState now)
    {
        // Changement de zone : noté seulement s'il est lu deux fois de suite (évite « S A Pe Meadows », lecture ratée isolée).
        if (now.ZoneName is not null && _confirmedZone is null)
        {
            _confirmedZone = now.ZoneName;
        }
        else if (now.ZoneName is not null && now.ZoneName != _confirmedZone)
        {
            if (_zoneCandidate == now.ZoneName)
            {
                log.Info("Perception", $"Changement de zone : {_confirmedZone} → {now.ZoneName}.");
                _confirmedZone = now.ZoneName;
                _zoneCandidate = null;
            }
            else
            {
                _zoneCandidate = now.ZoneName;
            }
        }
        else
        {
            _zoneCandidate = null;
        }

        if (now.Region is not null && before.Region is not null && now.Region != before.Region)
        {
            log.Info("Perception", $"Changement de région : {before.Region.Value.Name} (R{before.Region.Value.Number}) → " +
                                   $"{now.Region.Value.Name} (R{now.Region.Value.Number}).");
        }

        // Vie qui baisse : signe d'attaque (Q-014).
        if (now.Health.Value is { } hp && before.Health.Value is { } hpBefore && hp < hpBefore)
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

        // Faim sous 50 % (D-032).
        if (now.HungerRatio is { } hunger && before.HungerRatio is { } hungerBefore && hunger < 0.5 && hungerBefore >= 0.5)
        {
            log.Alerte("Perception", $"Faim sous 50 % (≈{hunger * 100:0} %) : il faudra manger (D-032).");
        }

        // Cible qui apparaît, change ou disparaît.
        var nameBefore = before.Target?.Name;
        var nameNow = now.Target?.Name;
        if (nameNow != nameBefore)
        {
            if (nameNow is null)
            {
                log.Info("Perception", $"Plus de cible (avant : {nameBefore}).");
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
        return $"{position}, {hud.ZoneName ?? "zone ?"}, {region}, vie {hud.Health}, endurance {hud.Stamina}, faim {hunger}, cible {target}";
    }
}
