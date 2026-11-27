using BitcraftBot.Input.Human;

namespace BitcraftBot.Input.Actions;

// Ce que le contrôleur sait faire avec Windows : tout passe par là (SendInput dans le vrai bot, un faux dans les tests).
public interface IInputDevice
{
    (int X, int Y) CursorPosition();
    bool IsForeground(IntPtr window);
    void MoveTo(int x, int y);
    void LeftDown();
    void LeftUp();
    void KeyDown(ushort scanCode);
    void KeyUp(ushort scanCode);

    // Tape un caractère (lettre, accent, ponctuation) sans passer par la position de la touche (chat, v0.4.5).
    void TypeChar(char c) => throw new NotSupportedException("Frappe de texte non prise en charge.");
}

// Touches que le bot a le droit d'envoyer au jeu (scancodes = position physique, AZERTY de Julien).
// Toute autre touche est refusée par le contrôleur.
public static class GameKeys
{
    public const ushort Shift = 0x2A;
    public const ushort E = 0x12;
    public const ushort Z = 0x11;
    public const ushort Q = 0x1E;
    public const ushort S = 0x1F;
    public const ushort D = 0x20;
    public const ushort W = 0x2C;
    // v0.4.24 : reprise des récoltes. Tab = cible suivante (plus envoyée depuis v0.4.25, gardée dans la liste) ;
    // Interact = M en AZERTY (touche à droite de L : code 0x27).
    public const ushort Tab = 0x0F;
    public const ushort Interact = 0x27;

    // Rangée du haut du clavier (cases de la barre d'action, v0.4.17) : touches « 1 » à « 0 », « - », « = » du clavier QWERTY,
    // soit « & é " ' ( - è _ ç à ) = » en AZERTY : même position physique.
    public static readonly ushort[] ActionBarKeys = [0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D];

    private static readonly string[] ActionBarNames = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "="];

    // Touche de la case de la barre d'action (0 = touche 1). null hors de la barre.
    public static ushort? ForSlot(int slot) => slot >= 0 && slot < ActionBarKeys.Length ? ActionBarKeys[slot] : null;

    // Déclarée avant Allowed (ordre d'initialisation).
    public static readonly IReadOnlySet<ushort> Forbidden = new HashSet<ushort> { 0x0C, 0x0D, V, B };

    public static readonly IReadOnlyDictionary<ushort, string> Allowed = BuildAllowed();

    private static Dictionary<ushort, string> BuildAllowed()
    {
        var keys = new Dictionary<ushort, string>
        {
            [Shift] = "Maj",
            [E] = "E",
            [Z] = "Z",
            [Q] = "Q",
            [S] = "S",
            [D] = "D",
            [W] = "W",
            [Tab] = "Tab",
            [Interact] = "M",
            [MapKey] = "carte (« . » clavier principal)",
            [MapKeyNumpad] = "carte (« . » pavé numérique)",
        };
        for (var i = 0; i < ActionBarKeys.Length; i++)
        {
            // v0.4.29 : « - » et « = » (chariot, bateau) jamais permis, même si une capacité y est rangée.
            if (!Forbidden.Contains(ActionBarKeys[i]))
            {
                keys[ActionBarKeys[i]] = ActionBarNames[i];
            }
        }
        return keys;
    }

    // v0.4.29 (règles de Julien, 27/11) : touches JAMAIS envoyées, quoi qu'il arrive. Vérifiées avant la liste permise.
    // « - » (0x0C) = chariot, « = » (0x0D) = bateau à voile ; V (0x2F) et B (0x30) : Maj + V = Quick Deploy/Store,
    // Maj + B = Quick Board. V et B ne sont jamais utiles au bot : interdites seules aussi.
    public const ushort V = 0x2F;
    public const ushort B = 0x30;

    // v0.4.29 : touche de la grande carte du jeu (« . »). Deux candidates tant que T-11 n'a pas trouvé la bonne :
    // 0x34 = position du « . » QWERTY (touche « : » en AZERTY) ; 0x53 = « . » du pavé numérique.
    public const ushort MapKey = 0x34;
    public const ushort MapKeyNumpad = 0x53;

    // Sprint = case 4 de la barre (touche « ' » en AZERTY, position du « 4 »).
    public const ushort Sprint = 0x05;

    // Vrai si la touche (ou la combinaison) est interdite.
    public static bool IsForbidden(IEnumerable<ushort> keys) => keys.Any(Forbidden.Contains);

    public static string Name(ushort scanCode) => Allowed.TryGetValue(scanCode, out var name) ? name : $"0x{scanCode:X2}";

    // Entrée et Échap : PAS dans la liste autorisée. Seul l'envoi d'un message dans le chat s'en sert (D-054, ChatAsync).
    public const ushort Enter = 0x1C;
    public const ushort Escape = 0x01;
}

