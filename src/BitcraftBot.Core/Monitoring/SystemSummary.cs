using System.Globalization;

namespace BitcraftBot.Core.Monitoring;

// Ligne « système » de l'overlay (v0.4.23, demande de Julien) : CPU arrondi à 0,5 %, RAM arrondie à 5 Mo.
public static class SystemSummary
{
    public static double RoundCpu(double percent) => Math.Round(percent * 2, MidpointRounding.AwayFromZero) / 2;

    public static double RoundRam(double mb) => Math.Round(mb / 5, MidpointRounding.AwayFromZero) * 5;

    public static string Overlay(ResourceSample s, CultureInfo? culture = null)
    {
        var c = culture ?? CultureInfo.GetCultureInfo("fr-FR");
        string Cpu(double v) => string.Format(c, "CPU {0:0.0} %", RoundCpu(v));
        string Ram(double mb) => string.Format(c, "RAM {0:N0} Mo", RoundRam(mb));
        var game = s.GameRamMb is { } g ? $"   ·   Jeu : {Cpu(s.GameCpuPercent ?? 0)}, {Ram(g)}" : "";
        return $"Bot : {Cpu(s.BotCpuPercent)}, {Ram(s.BotRamMb)}{game}";
    }
}
