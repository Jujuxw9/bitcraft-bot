using BitcraftBot.Core.Logging;
using BitcraftBot.GameData;
using BitcraftBot.Perception.Hud;

namespace BitcraftBot.App;

// Écrit dans le journal ce qui change sur l'écran (v0.3.20 : la cible seulement), pas chaque lecture,
// plus un résumé toutes les 30 secondes.
public sealed class HudChangeLogger(SessionLog log)
{
    private static readonly TimeSpan SummaryInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan NotificationDuration = TimeSpan.FromSeconds(8);
    private const string NoTarget = "(aucune)";

    private readonly ConfirmedText _target = new();
    private readonly HashSet<string> _checkedTargets = [];
    private DateTime _lastSummary = DateTime.MinValue;
    private DateTime _notificationUntil = DateTime.MinValue;
    private int _reads;
    private double _totalReadMs;
    private bool _first = true;

    // Données du jeu (D-033), pour comparer ce qui est lu à l'écran avec ce que disent les fichiers.
    public GameDataStore? GameData { get; set; }

    public void Reset()
    {
        _target.Reset();
        _lastSummary = DateTime.MinValue;
        _notificationUntil = DateTime.MinValue;
        _reads = 0;
        _totalReadMs = 0;
        _first = true;
    }

    // Passage de niveau lu sur le serveur : le bandeau du jeu recouvre la cible pendant quelques secondes (Q-018).
    public void NotifyLevelUp(DateTime at) => _notificationUntil = at + NotificationDuration;

    public void OnHudRead(HudState hud, double captureFps)
    {
        _reads++;
        _totalReadMs += hud.ReadDuration.TotalMilliseconds;
        if (_first)
        {
            _first = false;
            log.Info("Perception", $"Première lecture de l'écran : {Describe(hud)}.");
        }

        // Pendant une notification de montée de niveau, le bandeau recouvre la cible
        // (16/11 à 20:08 : cible « Lim MINING », santé 30/30000). La cible est ignorée 8 s.
        var masked = hud.ReadAt < _notificationUntil;

        // Cible qui apparaît, change ou disparaît : notée seulement si lue 2 fois de suite, et pas pendant une notification.
        if (!masked && _target.Update(hud.Target?.Name ?? NoTarget) is { } targetChange)
        {
            if (targetChange.After == NoTarget)
            {
                log.Info("Perception", $"Plus de cible (avant : {targetChange.Before}).");
            }
            else
            {
                log.Info("Perception", $"Cible : {hud.Target}.");
                CheckAgainstGameData(hud.Target!);
            }
        }

        if (hud.ReadAt - _lastSummary >= SummaryInterval)
        {
            if (_lastSummary != DateTime.MinValue)
            {
                log.Detail("Perception", $"Résumé : {Describe(hud)}. Lecture moyenne {_totalReadMs / _reads:0} ms, {captureFps:0.0} images/s.");
            }
            _lastSummary = hud.ReadAt;
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

    public static string Describe(HudState hud) => $"cible {(hud.Target is null ? "aucune" : hud.Target.ToString())}";
}