// Ce que l'envoi dans le chat demande à l'écran (v0.4.5). Chaque fonction capture une image neuve du jeu.
public sealed class ChatProbe
{
    // Chat ouvert (zone de saisie visible) ? null = jeu pas visible, image impossible.
    public required Func<Task<bool?>> IsOpen { get; init; }

    // Canal affiché sur le bouton du chat (« REGION »), tel que lu ; vide si pas lu.
    public required Func<Task<string>> Channel { get; init; }

    // Le canal lu est-il le bon ?
    public required Func<string, bool> ChannelOk { get; init; }

    // Dernier clic de souris de Julien (vrai clic, pas celui du bot). Un clic pendant la frappe arrête tout (v0.4.8).
    public Func<DateTime> LastUserClick { get; init; } = () => DateTime.MinValue;
}

public enum ActionOutcome
{
    Fait,
    Refuse,
    Interrompu,
    Erreur,
}

public sealed record ActionResult(ActionOutcome Outcome, string Detail)
{
    public bool Done => Outcome == ActionOutcome.Fait;
}

public enum ActionLogLevel
{
    Detail,
    Info,
    Alerte,
    Erreur,
}

// Ce que le contrôleur demande au reste du bot, à chaque vérification.
public sealed class ActionContext
{
    // Fenêtre du jeu (null si introuvable).
    public Func<IntPtr?> GameWindow { get; init; } = () => null;

    // Jeu visible, pas caché par une autre fenêtre.
    public Func<bool> GameVisible { get; init; } = () => false;

    // Dernière touche tapée par Julien (touche physique, pas simulée).
    public Func<DateTime> LastPhysicalKey { get; init; } = () => DateTime.MinValue;

    public Func<DateTime> Now { get; init; } = () => DateTime.Now;

    // Écran du jeu pas prêt (accueil, connexion, chargement) : la raison, sinon null (v0.4.1).
    public Func<string?> ScreenNotReady { get; init; } = () => null;
}

public sealed class ActionOptions
{
    // Au plus N actions par minute, toutes actions confondues (garde-fou contre une boucle folle).
    public int MaxActionsPerMinute { get; set; } = 20;

    // Pas d'action si Julien a tapé au clavier il y a moins de ce délai.
    public TimeSpan UserQuietTime { get; set; } = TimeSpan.FromSeconds(3);

    // Arrêt si la souris bouge pendant un geste du bot (c'est Julien qui la tient).
    public bool StopOnMouseMove { get; set; } = true;

    public int MouseTolerancePx { get; set; } = 4;
}

// Contrôleur d'actions commun : TOUTES les touches et tous les clics du bot passent par lui, avec les mêmes sécurités :
// - une seule action à la fois ;
// - touches limitées à la liste autorisée ;
// - BitCraft fenêtre active et visible, vérifié avant chaque appui ;
// - rien si Julien tape au clavier ;
// - nombre d'actions par minute limité ;
// - arrêt (Halt : fermeture, souris bougée) : l'action en cours est interrompue et plus rien ne part jusqu'à la reprise ;
// - souris bougée par Julien pendant un geste : arrêt d'urgence ;
// - touches et bouton toujours relâchés, même en cas d'erreur ;
// - chaque action est écrite dans le journal.
public sealed class ActionController
{
    private readonly IInputDevice _device;
    private readonly ActionContext _context;
    private readonly Random _random;
    private readonly Func<int, CancellationToken, Task> _delay;
    private readonly Queue<DateTime> _recent = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _current;
    private int _busy;

