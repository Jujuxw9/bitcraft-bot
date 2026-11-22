using System.Diagnostics;
using System.Text;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Native;

namespace BitcraftBot.Perception.Screen;

public enum Place
{
    // Bureau de Windows (fond d'écran, icônes, barre des tâches).
    Bureau,

    // Un autre programme est au premier plan, le jeu est fermé ou caché.
    AutreProgramme,

    // Jeu ouvert mais réduit dans la barre des tâches.
    JeuReduit,

    // Le jeu est visible (au premier plan, ou visible pendant que Julien clique sur le bot).
    Jeu,
}

// Ce que le bot voit, en un mot (v0.4.1).
public sealed record ScreenSituation(Place Place, bool GameRunning, GameScreen? Game, string? Foreground, string Evidence)
{
    public string Describe() => Place switch
    {
        Place.Bureau => GameRunning ? "Bureau de Windows (jeu caché)" : "Bureau de Windows (jeu fermé)",
        Place.AutreProgramme => $"Autre programme : {Foreground ?? "?"}{(GameRunning ? " (jeu caché)" : " (jeu fermé)")}",
        Place.JeuReduit => "Jeu réduit dans la barre des tâches",
        _ => $"Jeu : {GameScreens.French(Game ?? GameScreen.Inconnu)}",
    };

    // Le bot peut agir dans le jeu : seulement en jeu (pas pendant l'accueil, la connexion ni le chargement).
    public bool InGame => Place == Place.Jeu && Game == GameScreen.EnJeu;

    // Même situation, pour n'écrire qu'une ligne au changement.
    public bool SameAs(ScreenSituation? other) =>
        other is not null && other.Place == Place && other.Game == Game && other.GameRunning == GameRunning
        && (Place != Place.AutreProgramme || other.Foreground == Foreground);
}

// Ce que Windows dit de la fenêtre au premier plan (séparé pour être testé).
public sealed record ForegroundInfo(string ClassName, string ProcessName);

public static class ScreenSituations
{
    // Classes des fenêtres du bureau de Windows : fond d'écran (Progman, WorkerW), barre des tâches.
    private static readonly string[] DesktopClasses = ["Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"];

    // Situation hors image : où est Julien, le jeu est-il visible ?
    // gameVisible : jeu trouvé, pas réduit, pas caché par une autre fenêtre (GameVisibility).
    public static ScreenSituation Decide(bool gameRunning, bool gameMinimized, bool gameVisible, ForegroundInfo? foreground, string? ownProcess)
    {
        if (gameRunning && gameVisible)
        {
            return new ScreenSituation(Place.Jeu, true, null, foreground?.ProcessName, "jeu visible");
        }
        if (gameRunning && gameMinimized)
        {
            return new ScreenSituation(Place.JeuReduit, true, null, foreground?.ProcessName, "fenêtre du jeu réduite");
        }
        if (foreground is null || DesktopClasses.Contains(foreground.ClassName))
        {
            return new ScreenSituation(Place.Bureau, gameRunning, null, null, $"fenêtre au premier plan : {foreground?.ClassName ?? "aucune"}");
        }
        var name = foreground.ProcessName == ownProcess ? "le bot" : foreground.ProcessName;
        return new ScreenSituation(Place.AutreProgramme, gameRunning, null, name, $"fenêtre au premier plan : programme {name}");
    }

    // Fenêtre au premier plan : sa classe et le nom de son programme (jamais son titre : il peut être privé).
    public static ForegroundInfo? Foreground()
    {
        var handle = Win32.GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return null;
        }
        var builder = new StringBuilder(128);
        Win32.GetClassName(handle, builder, builder.Capacity);
        Win32.GetWindowThreadProcessId(handle, out var pid);
        string process;
        try
        {
            using var p = Process.GetProcessById((int)pid);
            process = p.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            process = "?";
        }
        return new ForegroundInfo(builder.ToString(), process);
    }

    public static bool IsMinimized(GameWindow game) => Win32.IsIconic(game.Handle);

    // La fenêtre existe-t-elle encore (jeu pas fermé) ?
    public static bool Exists(GameWindow game) => Win32.IsWindow(game.Handle);
}
