using System.Text.RegularExpressions;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Perception.Ocr;

namespace BitcraftBot.Perception.Screen;

// Écran du jeu reconnu sur une image (demande de Julien, v0.4.1) : le jeu passe par l'accueil (un clic),
// la connexion (bouton Login), le chargement, puis le jeu.
public enum GameScreen
{
    Inconnu,
    Accueil,
    Connexion,
    Chargement,
    EnJeu,
}

// Indice : ce qui a permis de reconnaître l'écran (écrit dans le journal). Text : texte lu sur l'écran inconnu.
public sealed record ScreenReading(GameScreen Screen, string Evidence, string? Text = null);

public static class GameScreens
{
    public static string French(GameScreen s) => s switch
    {
        GameScreen.Accueil => "écran d'accueil",
        GameScreen.Connexion => "écran de connexion",
        GameScreen.Chargement => "écran de chargement",
        GameScreen.EnJeu => "en jeu",
        _ => "écran inconnu",
    };
}

// Reconnaît l'écran du jeu, du plus sûr au moins sûr :
// 1. En jeu : les coordonnées « N 8958, E 6747 » sous la mini-carte (lecture d'une petite zone, rapide).
// 2. Chargement : le mot « LOADING » en bas à gauche (vu le 2026-11-16, « Initializing player… »).
// 3. Sinon, lecture de tout l'écran et mots-clés : connexion (Enter BitCraft, Log out…), accueil (Press any button),
//    puis coordonnées dans le texte de tout l'écran. Mots vérifiés sur les captures de Julien du 2026-11-22.
//    Écran pas reconnu : « inconnu », avec le texte lu, pour corriger les mots-clés.
public sealed partial class GameScreenClassifier(OcrReader ocr)
{
    // Zone du mot « LOADING » et de la ligne en dessous (capture du 2026-11-16, 1920 × 1080).
    public static readonly ScreenRect LoadingText = new(170, 882, 260, 50);

    public static readonly string[] LoadingWords = ["loading", "initializing", "connecting"];

    // Connexion : vu le 2026-11-22 (compte déjà connu) : « WELCOME », « ENTER BITCRAFT », « LOG OUT »,
    // « ANNOUNCEMENTS », « HAVING TROUBLE? », « CHANGE YOUR EMAIL ». Les mots « login », « password »… restent
    // pour l'écran sans compte connu (pas encore vu, À vérifier).
    public static readonly string[] LoginWords =
        ["enter bitcraft", "log out", "having trouble", "change your email", "announcements", "login", "log in", "sign in", "password", "username"];

    // Accueil : « PRESS ANY BUTTON TO START » (vu le 2026-11-22).
    public static readonly string[] WelcomeWords = ["press any", "any button", "to start"];

    public async Task<ScreenReading> ClassifyAsync(Frame frame)
    {
        // Zone étroite d'abord : la zone large attrape aussi la distance du repère (« 35m ») au milieu du texte.
        foreach (var zone in new[] { HudLayout.Coordinates, HudLayout.CoordinatesSearch })
        {
            var coordinates = await ocr.ReadAsync(frame, Scale(zone, frame), TextStyle.BrightText, 220, scale: 2);
            if (LooksLikeCoordinates(coordinates))
            {
                return new ScreenReading(GameScreen.EnJeu, $"coordonnées « {coordinates.Trim()} » sous la mini-carte");
            }
        }
        var loading = await ocr.ReadAsync(frame, Scale(LoadingText, frame), TextStyle.BrightText, 150, scale: 2);
        if (FindWord(loading, LoadingWords) is { } lw)
        {
            return new ScreenReading(GameScreen.Chargement, $"mot « {lw} » en bas à gauche");
        }
        // Personnage (goutte bleue) au centre de la mini-carte (v0.4.4) : les coordonnées sont parfois mal lues
        // (chasse, bandeaux, discussion), 20 « écran inconnu » en jeu le 23/11.
        if (MinimapCone.PlayerVisible(frame))
        {
            return new ScreenReading(GameScreen.EnJeu, "personnage au centre de la mini-carte");
        }
        // Tout l'écran : texte clair, puis texte foncé (boutons clairs ?).
        var whole = new ScreenRect(0, 0, frame.Width, frame.Height);
        var bright = await ocr.ReadAsync(frame, whole, TextStyle.BrightText, 200, scale: 1);
        var dark = await ocr.ReadAsync(frame, whole, TextStyle.DarkText, 70, scale: 1);
        // Tout le texte sert à reconnaître l'écran ; seul le journal reçoit un extrait (avant : coupé à 300 caractères
        // AVANT la reconnaissance, les mots de la connexion tombaient dans la partie coupée).
        return ClassifyText($"{bright} {dark}");
    }