    public ActionController(IInputDevice device, ActionContext context, ActionOptions? options = null, Random? random = null,
        Func<int, CancellationToken, Task>? delay = null)
    {
        _device = device;
        _context = context;
        Options = options ?? new ActionOptions();
        _random = random ?? new Random();
        _delay = delay ?? ((ms, token) => Task.Delay(ms, token));
    }

    public ActionOptions Options { get; }

    public event Action<ActionLogLevel, string>? Log;

    public bool IsHalted { get; private set; }

    public string? HaltReason { get; private set; }

    public bool IsBusy => Volatile.Read(ref _busy) == 1;

    public int ActionsLastMinute
    {
        get
        {
            lock (_lock)
            {
                Prune(_context.Now());
                return _recent.Count;
            }
        }
    }

    // Arrêt d'urgence : interrompt l'action en cours, refuse toutes les suivantes jusqu'à Resume().
    public void Halt(string reason)
    {
        lock (_lock)
        {
            if (!IsHalted)
            {
                IsHalted = true;
                HaltReason = reason;
                Log?.Invoke(ActionLogLevel.Alerte, $"Actions bloquées : {reason}.");
            }
            _current?.Cancel();
        }
    }

    public void Resume(string why)
    {
        lock (_lock)
        {
            if (IsHalted)
            {
                IsHalted = false;
                HaltReason = null;
                Log?.Invoke(ActionLogLevel.Info, $"Actions de nouveau permises ({why}).");
            }
        }
    }

    // Raison pour laquelle aucune action ne peut partir maintenant (null = tout est bon).
    public string? WhyNot()
    {
        if (IsHalted)
        {
            return $"actions bloquées ({HaltReason})";
        }
        if (IsBusy)
        {
            return "une action est déjà en cours";
        }
        var game = _context.GameWindow();
        if (game is not { } handle || handle == IntPtr.Zero)
        {
            return "jeu introuvable";
        }
        if (!_device.IsForeground(handle))
        {
            return "BitCraft n'est pas la fenêtre active";
        }
        if (!_context.GameVisible())
        {
            return "le jeu est caché par une autre fenêtre";
        }
        if (_context.ScreenNotReady() is { } screen)
        {
            return screen;
        }
        var now = _context.Now();
        if (now - _context.LastPhysicalKey() < Options.UserQuietTime)
        {
            return "vous tapez au clavier";
        }
        lock (_lock)
        {
            Prune(now);
            if (_recent.Count >= Options.MaxActionsPerMinute)
            {
                return $"déjà {Options.MaxActionsPerMinute} actions dans la minute";
            }
        }
        return null;
    }

