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

    public static readonly IReadOnlyDictionary<ushort, string> Allowed = new Dictionary<ushort, string>
    {
        [Shift] = "Maj",
        [E] = "E",
        [Z] = "Z",
        [Q] = "Q",
        [S] = "S",
        [D] = "D",
        [W] = "W",
    };

    public static string Name(ushort scanCode) => Allowed.TryGetValue(scanCode, out var name) ? name : $"0x{scanCode:X2}";
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
