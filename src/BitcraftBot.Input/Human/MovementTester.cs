using BitcraftBot.Input.Native;

namespace BitcraftBot.Input.Human;

// Raison de fin d'un test de mouvements.
public enum TestEnd
{
    Termine,
    ArreteParJulien,
    JeuPlusAuPremierPlan,
    SourisBougeeALaMain,
    Erreur,
}

public sealed class MovementTestOptions
{
    public required IntPtr GameWindow { get; init; }

    // Zone autorisée pour la souris (pixels écran) : centre de l'écran, hors HUD.
    public required SafeArea Area { get; init; }

    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(30);

    // Étape 2 : clics gauches (petits déplacements du personnage).
    public bool AllowLeftClicks { get; init; }

    // Les clics se font dans ce rayon autour du personnage (centre de l'écran), pour ne pas aller loin.
    public int ClickRadius { get; init; } = 220;
    public (int X, int Y) CharacterCenter { get; init; } = (960, 560);
}

// Exécute les tests de mouvements (D-035), avec ses sécurités :
// jeu au premier plan obligatoire, arrêt si Julien bouge la souris, arrêt immédiat sur annulation.
public sealed class MovementTester(InputSender sender, Random random, Action<string> log)
{
    // Écart toléré entre la position voulue et la position réelle du curseur avant de conclure
    // que Julien a bougé la souris lui-même.
    private const int ManualMoveTolerance = 25;

    private (int X, int Y)? _expectedCursor;

    public async Task<TestEnd> RunMouseAsync(MovementTestOptions options, CancellationToken token)
    {
        var mouse = new HumanMouse(random);
        var end = DateTime.Now + options.Duration;
        var gestures = 0;
        var clicks = 0;
        _expectedCursor = null;
        try
        {
            while (DateTime.Now < end)
            {
                var check = Check(options);
                if (check is not null) return Finish(check.Value, gestures, clicks);

                var (fromX, fromY) = sender.CursorPosition();
                var clickNow = options.AllowLeftClicks && random.NextDouble() < 0.35;
                var (toX, toY) = clickNow
                    ? ClickTarget(options)
                    : mouse.RandomTarget(fromX, fromY, options.Area);

                var path = mouse.Path(fromX, fromY, toX, toY, options.Area);
                var started = DateTime.Now;
                foreach (var step in path)
                {
                    var wait = step.AtMs - (DateTime.Now - started).TotalMilliseconds;
                    if (wait > 0) await Task.Delay(TimeSpan.FromMilliseconds(wait), token);
                    check = Check(options);
                    if (check is not null) return Finish(check.Value, gestures, clicks);
                    sender.MoveTo(step.X, step.Y);
                    _expectedCursor = (step.X, step.Y);
                }
                gestures++;
                var duration = path.Count > 0 ? path[^1].AtMs : 0;

                if (clickNow)
                {
                    // Petit temps de visée, puis clic gauche avec une durée d'appui humaine.
                    await Task.Delay(TimeSpan.FromMilliseconds(60 + random.NextDouble() * 140), token);
                    check = Check(options);
                    if (check is not null) return Finish(check.Value, gestures, clicks);
                    sender.LeftDown();
                    await Task.Delay(TimeSpan.FromMilliseconds(50 + random.NextDouble() * 70), CancellationToken.None);
                    sender.LeftUp();
                    clicks++;
                    log($"Geste {gestures} : ({fromX}, {fromY}) → ({toX}, {toY}) en {duration:0} ms, puis clic gauche (déplacement).");
                }
                else
                {
                    log($"Geste {gestures} : ({fromX}, {fromY}) → ({toX}, {toY}) en {duration:0} ms, {path.Count} positions.");
                }
                await Task.Delay(TimeSpan.FromMilliseconds(mouse.PauseMs()), token);
            }
            return Finish(TestEnd.Termine, gestures, clicks);
        }
        catch (OperationCanceledException)
        {
            return Finish(TestEnd.ArreteParJulien, gestures, clicks);
        }
        catch (InvalidOperationException ex)
        {
            log($"Erreur d'envoi : {ex.Message}");
            return Finish(TestEnd.Erreur, gestures, clicks);
        }
        finally
        {
            // Jamais de bouton laissé enfoncé.
            TryRelease(() => sender.LeftUp());
        }
    }