    // Appuie sur une touche ou une combinaison (ex. Maj + E) : appui dans l'ordre, relâchement dans l'ordre inverse.
    public async Task<ActionResult> PressAsync(string label, params ushort[] keys)
    {
        if (keys.Length > 0 && GameKeys.IsForbidden(keys))
        {
            var forbidden = $"{label} : touche interdite (chariot, bateau, Quick Deploy/Board), jamais envoyée";
            Log?.Invoke(ActionLogLevel.Erreur, forbidden);
            return new ActionResult(ActionOutcome.Refuse, forbidden);
        }
        if (keys.Length == 0 || keys.Any(k => !GameKeys.Allowed.ContainsKey(k)))
        {
            var refused = $"{label} : touche non autorisée ({string.Join(" + ", keys.Select(GameKeys.Name))})";
            Log?.Invoke(ActionLogLevel.Erreur, refused);
            return new ActionResult(ActionOutcome.Refuse, refused);
        }
        var chord = string.Join(" + ", keys.Select(GameKeys.Name));
        return await RunAsync(label, chord, async token =>
        {
            var pressed = new Stack<ushort>();
            try
            {
                foreach (var key in keys)
                {
                    EnsureStillAllowed(token);
                    _device.KeyDown(key);
                    pressed.Push(key);
                    await _delay(40 + _random.Next(70), token);
                }
                await _delay(50 + _random.Next(80), token);
            }
            finally
            {
                // Relâchement garanti, sans tenir compte de l'arrêt : une touche ne reste jamais enfoncée.
                while (pressed.Count > 0)
                {
                    _device.KeyUp(pressed.Pop());
                    if (pressed.Count > 0)
                    {
                        await _delay(20 + _random.Next(40), CancellationToken.None);
                    }
                }
            }
        });
    }

    // Déplace la souris d'une façon humaine jusqu'à (x, y), dans la zone autorisée, puis clique (bouton gauche).
    public async Task<ActionResult> ClickAsync(string label, int x, int y, SafeArea area, HumanMouse mouse)
    {
        if (!area.Contains(x, y))
        {
            var refused = $"{label} : clic hors de la zone autorisée ({x}, {y})";
            Log?.Invoke(ActionLogLevel.Erreur, refused);
            return new ActionResult(ActionOutcome.Refuse, refused);
        }
        return await RunAsync(label, $"clic en ({x}, {y})", async token =>
        {
            var (fromX, fromY) = _device.CursorPosition();
            (int X, int Y)? expected = null;
            var elapsed = 0.0;
            foreach (var step in mouse.Path(fromX, fromY, x, y, area))
            {
                EnsureStillAllowed(token);
                if (expected is { } e && Options.StopOnMouseMove)
                {
                    var (cx, cy) = _device.CursorPosition();
                    if (Math.Abs(cx - e.X) > Options.MouseTolerancePx || Math.Abs(cy - e.Y) > Options.MouseTolerancePx)
                    {
                        Halt("souris bougée pendant un geste du bot");
                        throw new OperationCanceledException(token);
                    }
                }
                var wait = (int)Math.Round(step.AtMs - elapsed);
                if (wait > 0)
                {
                    await _delay(wait, token);
                }
                elapsed = step.AtMs;
                _device.MoveTo(step.X, step.Y);
                expected = (step.X, step.Y);
            }
            await _delay(60 + _random.Next(120), token);
            EnsureStillAllowed(token);
            var down = false;
            try
            {
                _device.LeftDown();
                down = true;
                await _delay(50 + _random.Next(70), token);
            }
            finally
            {
                if (down)
                {
                    _device.LeftUp();
                }
            }
        });
    }

    // v0.4.25 (reprise des récoltes) : déplace la souris d'une façon humaine jusqu'à (x, y), dans la zone autorisée,
    // SANS cliquer. Mêmes sécurités que ClickAsync (souris bougée par Julien pendant le geste = arrêt d'urgence).
    public async Task<ActionResult> MoveAsync(string label, int x, int y, SafeArea area, HumanMouse mouse)
    {
        if (!area.Contains(x, y))
        {
            var refused = $"{label} : souris hors de la zone autorisée ({x}, {y})";
            Log?.Invoke(ActionLogLevel.Erreur, refused);
            return new ActionResult(ActionOutcome.Refuse, refused);
        }
        return await RunAsync(label, $"souris en ({x}, {y})", async token =>
        {
            var (fromX, fromY) = _device.CursorPosition();
            (int X, int Y)? expected = null;
            var elapsed = 0.0;
            foreach (var step in mouse.Path(fromX, fromY, x, y, area))
            {
                EnsureStillAllowed(token);
                if (expected is { } e && Options.StopOnMouseMove)
                {
                    var (cx, cy) = _device.CursorPosition();
                    if (Math.Abs(cx - e.X) > Options.MouseTolerancePx || Math.Abs(cy - e.Y) > Options.MouseTolerancePx)
                    {
                        Halt("souris bougée pendant un geste du bot");
                        throw new OperationCanceledException(token);
                    }
                }
                var wait = (int)Math.Round(step.AtMs - elapsed);
                if (wait > 0)
                {
                    await _delay(wait, token);
                }
                elapsed = step.AtMs;
                _device.MoveTo(step.X, step.Y);
                expected = (step.X, step.Y);
            }
            // Dernier point exact (le chemin humain finit sur la cible, on s'en assure).
            _device.MoveTo(x, y);
        });
    }

