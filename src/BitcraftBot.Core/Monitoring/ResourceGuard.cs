namespace BitcraftBot.Core.Monitoring;

// Seuils de surveillance. Réglables dans l'onglet Système (enregistrés dans config.local.json).
public sealed class ResourceLimits
{
    // RAM du bot : alerte, puis arrêt de la session si le seuil critique est dépassé.
    public double BotRamWarningMb { get; set; } = 600;
    public double BotRamCriticalMb { get; set; } = 1200;

    // CPU du bot : alerte si dépassé pendant plusieurs mesures de suite.
    public double BotCpuWarningPercent { get; set; } = 25;
    public int BotCpuSustainedSamples { get; set; } = 10;

    // RAM libre du PC : alerte sous ce pourcentage.
    public double SystemRamAvailableWarningPercent { get; set; } = 10;
}

public enum ResourceLevel
{
    Normal,
    Alerte,
    Critique,
}

// Événement à écrire dans le journal (ou à traiter : un événement critique arrête la session).
public sealed record ResourceEvent(ResourceLevel Level, string Message);

// Compare chaque mesure aux seuils. Ne prévient qu'au changement d'état (pas à chaque mesure),
// pour ne pas remplir le journal.
public sealed class ResourceGuard(ResourceLimits limits)
{
    private ResourceLevel _botRam = ResourceLevel.Normal;
    private bool _botCpuHigh;
    private int _botCpuHighCount;
    private bool _systemRamLow;

    public ResourceLimits Limits => limits;

    public ResourceLevel BotRamLevel => _botRam;

    public IReadOnlyList<ResourceEvent> Evaluate(ResourceSample sample)
    {
        var events = new List<ResourceEvent>();

        // RAM du bot
        var ramLevel = sample.BotRamMb >= limits.BotRamCriticalMb ? ResourceLevel.Critique
            : sample.BotRamMb >= limits.BotRamWarningMb ? ResourceLevel.Alerte
            : ResourceLevel.Normal;
        if (ramLevel != _botRam)
        {
            events.Add(ramLevel switch
            {
                ResourceLevel.Critique => new ResourceEvent(ResourceLevel.Critique,
                    $"RAM du bot critique : {sample.BotRamMb:0} Mo (seuil {limits.BotRamCriticalMb:0} Mo). Session arrêtée par sécurité."),
                ResourceLevel.Alerte => new ResourceEvent(ResourceLevel.Alerte,
                    $"RAM du bot élevée : {sample.BotRamMb:0} Mo (seuil d'alerte {limits.BotRamWarningMb:0} Mo)."),
                _ => new ResourceEvent(ResourceLevel.Normal, $"RAM du bot revenue à la normale : {sample.BotRamMb:0} Mo."),
            });
            _botRam = ramLevel;
        }

        // CPU du bot : seulement si l'excès dure.
        _botCpuHighCount = sample.BotCpuPercent >= limits.BotCpuWarningPercent ? _botCpuHighCount + 1 : 0;
        var cpuHigh = _botCpuHighCount >= limits.BotCpuSustainedSamples;
        if (cpuHigh != _botCpuHigh)
        {
            events.Add(cpuHigh
                ? new ResourceEvent(ResourceLevel.Alerte,
                    $"CPU du bot élevé : {sample.BotCpuPercent:0} % depuis {limits.BotCpuSustainedSamples} mesures (seuil {limits.BotCpuWarningPercent:0} %).")
                : new ResourceEvent(ResourceLevel.Normal, $"CPU du bot revenu à la normale : {sample.BotCpuPercent:0} %."));
            _botCpuHigh = cpuHigh;
        }

        // RAM libre du PC
        var ramLow = sample.SystemRamAvailablePercent < limits.SystemRamAvailableWarningPercent;
        if (ramLow != _systemRamLow)
        {
            events.Add(ramLow
                ? new ResourceEvent(ResourceLevel.Alerte,
                    $"RAM libre du PC faible : {sample.SystemRamAvailableMb:0} Mo ({sample.SystemRamAvailablePercent:0} %).")
                : new ResourceEvent(ResourceLevel.Normal,
                    $"RAM libre du PC revenue à la normale : {sample.SystemRamAvailableMb:0} Mo."));
            _systemRamLow = ramLow;
        }

        return events;
    }
}
