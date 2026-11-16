using BitcraftBot.Core.Time;

namespace BitcraftBot.Core.Safety;

// Raison d'un arrêt de session.
public enum StopReason
{
    Manuel,
    ToucheUrgence,
    DureeMaximale,
    SourisBougee,
    Anomalie,
}

// Réglages de sécurité (D-004, D-022). Chaque mécanisme est activable dans l'interface.
public sealed class SafetyOptions
{
    public bool EmergencyKeyEnabled { get; set; } = true;
    public bool MaxDurationEnabled { get; set; } = true;
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromMinutes(15);

    // Prévus pour les versions qui agissent sur le jeu (v0.4 et plus).
    public bool MouseMoveStopEnabled { get; set; }
    public bool AnomalyStopEnabled { get; set; }
}

// Surveille la session et demande l'arrêt quand une règle de sécurité est déclenchée.
public sealed class SafetyMonitor
{
    private readonly IClock _clock;
    private DateTime? _startedAt;

    public SafetyOptions Options { get; }

    public bool IsRunning => _startedAt is not null;

    // Raison du dernier arrêt (null tant qu'aucun arrêt n'a eu lieu).
    public StopReason? LastStopReason { get; private set; }

    // Durée de la dernière session terminée.
    public TimeSpan LastSessionDuration { get; private set; }

    // Déclenché une seule fois par arrêt.
    public event Action<StopReason>? StopRequested;

    public SafetyMonitor(SafetyOptions options, IClock? clock = null)
    {
        Options = options;
        _clock = clock ?? SystemClock.Instance;
    }

    public void Start()
    {
        _startedAt = _clock.Now;
        LastStopReason = null;
    }

    public TimeSpan Elapsed => _startedAt is { } start ? _clock.Now - start : TimeSpan.Zero;

    public TimeSpan? Remaining =>
        IsRunning && Options.MaxDurationEnabled ? Options.MaxDuration - Elapsed : null;

    // Appelé régulièrement : vérifie la durée maximale.
    public void Tick()
    {
        if (!IsRunning)
        {
            return;
        }
        if (Options.MaxDurationEnabled && Elapsed >= Options.MaxDuration)
        {
            RequestStop(StopReason.DureeMaximale);
        }
    }

    // Appelé par le crochet clavier quand la touche d'urgence est pressée.
    public void OnEmergencyKey()
    {
        if (Options.EmergencyKeyEnabled)
        {
            RequestStop(StopReason.ToucheUrgence);
        }
    }

    public void RequestStop(StopReason reason)
    {
        if (!IsRunning)
        {
            return;
        }
        LastSessionDuration = Elapsed;
        _startedAt = null;
        LastStopReason = reason;
        StopRequested?.Invoke(reason);
    }

    public static string Describe(StopReason reason) => reason switch
    {
        StopReason.Manuel => "arrêt demandé avec le bouton",
        StopReason.ToucheUrgence => "ARRÊT D'URGENCE (touche Num 5)",
        StopReason.DureeMaximale => "durée maximale de session atteinte",
        StopReason.SourisBougee => "souris bougée à la main",
        StopReason.Anomalie => "anomalie détectée",
        _ => reason.ToString(),
    };
}