    // Écrit un message dans le chat du jeu, comme Julien (D-054, D-055) :
    // chat fermé vérifié → Entrée → chat ouvert vérifié → canal « Région » vérifié → texte tapé → Entrée →
    // chat refermé vérifié (sinon Échap). Si quelque chose ne va pas, rien n'est envoyé et le chat est refermé.
    // Julien qui tape une touche pendant la frappe : arrêt, chat refermé.
    // v0.4.7 : command (« /y », « /s »…) = commande du jeu pour écrire dans le bon canal. Si le bouton ne montre pas
    // le bon canal, la commande est tapée d'abord ; si le bouton ne change pas, elle reste au début de la ligne
    // (le jeu la lit à l'envoi). Frappe irrégulière, comme une vraie main (pauses après la ponctuation, hésitations).
    public async Task<ActionResult> ChatAsync(string label, string text, ChatProbe probe, string? command = null)
    {
        if (command is not null && (command.Length is < 2 or > 5 || command[0] != '/' || !command[1..].All(char.IsAsciiLetterLower)))
        {
            var refused = $"{label} : commande de canal refusée (« {command} »)";
            Log?.Invoke(ActionLogLevel.Erreur, refused);
            return new ActionResult(ActionOutcome.Refuse, refused);
        }
        if (string.IsNullOrWhiteSpace(text) || text.Length > 250 || text.Any(char.IsControl))
        {
            var refused = $"{label} : texte refusé (vide, trop long ou caractère de contrôle)";
            Log?.Invoke(ActionLogLevel.Erreur, refused);
            return new ActionResult(ActionOutcome.Refuse, refused);
        }
        var startedAt = _context.Now();
        var opened = false;
        string? problem = null;
        var result = await RunAsync(label, $"message de {text.Length} caractères dans le chat", async token =>
        {
            try
            {
                if (await probe.IsOpen() != false)
                {
                    problem = "chat déjà ouvert ou pas vu";
                    return;
                }
                await TapAsync(GameKeys.Enter, token);
                opened = true;
                if (!await WaitChatAsync(probe, open: true, token))
                {
                    problem = "le chat ne s'est pas ouvert";
                    return;
                }
                var channel = await probe.Channel();
                // Petit temps avant la première lettre (on pose les doigts sur le clavier).
                await _delay(300 + _random.Next(500), token);
                if (!probe.ChannelOk(channel))
                {
                    if (command is null)
                    {
                        problem = $"canal du chat pas le bon (lu : « {channel} »)";
                        return;
                    }
                    if (!await TypeHumanAsync(command + " ", startedAt, probe, token))
                    {
                        problem = ClosedWhileTyping;
                        return;
                    }
                    await _delay(350 + _random.Next(400), token);
                    var after = await probe.Channel();
                    Log?.Invoke(ActionLogLevel.Info, probe.ChannelOk(after)
                        ? $"{label} : canal changé par « {command} » (lu avant : « {channel} », après : « {after} »)."
                        : $"{label} : bouton du canal pas changé par « {command} » (lu avant : « {channel} », après : « {after} ») : commande gardée au début du message.");
                }
                if (!await TypeHumanAsync(text, startedAt, probe, token))
                {
                    problem = ClosedWhileTyping;
                    return;
                }
                await _delay(300 + _random.Next(500), token);
                EnsureStillAllowed(token);
                EnsureUserQuiet(startedAt, probe);
                // v0.4.8 : chat encore ouvert juste avant Entrée (sinon Entrée le rouvrirait, journal du 24/11 00:04).
                if (await probe.IsOpen() != true)
                {
                    problem = ClosedWhileTyping;
                    return;
                }
                EnsureStillAllowed(token);
                await TapAsync(GameKeys.Enter, token);
                if (await WaitChatAsync(probe, open: false, token))
                {
                    opened = false;
                }
                else
                {
                    problem = "chat encore ouvert après l'envoi";
                }
            }
            finally
            {
                if (opened)
                {
                    await CloseChatAsync(label, probe);
                }
            }
        });
        if (result.Done && problem is not null)
        {
            Log?.Invoke(ActionLogLevel.Alerte, $"{label} : pas envoyé ({problem}).");
            return new ActionResult(ActionOutcome.Refuse, problem);
        }
        return result;
    }