    // Reconnaissance par mots-clés du texte de tout l'écran (séparée pour être testée sans image).
    public static ScreenReading ClassifyText(string text)
    {
        var extract = Compact(text);
        if (FindWord(text, LoadingWords) is { } lw)
        {
            return new ScreenReading(GameScreen.Chargement, $"mot « {lw} »", extract);
        }
        if (FindWord(text, LoginWords) is { } li)
        {
            return new ScreenReading(GameScreen.Connexion, $"mot « {li} »", extract);
        }
        if (FindWord(text, WelcomeWords) is { } we)
        {
            return new ScreenReading(GameScreen.Accueil, $"mot « {we} »", extract);
        }
        // Coordonnées trouvées dans le texte de tout l'écran (la petite zone a pu être mal lue : bandeau
        // « ENTERING … » au-dessus, journal du 22/11 à 23:48:10).
        if (CoordinatesShape().Match(text) is { Success: true } c)
        {
            return new ScreenReading(GameScreen.EnJeu, $"coordonnées « {c.Value.Trim()} » dans le texte de l'écran", extract);
        }
        return new ScreenReading(GameScreen.Inconnu, string.IsNullOrWhiteSpace(text) ? "aucun texte lu" : "aucun mot connu", extract);
    }

    // « N 8958, E 6747 », même mal lu (« N 0927, E 6707 », « N 8923r E 6713 ») : seule la présence compte ici.
    public static bool LooksLikeCoordinates(string? text) => text is not null && CoordinatesShape().IsMatch(text);

    [GeneratedRegex(@"\bN\s*-?\d[\d.,]{2,}.*?\bE\s*-?\d[\d\s]{2,}")]
    private static partial Regex CoordinatesShape();

    public static string? FindWord(string? text, IEnumerable<string> words)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        // Début de mot obligatoire : « disconnecting » (annonces de l'écran de connexion) ne doit pas donner « connecting ».
        var normalized = " " + Spaces().Replace(text.ToLowerInvariant(), " ") + " ";
        return words.FirstOrDefault(w => Regex.IsMatch(normalized, @"(?<![\p{L}\p{N}])" + Regex.Escape(w), RegexOptions.CultureInvariant));
    }

    // Les zones sont mesurées en 1920 × 1080 : ramenées à la taille de l'image.
    public static ScreenRect Scale(ScreenRect r, Frame frame)
    {
        if (frame.Width == HudLayout.ReferenceWidth && frame.Height == HudLayout.ReferenceHeight)
        {
            return r;
        }
        var sx = frame.Width / (double)HudLayout.ReferenceWidth;
        var sy = frame.Height / (double)HudLayout.ReferenceHeight;
        return new ScreenRect((int)(r.X * sx), (int)(r.Y * sy), Math.Max(1, (int)(r.Width * sx)), Math.Max(1, (int)(r.Height * sy)));
    }

    private static string Compact(string text)
    {
        var t = Spaces().Replace(text, " ").Trim();
        return t.Length > 300 ? t[..300] + "…" : t;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