    public async Task<TestEnd> RunCameraAsync(MovementTestOptions options, CancellationToken token)
    {
        var camera = new HumanCamera(random);
        var end = DateTime.Now + options.Duration;
        var actions = 0;
        _expectedCursor = sender.CursorPosition();
        try
        {
            while (DateTime.Now < end)
            {
                var check = Check(options);
                if (check is not null) return FinishCamera(check.Value, actions);

                var action = camera.Next(actions);
                var scan = CameraKeys.ScanCode(action.Key);
                sender.KeyDown(scan);
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(action.HoldMs), token);
                }
                finally
                {
                    sender.KeyUp(scan);
                }
                actions++;
                log($"Caméra {actions} : {CameraKeys.Label(action.Key)} maintenue {action.HoldMs:0} ms, pause {action.PauseMs:0} ms.");
                await Task.Delay(TimeSpan.FromMilliseconds(action.PauseMs), token);
            }
            return FinishCamera(TestEnd.Termine, actions);
        }
        catch (OperationCanceledException)
        {
            return FinishCamera(TestEnd.ArreteParJulien, actions);
        }
        catch (InvalidOperationException ex)
        {
            log($"Erreur d'envoi : {ex.Message}");
            return FinishCamera(TestEnd.Erreur, actions);
        }
        finally
        {
            // Toutes les touches de caméra relâchées, quoi qu'il arrive.
            foreach (var key in Enum.GetValues<CameraKey>())
            {
                TryRelease(() => sender.KeyUp(CameraKeys.ScanCode(key)));
            }
        }
    }

    // Vérifie les sécurités avant chaque mouvement. Null = on peut continuer.
    private TestEnd? Check(MovementTestOptions options)
    {
        if (!sender.IsForeground(options.GameWindow))
        {
            return TestEnd.JeuPlusAuPremierPlan;
        }
        if (_expectedCursor is { } expected)
        {
            var (x, y) = sender.CursorPosition();
            if (Math.Abs(x - expected.X) > ManualMoveTolerance || Math.Abs(y - expected.Y) > ManualMoveTolerance)
            {
                return TestEnd.SourisBougeeALaMain;
            }
        }
        return null;
    }

    private (int X, int Y) ClickTarget(MovementTestOptions options)
    {
        var angle = random.NextDouble() * 2 * Math.PI;
        var radius = options.ClickRadius * (0.4 + random.NextDouble() * 0.6);
        var x = options.CharacterCenter.X + (int)(Math.Cos(angle) * radius);
        var y = options.CharacterCenter.Y + (int)(Math.Sin(angle) * radius * 0.6);
        return options.Area.Clamp(x, y);
    }

    private TestEnd Finish(TestEnd reason, int gestures, int clicks)
    {
        log($"Test souris terminé ({Describe(reason)}) : {gestures} gestes, {clicks} clics.");
        return reason;
    }

    private TestEnd FinishCamera(TestEnd reason, int actions)
    {
        log($"Test caméra terminé ({Describe(reason)}) : {actions} mouvements.");
        return reason;
    }

    public static string Describe(TestEnd reason) => reason switch
    {
        TestEnd.Termine => "durée écoulée",
        TestEnd.ArreteParJulien => "arrêt demandé",
        TestEnd.JeuPlusAuPremierPlan => "le jeu n'est plus au premier plan",
        TestEnd.SourisBougeeALaMain => "souris bougée à la main",
        TestEnd.Erreur => "erreur",
        _ => reason.ToString(),
    };

    private static void TryRelease(Action release)
    {
        try
        {
            release();
        }
        catch (InvalidOperationException)
        {
            // Rien de plus à faire : on a essayé de relâcher.
        }
    }
}