    // Frappe lettre par lettre, irrégulière comme une vraie main (v0.4.7, demande de Julien).
    // v0.4.8 (journal du 24/11 00:03, Julien : « vérifier entre chaque frappe ») : AVANT CHAQUE LETTRE, image neuve
    // du jeu, le chat doit être ouvert. Sinon : plus aucune lettre (elle partirait dans le jeu comme une action).
    // Renvoie false si le chat n'est plus ouvert. Le temps de la vérification est pris sur la pause entre deux lettres.
    private async Task<bool> TypeHumanAsync(string text, DateTime startedAt, ChatProbe probe, CancellationToken token)
    {
        char? previous = null;
        var pause = 0;
        foreach (var c in text)
        {
            var checkStart = System.Diagnostics.Stopwatch.GetTimestamp();
            EnsureStillAllowed(token);
            EnsureUserQuiet(startedAt, probe);
            if (await probe.IsOpen() != true)
            {
                return false;
            }
            var checkMs = (int)System.Diagnostics.Stopwatch.GetElapsedTime(checkStart).TotalMilliseconds;
            if (pause - checkMs > 0)
            {
                await _delay(pause - checkMs, token);
            }
            EnsureStillAllowed(token);
            EnsureUserQuiet(startedAt, probe);
            _device.TypeChar(c);
            pause = KeyDelay(c, previous);
            previous = c;
        }
        if (pause > 0)
        {
            await _delay(pause, token);
        }
        return true;
    }

    private const string ClosedWhileTyping = "chat fermé pendant la frappe : arrêt, rien envoyé";

    // Temps entre deux lettres : 55 à 165 ms ; plus long après une espace et après la ponctuation ;
    // de temps en temps (4 %) une hésitation de 0,3 à 0,9 s.
    public int KeyDelay(char c, char? previous)
    {
        var delay = 55 + _random.Next(110);
        if (c == ' ')
        {
            delay += 30 + _random.Next(90);
        }
        if (previous is '.' or ',' or '!' or '?' or ':' or ';' or '…')
        {
            delay += 150 + _random.Next(300);
        }
        if (_random.NextDouble() < 0.04)
        {
            delay += 300 + _random.Next(600);
        }
        return delay;
    }

