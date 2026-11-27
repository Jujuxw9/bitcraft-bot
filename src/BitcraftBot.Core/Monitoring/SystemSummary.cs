using System.Globalization;

namespace BitcraftBot.Core.Monitoring;

// Ligne « système » de l'overlay (v0.4.23, demande de Julien) : CPU arrondi à 0,5 %, RAM arrondie à 5 Mo.
// v0.4.25 : ligne raccourcie (overlay plus étroit) : « Bot 1,5 % · 585 Mo │ Jeu 22 % · 4,3 Go ».
// Pour le jeu, CPU au pour cent près et RAM en Go avec une décimale.
public static class SystemSummary
{
    public const int MaxLength = 42;

    public static double RoundCpu(double percent) => Math.Round(percent * 2, MidpointRounding.AwayFromZero) / 2;

    public static double RoundRam(double mb) => Math.Round(mb / 5, MidpointRounding.AwayFromZero) * 5;

    public static string Overlay(ResourceSample s, CultureInfo? culture = null)
    {
        var c = culture ?? CultureInfo.GetCultureInfo("fr-FR");
        var bot = string.Format(c, "Bot {0:0.#} % · {1:0} Mo", RoundCpu(s.BotCpuPercent), RoundRam(s.BotRamMb));
        var game = s.GameRamMb is { } g
            ? string.Format(c, " │ Jeu {0:0} % · {1:0.0} Go", Math.Round(s.GameCpuPercent ?? 0, MidpointRounding.AwayFromZero), g / 1024.0)
            : "";
        return bot + game;
    }
}