    // Échap jusqu'à ce que le chat soit fermé (2 essais). Jamais laissé ouvert sans alerte (D-055).
    private async Task CloseChatAsync(string label, ChatProbe probe)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var open = await probe.IsOpen();
            if (open == false)
            {
                Log?.Invoke(ActionLogLevel.Info, $"{label} : chat refermé.");
                return;
            }
            if (open is null)
            {
                // Écran pas vu : pas d'Échap à l'aveugle (sur le jeu sans chat, Échap ouvre le menu).
                break;
            }
            var game = _context.GameWindow();
            if (game is not { } handle || !_device.IsForeground(handle))
            {
                break;
            }
            await TapAsync(GameKeys.Escape, CancellationToken.None);
            await _delay(400, CancellationToken.None);
        }
        if (await probe.IsOpen() == false)
        {
            Log?.Invoke(ActionLogLevel.Info, $"{label} : chat refermé (Échap).");
            return;
        }
        Halt("chat peut-être resté ouvert : vérifier le jeu");
    }

    // Attend que le chat soit ouvert (ou fermé), au plus 1,5 s.
    private async Task<bool> WaitChatAsync(ChatProbe probe, bool open, CancellationToken token)
    {
        for (var i = 0; i < 6; i++)
        {
            await _delay(250, token);
            if (await probe.IsOpen() == open)
            {
                return true;
            }
        }
        return false;
    }

    // Appui court sur une touche (Entrée, Échap), relâchement garanti.
    private async Task TapAsync(ushort key, CancellationToken token)
    {
        _device.KeyDown(key);
        try
        {
            await _delay(40 + _random.Next(60), token);
        }
        finally
        {
            _device.KeyUp(key);
        }
    }

    // Julien a tapé une touche depuis le début de l'envoi : on arrête (il reprend la main).
    private void EnsureUserQuiet(DateTime since, ChatProbe? probe = null)
    {
        if (_context.LastPhysicalKey() > since)
        {
            _interruption = "vous avez tapé au clavier";
            throw new OperationCanceledException();
        }
        // v0.4.8 : un clic de Julien peut fermer le chat (ou changer de fenêtre) : on arrête aussi.
        if (probe is not null && probe.LastUserClick() > since)
        {
            _interruption = "vous avez cliqué avec la souris";
            throw new OperationCanceledException();
        }
    }

    private async Task<ActionResult> RunAsync(string label, string what, Func<CancellationToken, Task> body)
    {
        var why = WhyNot();
        if (why is not null)
        {
            Log?.Invoke(ActionLogLevel.Detail, $"{label} ({what}) refusé : {why}.");
            return new ActionResult(ActionOutcome.Refuse, why);
        }
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            return new ActionResult(ActionOutcome.Refuse, "une action est déjà en cours");
        }
        CancellationTokenSource cts;
        lock (_lock)
        {
            _recent.Enqueue(_context.Now());
            cts = new CancellationTokenSource();
            _current = cts;
        }
        Log?.Invoke(ActionLogLevel.Info, $"{label} : {what}.");
        try
        {
            await body(cts.Token);
            return new ActionResult(ActionOutcome.Fait, what);
        }
        catch (OperationCanceledException)
        {
            var reason = IsHalted ? HaltReason ?? "arrêt" : _interruption ?? "interrompu";
            Log?.Invoke(ActionLogLevel.Alerte, $"{label} interrompu : {reason}. Touches relâchées.");
            return new ActionResult(ActionOutcome.Interrompu, reason);
        }
        catch (Exception ex)
        {
            Log?.Invoke(ActionLogLevel.Erreur, $"{label} : échec ({ex.Message}). Touches relâchées.");
            return new ActionResult(ActionOutcome.Erreur, ex.Message);
        }
        finally
        {
            lock (_lock)
            {
                _current = null;
            }
            _interruption = null;
            cts.Dispose();
            Volatile.Write(ref _busy, 0);
        }
    }

    private string? _interruption;

    // Vérifié avant chaque appui et chaque pas de souris : le jeu doit rester actif, et pas d'arrêt demandé.
    private void EnsureStillAllowed(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (IsHalted)
        {
            throw new OperationCanceledException(token);
        }
        var game = _context.GameWindow();
        if (game is not { } handle || !_device.IsForeground(handle))
        {
            _interruption = "BitCraft n'est plus la fenêtre active";
            throw new OperationCanceledException(token);
        }
    }

    private void Prune(DateTime now)
    {
        while (_recent.Count > 0 && now - _recent.Peek() > TimeSpan.FromMinutes(1))
        {
            _recent.Dequeue();
        }
    }
}
