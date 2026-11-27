using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BitcraftBot.Input.Actions;
using BitcraftBot.Input.Human;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using BitcraftBot.Server.World;
using Frame = BitcraftBot.Perception.Capture.Frame;

namespace BitcraftBot.App;

// v0.4.29 (T-10) : « Se déplacer ici ». Clic droit sur la carte du bot → menu → le bot ouvre la grande carte du jeu,
// mesure son échelle (barre du bas lue par OCR), pose la souris sur la case, clic gauche (« Set destination », D-074),
// referme la carte et suit la marche sur le serveur. Étude : memory/technique/deplacement-etude.md.
// Sécurités : petits trajets (≤ 30 cases), ligne droite sans escalade ni nage (relief gardé), une seule à la fois,
// touches interdites jamais envoyées (chariot, bateau), vie qui baisse = Sprint + fuite, vie à 0 = Get Up seulement.
public partial class MainWindow
{
    private const string TravelLog = "Déplacement";
    private readonly HumanMouse _travelMouse = new(new Random());
    private bool _travelBusy;
    private bool _travelStop;
    private DateTime _travelMapAllowedUntil = DateTime.MinValue;

    // Points de la souris pour la calibration (pixels à droite et en bas du centre).
    private const int CalibrationStep = 200;

    private static bool IsTravelTestRun => Environment.GetCommandLineArgs().Any(a => a.Equals("--essai-deplacement", StringComparison.OrdinalIgnoreCase));

    private static bool IsTravelMenuTestRun => Environment.GetCommandLineArgs().Any(a => a.Equals("--essai-menu-deplacement", StringComparison.OrdinalIgnoreCase));

    private void InitTravel()
    {
        TravelCheck.IsChecked = _settings.TravelEnabled;
        TravelStatusText.Text = _settings.TravelEnabled ? "Clic droit sur la carte du bot : « Se déplacer ici »." : "Coupé.";
        _log.Info(TravelLog, _settings.TravelEnabled
            ? $"Déplacements activés : clic droit sur la carte du bot, « Se déplacer ici » ({PathCheck.MaxDistance:0} cases au plus, sans escalade ni nage). Touche de la carte du jeu : 0x{_settings.MapKeyScanCode:X2}."
            : "Déplacements coupés (interrupteur dans OPTIONS).");
        if (IsTravelTestRun)
        {
            _ = RunTravelTestAsync();
        }
        if (IsTravelMenuTestRun)
        {
            _ = RunTravelMenuTestAsync();
        }
    }

    private void TravelCheck_Click(object sender, RoutedEventArgs e)
    {
        _settings.TravelEnabled = TravelCheck.IsChecked == true;
        _settings.Save(LocalSettings.DefaultPath);
        TravelStatusText.Text = _settings.TravelEnabled ? "Clic droit sur la carte du bot : « Se déplacer ici »." : "Coupé.";
        _log.Info(TravelLog, _settings.TravelEnabled ? "Déplacements activés (case cochée)." : "Déplacements coupés (case décochée).");
        if (!_settings.TravelEnabled)
        {
            _travelStop = true;
        }
    }

    // Vérification avant le trajet : distance et relief en ligne droite. null = permis.
    private PathCheck.Result? CheckTravel(double east, double north)
    {
        if (_lastSnapshot?.Position is not { } p)
        {
            return new PathCheck.Result(false, "position du personnage inconnue (serveur pas lu)");
        }
        if (_terrainStore is not { } store)
        {
            return new PathCheck.Result(false, "relief pas encore lu");
        }
        var chunks = store.Around(p.ScreenEast, p.ScreenNorth, PathCheck.MaxDistance + 4);
        return PathCheck.Check(p.ScreenEast, p.ScreenNorth, east, north, PathCheck.FromChunks(chunks));
    }

    // Menu du clic droit sur la carte du bot.
    private void OnMapRightClick(double east, double north)
    {
        _log.Detail("Carte", $"Clic droit sur la carte : case E {east:0}, N {north:0}.");
        var menu = BuildTravelMenu(east, north, out _);
        menu.PlacementTarget = SrvMap;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private ContextMenu BuildTravelMenu(double east, double north, out PathCheck.Result check)
    {
        var e = Math.Round(east);
        var n = Math.Round(north);
        check = CheckTravel(e, n)!;
        var menu = new ContextMenu();
        var item = new MenuItem { Header = $"Se déplacer ici (N {n:0}, E {e:0})" };
        if (!_settings.TravelEnabled)
        {
            item.IsEnabled = false;
            menu.Items.Add(item);
            menu.Items.Add(new MenuItem { Header = "Déplacements coupés (OPTIONS)", IsEnabled = false });
            return menu;
        }
        if (_travelBusy)
        {
            menu.Items.Add(new MenuItem { Header = "Arrêter le déplacement en cours" });
            ((MenuItem)menu.Items[0]).Click += (_, _) => StopTravel("bouton Arrêter du menu");
            return menu;
        }
        if (!check.Allowed)
        {
            item.IsEnabled = false;
            menu.Items.Add(item);
            menu.Items.Add(new MenuItem { Header = $"Refusé : {check.Detail}", IsEnabled = false });
            return menu;
        }
        item.Click += (_, _) => _ = TravelToAsync(e, n, flee: false);
        menu.Items.Add(item);
        return menu;
    }

    private void StopTravel(string why)
    {
        if (_travelBusy)
        {
            _travelStop = true;
            _log.Info(TravelLog, $"Arrêt du déplacement demandé ({why}).");
        }
    }

    // Trajet complet. Rend vrai si arrivé.
    private async Task<bool> TravelToAsync(double east, double north, bool flee)
    {
        if (_travelBusy && !flee)
        {
            _log.Info(TravelLog, "Déplacement refusé : un déplacement est déjà en cours.");
            return false;
        }
        if (!_settings.TravelEnabled)
        {
            _log.Info(TravelLog, "Déplacement refusé : déplacements coupés.");
            return false;
        }
        var check = CheckTravel(east, north);
        if (check is not { Allowed: true })
        {
            _log.Info(TravelLog, $"Déplacement vers N {north:0}, E {east:0} refusé : {check?.Detail}.");
            TravelStatusText.Text = $"Refusé : {check?.Detail}.";
            return false;
        }
        if (CurrentGame() is null)
        {
            _log.Info(TravelLog, $"Déplacement vers N {north:0}, E {east:0} refusé : jeu fermé.");
            TravelStatusText.Text = "Refusé : jeu fermé.";
            return false;
        }
        var wasBusy = _travelBusy;
        _travelBusy = true;
        if (!wasBusy)
        {
            _travelStop = false;
        }
        try
        {
            var start = _lastSnapshot!.Position!.Value;
            _log.Info(TravelLog, $"{(flee ? "Fuite" : "Déplacement")} vers N {north:0}, E {east:0} depuis {start.ScreenText} ({check.Detail}).");
            // v0.4.30 (T-12) : le clic du menu laisse le bot au premier plan ; attendre le retour sur le jeu (20 s au plus).
            if (_actions.WhyNot() is { } notYet)
            {
                _log.Info(TravelLog, $"En attente avant d'ouvrir la carte : {notYet}. Repassez sur le jeu (Alt + Tab, sans cliquer dedans) puis lâchez souris et clavier ; 20 s au plus.");
                TravelStatusText.Text = "Repassez sur le jeu (Alt + Tab), puis ne touchez plus à rien…";
                if (await ActionReadiness.WaitAsync(_actions.WhyNot, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(500), () => _travelStop) is { } still)
                {
                    _log.Info(TravelLog, $"Déplacement abandonné avant tout geste : {still}.");
                    TravelStatusText.Text = $"Pas parti : {still}.";
                    return false;
                }
                _log.Detail(TravelLog, "Jeu prêt : début du geste.");
            }
            TravelStatusText.Text = $"En route vers N {north:0}, E {east:0}…";
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                if (!await SetDestinationAsync(east, north))
                {
                    TravelStatusText.Text = "Arrêté (voir le journal).";
                    return false;
                }
                var departed = await WaitDepartureAsync(east, north, start);
                if (departed)
                {
                    break;
                }
                if (attempt == 2)
                {
                    _log.Alerte(TravelLog, "Arrêt : le jeu n'a pas accepté la destination (2 essais).");
                    TravelStatusText.Text = "Arrêté : destination pas acceptée.";
                    return false;
                }
                _log.Info(TravelLog, "Pas de départ en 3 s : nouvel essai (une fois).");
            }
            return await FollowAsync(east, north, start, flee);
        }
        finally
        {
            if (!wasBusy)
            {
                _travelBusy = false;
            }
        }
    }

    // Ouvre la carte, calibre, vise, clique, referme. Faux = arrêt (déjà écrit au journal).
    private async Task<bool> SetDestinationAsync(double east, double north)
    {
        var key = _settings.MapKeyScanCode;
        if (await OpenMapAsync(key) is not { } bar)
        {
            return false;
        }
        var ok = false;
        try
        {
            if (await CalibrateAsync(bar) is not { } scale)
            {
                return false;
            }
            var game = CurrentGame()!;
            var c = GameWindowLocator.GetClientArea(game.Handle);
            var area = MapClickArea(c);
            var point = scale.ToScreen(east, north);
            for (var aim = 0; aim < 3; aim++)
            {
                if (!area.Contains(point.X, point.Y))
                {
                    _log.Info(TravelLog, "Arrêt : cible hors de la carte affichée (trop loin pour le zoom actuel).");
                    return false;
                }
                if (!await MouseAsync("Déplacement : souris sur la cible", point.X, point.Y, area))
                {
                    return false;
                }
                await Task.Delay(200);
                if (await ReadBarAsync() is not { } read)
                {
                    _log.Alerte(TravelLog, "Arrêt : barre de la carte illisible pendant la visée.");
                    return false;
                }
                var en = scale.ToEastNorth(read);
                _log.Detail(TravelLog, $"Visée {aim + 1} : souris en ({point.X}, {point.Y}), barre E {en.East}, N {en.North}, cible E {east:0}, N {north:0}.");
                if (MapScale.OnTarget(en, east, north))
                {
                    if (_travelStop)
                    {
                        _log.Info(TravelLog, "Arrêt demandé avant le clic : rien cliqué.");
                        return false;
                    }
                    var clicked = await ActAsync(() => _actions.ClickAsync("Déplacement : Set destination (clic gauche)", point.X, point.Y, area, _travelMouse));
                    if (!clicked.Done)
                    {
                        _log.Info(TravelLog, $"Arrêt : clic pas fait ({clicked.Detail}).");
                        return false;
                    }
                    ok = true;
                    await Task.Delay(300);
                    return true;
                }
                point = scale.Correct(point, en, east, north);
            }
            _log.Info(TravelLog, "Arrêt : visée à plus d'une case après 3 essais, rien cliqué.");
            return false;
        }
        finally
        {
            await CloseMapAsync(key, ok);
        }
    }

    // Zone où la souris peut aller sur la grande carte : hors du panneau « Map Options » et de la boussole (à droite),
    // hors de la barre du bas, à 40 px des bords.
    private static SafeArea MapClickArea(ScreenRect c) =>
        new(c.X + 40, c.Y + 40, c.X + (int)(c.Width * 0.80), c.Y + (int)(c.Height * 0.88));

    // Zone de la barre du bas de la carte, en pixels de l'image du jeu (mesurée sur la capture du 16/11 : x 30-69 %, y 91-97,5 %).
    private static ScreenRect MapBarZone(Frame f) => WorldMapZones.BarZone(f);

    private async Task<string?> ReadBarTextAsync()
    {
        if (_ocr is null)
        {
            return null;
        }
        var frame = await Task.Run(CaptureGameNow);
        if (frame is null)
        {
            return null;
        }
        foreach (var threshold in new[] { 140, 110, 170 })
        {
            var text = await _ocr.ReadAsync(frame, MapBarZone(frame), TextStyle.DarkText, threshold);
            if (MapScale.ParseBar(text) is not null || WorldMapZones.SaysSetDestination(text))
            {
                return text;
            }
        }
        return "";
    }

    private async Task<(int A, int B)?> ReadBarAsync() => MapScale.ParseBar(await ReadBarTextAsync());

    // Ouvre la grande carte avec la touche ; vrai si « Set destination » est lu dans la barre du bas.
    private async Task<string?> OpenMapAsync(ushort key)
    {
        _travelMapAllowedUntil = DateTime.Now.AddSeconds(30);
        var pressed = await ActAsync(() => _actions.PressAsync("Déplacement : ouvrir la carte", key));
        if (!pressed.Done)
        {
            _travelMapAllowedUntil = DateTime.MinValue;
            _log.Info(TravelLog, $"Arrêt : carte pas ouverte ({pressed.Detail}).");
            return null;
        }
        // v0.4.30 (T-12) : la carte peut s'ouvrir lentement : trois lectures (0,6 s, 1 s, 1,4 s).
        string? text = null;
        for (var i = 0; i < 3 && !WorldMapZones.SaysSetDestination(text); i++)
        {
            await Task.Delay(i == 0 ? 600 : 400);
            text = await ReadBarTextAsync();
        }
        if (!WorldMapZones.SaysSetDestination(text))
        {
            var frame = await Task.Run(CaptureGameNow);
            var seen = frame is not null && GameWindows.IsWorldMap(frame);
            if (WorldMapZones.AfterKey(text, seen) == MapOpenOutcome.ARefermer)
            {
                _log.Alerte(TravelLog, $"Carte vue mais barre illisible (« {text} ») : même touche pour la refermer.");
                await SaveTravelCaptureAsync("carte-barre-illisible");
                await CloseMapAsync(key, false);
            }
            else
            {
                // Rien d'ouvert : ne pas rappuyer (un second appui ouvrirait la carte pour rien).
                _travelMapAllowedUntil = DateTime.MinValue;
                _log.Alerte(TravelLog, $"Carte non ouverte par la touche 0x{key:X2} (barre lue : « {text} ») : rien rappuyé.");
            }
            return null;
        }
        _log.Detail(TravelLog, $"Carte ouverte, barre : « {text} ».");
        return text;
    }

    private async Task CloseMapAsync(ushort key, bool afterClick)
    {
        var pressed = await ActAsync(() => _actions.PressAsync("Déplacement : fermer la carte", key));
        // v0.4.30 (T-12) : la carte peut mettre un peu de temps à disparaître : trois regards (0,5 s, 1 s, 1,5 s) avant l'alerte.
        await Task.Delay(500);
        var closed = pressed.Done && await ActionReadiness.UntilAsync(async () =>
        {
            var frame = await Task.Run(CaptureGameNow);
            return frame is null || !GameWindows.IsWorldMap(frame);
        }, 3, TimeSpan.FromMilliseconds(500));
        var stillOpen = pressed.Done && !closed;
        if (!pressed.Done || stillOpen)
        {
            _actions.Halt("grande carte du jeu peut-être restée ouverte : vérifier le jeu");
            _log.Alerte(TravelLog, $"Carte peut-être restée ouverte ({(pressed.Done ? "encore vue à l'écran" : pressed.Detail)}).");
        }
        else
        {
            _log.Detail(TravelLog, afterClick ? "Carte refermée après le clic." : "Carte refermée sans clic.");
        }
        _travelMapAllowedUntil = DateTime.Now.AddSeconds(4);
    }

    // Trois lectures : centre, 200 px à droite, 200 px en bas → échelle et ordre des nombres.
    private async Task<MapScale?> CalibrateAsync(string bar)
    {
        if (CurrentGame() is not { } game)
        {
            return null;
        }
        var c = GameWindowLocator.GetClientArea(game.Handle);
        var area = MapClickArea(c);
        var cx = c.X + c.Width / 2;
        var cy = c.Y + c.Height / 2;
        var reads = new List<(int A, int B)>();
        foreach (var (x, y) in new[] { (cx, cy), (cx + CalibrationStep, cy), (cx, cy + CalibrationStep) })
        {
            if (!await MouseAsync("Déplacement : souris pour mesurer la carte", x, y, area))
            {
                return null;
            }
            await Task.Delay(150);
            if (await ReadBarAsync() is not { } read)
            {
                _log.Alerte(TravelLog, "Arrêt : barre de la carte illisible (coordonnées sous la souris).");
                await SaveTravelCaptureAsync("barre-illisible");
                return null;
            }
            reads.Add(read);
        }
        var (scale, refusal) = MapScale.Calibrate(reads[0], reads[1], reads[2], cx, cy, CalibrationStep, CalibrationStep, _lastSnapshot?.Position);
        _log.Info(TravelLog, $"Mesure de la carte : centre ({reads[0].A}, {reads[0].B}), à droite ({reads[1].A}, {reads[1].B}), en bas ({reads[2].A}, {reads[2].B}) → "
            + (scale is { } s ? $"ordre {(s.FirstIsNorth ? "(N, E)" : "(E, N)")}, {s.CellsPerPixelX:0.000} case/pixel." : $"refus : {refusal}."));
        return scale;
    }

    private async Task<bool> MouseAsync(string label, int x, int y, SafeArea area)
    {
        var moved = await ActAsync(() => _actions.MoveAsync(label, x, y, area, _travelMouse));
        if (!moved.Done)
        {
            _log.Info(TravelLog, $"Arrêt : souris pas déplacée ({moved.Detail}).");
        }
        return moved.Done;
    }

    // Une action du contrôleur ; si seule la limite par minute bloque, attend (30 s au plus).
    private async Task<ActionResult> ActAsync(Func<Task<ActionResult>> act)
    {
        for (var i = 0; ; i++)
        {
            var result = await act();
            if (result.Done || i >= 30 || !result.Detail.Contains("actions dans la minute", StringComparison.Ordinal))
            {
                return result;
            }
            await Task.Delay(1000);
        }
    }

    // Départ confirmé dans les 3 s : position qui se rapproche de la cible ou destination du serveur plus proche.
    private async Task<bool> WaitDepartureAsync(double east, double north, Server.World.ServerPosition start)
    {
        var startLeft = Distance(start.ScreenEast, start.ScreenNorth, east, north);
        for (var i = 0; i < 6; i++)
        {
            await Task.Delay(500);
            var s = _serverReader?.Snapshot();
            if (s?.Position is not { } p)
            {
                continue;
            }
            var left = Distance(p.ScreenEast, p.ScreenNorth, east, north);
            var dest = s.Destination is { } d ? Distance(d.ScreenEast, d.ScreenNorth, east, north) : (double?)null;
            if (left < startLeft - 0.3 || (dest is { } dl && dl < startLeft - 0.5) || s.Walking == true)
            {
                _log.Info(TravelLog, $"Départ confirmé : destination du serveur {(s.Destination is { } dd ? dd.ScreenText : "—")} (écart avec la cible : {(dest is { } x ? $"{x:0.0}" : "—")} case), marche {(s.Walking == true ? "oui" : "non")}.");
                return true;
            }
        }
        return false;
    }

    private static double Distance(double e1, double n1, double e2, double n2) => Math.Sqrt((e1 - e2) * (e1 - e2) + (n1 - n2) * (n1 - n2));

    // Suivi de la marche (toutes les 500 ms) jusqu'à l'arrivée ou un arrêt.
    private async Task<bool> FollowAsync(double east, double north, Server.World.ServerPosition start, bool flee)
    {
        var distance = Distance(start.ScreenEast, start.ScreenNorth, east, north);
        var guard = new TravelGuard(east, north, distance, DateTime.Now);
        var destinations = new HashSet<string>();
        var began = DateTime.Now;
        while (true)
        {
            await Task.Delay(500);
            if (_travelStop)
            {
                _log.Info(TravelLog, "Déplacement arrêté (demande). Le personnage peut finir son pas en cours.");
                TravelStatusText.Text = "Arrêté.";
                return false;
            }
            var s = _serverReader?.Snapshot();
            if (s?.Position is not { } p)
            {
                continue;
            }
            if (s.Destination is { } d)
            {
                destinations.Add(d.ScreenText);
            }
            var state = guard.Update(DateTime.Now, p.ScreenEast, p.ScreenNorth, s.Health, _serverReader?.LastUpdate ?? DateTime.MinValue);
            switch (state)
            {
                case TravelState.EnRoute:
                    continue;
                case TravelState.Arrive:
                    var seconds = (DateTime.Now - began).TotalSeconds;
                    _log.Info(TravelLog, $"Arrivé : {guard.Detail}, {p.ScreenText}, en {seconds:0.0} s ({distance / Math.Max(0.1, seconds):0.00} case/s), {destinations.Count} destination(s) différente(s) vue(s) sur le serveur.");
                    TravelStatusText.Text = $"Arrivé ({p.ScreenText}).";
                    return true;
                case TravelState.Danger when !flee:
                    _log.Alerte(TravelLog, $"Danger : {guard.Detail}. Sprint (4) et fuite à l'opposé.");
                    await ActAsync(() => _actions.PressAsync("Déplacement : Sprint (fuite)", GameKeys.Sprint));
                    var (fe, fn) = TravelGuard.FleePoint(p.ScreenEast, p.ScreenNorth, east, north);
                    await TravelToAsync(fe, fn, flee: true);
                    _log.Info(TravelLog, "Après la fuite : arrêt, le bot ne reprend pas le trajet tout seul (v0.4.29).");
                    TravelStatusText.Text = "Arrêté après une fuite.";
                    return false;
                case TravelState.ATerre:
                    _log.Alerte(TravelLog, "À terre (vie à 0) : arrêt du trajet, recherche du bouton « Get Up » (jamais « Go Home »).");
                    await GetUpAsync();
                    TravelStatusText.Text = "Arrêté : personnage à terre.";
                    return false;
                default:
                    _log.Alerte(TravelLog, $"Arrêt : {guard.Detail}.");
                    TravelStatusText.Text = $"Arrêté : {guard.Detail}.";
                    return false;
            }
        }
    }

    // Fenêtre « à terre » : clic seulement sur « Get Up », et seulement si « Go Home » est lu ailleurs (GetUpChooser).
    private async Task GetUpAsync()
    {
        var before = _serverReader?.Snapshot()?.Position;
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(1000);
            if (_ocr is null || CurrentGame() is not { } game)
            {
                break;
            }
            var frame = await Task.Run(CaptureGameNow);
            if (frame is null)
            {
                continue;
            }
            var zone = new ScreenRect(frame.Width / 4, frame.Height / 4, frame.Width / 2, frame.Height / 2);
            var lines = new List<GetUpChooser.Word>();
            foreach (var style in new[] { TextStyle.BrightText, TextStyle.DarkText })
            {
                foreach (var (text, box) in await _ocr.ReadLinesAsync(frame, zone, style, 140))
                {
                    lines.Add(new GetUpChooser.Word(text, box.X, box.Y, box.Width, box.Height));
                }
                if (GetUpChooser.Choose(lines) is not null)
                {
                    break;
                }
                lines.Clear();
            }
            if (GetUpChooser.Choose(lines) is not { } at)
            {
                continue;
            }
            await SaveTravelCaptureAsync("a-terre");
            var c = GameWindowLocator.GetClientArea(game.Handle);
            var x = frame.Source.X + at.X;
            var y = frame.Source.Y + at.Y;
            var area = new SafeArea(c.X + c.Width / 4, c.Y + c.Height / 4, c.X + c.Width * 3 / 4, c.Y + c.Height * 3 / 4);
            var clicked = await ActAsync(() => _actions.ClickAsync("Déplacement : Get Up", x, y, area, _travelMouse));
            _log.Info(TravelLog, $"Clic sur « Get Up » en ({x}, {y}) : {clicked.Detail}.");
            await Task.Delay(5000);
            var s = _serverReader?.Snapshot();
            if (s?.Health is > 0 && s.Position is { } p && before is { } b && p.ScreenDistanceTo(b) < 3)
            {
                _log.Info(TravelLog, "Relevé sur place (Get Up réussi). Arrêt : le cargo tombé reste au sol, à vous de voir.");
                return;
            }
            if (s?.Position is { } p2 && before is { } b2 && p2.ScreenDistanceTo(b2) >= 3)
            {
                _actions.Halt("position qui a sauté après Get Up : Go Home cliqué par erreur ?");
                _log.Alerte(TravelLog, "Alerte : la position a sauté après le clic (Go Home par erreur ?). Tout est arrêté.");
                return;
            }
        }
        _log.Alerte(TravelLog, "« Get Up » pas reconnu avec certitude : rien cliqué. Relevez le personnage vous-même (Get Up, pas Go Home).");
        await SaveTravelCaptureAsync("a-terre-non-reconnu");
    }

    // Capture de la fenêtre du jeu seulement (jamais l'écran entier), dans images/analyse/ si le dépôt est trouvé.
    private async Task<string?> SaveTravelCaptureAsync(string step)
    {
        var frame = await Task.Run(CaptureGameNow);
        if (frame is null)
        {
            return null;
        }
        var folder = TravelCaptureFolder();
        Directory.CreateDirectory(folder);
        var n = Directory.GetFiles(folder, "20261127_essai-deplacement_*.png").Length + 1;
        var path = Path.Combine(folder, $"20261127_essai-deplacement_{step}_{n:000}.png");
        try
        {
            await FrameSaver.SavePngAsync(frame, path);
            _log.Detail(TravelLog, $"Capture : {Path.GetFileName(path)}.");
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string TravelCaptureFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "images", "analyse");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }
        return Path.Combine(AppContext.BaseDirectory, "captures-essai", "deplacement");
    }

    // Essai sans le jeu (T-10) : menu pour une case à 5 à l'est, puis demande de trajet → refus « jeu fermé » attendu.
    private async Task RunTravelMenuTestAsync()
    {
        for (var i = 0; i < 90 && _lastSnapshot?.Position is null; i++)
        {
            await Task.Delay(1000);
        }
        if (_lastSnapshot?.Position is not { } p)
        {
            _log.Info(TravelLog, "Essai du menu : position jamais lue.");
            return;
        }
        await Task.Delay(5000);
        foreach (var (de, dn) in new[] { (5.0, 0.0), (0.0, 5.0), (40.0, 0.0) })
        {
            var menu = BuildTravelMenu(p.ScreenEast + de, p.ScreenNorth + dn, out var check);
            _log.Info(TravelLog, $"Essai du menu : case à {de:0} E, {dn:0} N → « {string.Join(" / ", menu.Items.OfType<MenuItem>().Select(m => $"{m.Header}{(m.IsEnabled ? "" : " (grisé)")}"))} » ; contrôle : {check.Detail}.");
            if (check.Allowed)
            {
                await TravelToAsync(Math.Round(p.ScreenEast + de), Math.Round(p.ScreenNorth + dn), flee: false);
            }
        }
        _log.Info(TravelLog, "Essai du menu fini.");
    }

    // Mode d'essai pour T-11 (« --essai-deplacement ») : attend le jeu « en jeu », trouve la touche de la carte,
    // mesure l'échelle, va à 5 cases et revient. 15 min au plus. Jamais d'arbre coupé, jamais de chariot.
    private async Task RunTravelTestAsync()
    {
        var deadline = DateTime.Now.AddMinutes(15);
        _log.Info(TravelLog, "Essai du déplacement : attente du jeu (écran « en jeu », position lue, 3 s sans souris ni clavier). 15 min au plus.");
        while (DateTime.Now < deadline)
        {
            await Task.Delay(2000);
            if (CurrentGame() is not null && _lastSnapshot?.Position is not null && _actions.WhyNot() is null
                && _situation is { Place: Place.Jeu, Game: GameScreen.EnJeu })
            {
                break;
            }
        }
        if (DateTime.Now >= deadline)
        {
            _log.Info(TravelLog, "Essai du déplacement : jeu jamais prêt en 15 min, rien fait.");
            return;
        }
        try
        {
            await SaveTravelCaptureAsync("01-depart-cible-en-haut");
            // 1. Touche de la carte : 0x34 puis 0x53.
            ushort? found = null;
            foreach (var key in new[] { GameKeys.MapKey, GameKeys.MapKeyNumpad })
            {
                _log.Info(TravelLog, $"Essai : touche 0x{key:X2} pour la carte.");
                if (await OpenMapAsync(key) is not null)
                {
                    await SaveTravelCaptureAsync($"02-carte-ouverte-0x{key:X2}");
                    found = key;
                    // 2. Échelle, puis fermeture.
                    var scale = await CalibrateAsync("");
                    await CloseMapAsync(key, false);
                    await SaveTravelCaptureAsync("03-carte-fermee");
                    if (scale is null)
                    {
                        _log.Info(TravelLog, "Essai : échelle non mesurée, fin de l'essai.");
                        return;
                    }
                    break;
                }
                await SaveTravelCaptureAsync($"02-carte-pas-ouverte-0x{key:X2}");
                await Task.Delay(1500);
            }
            if (found is not { } mapKey)
            {
                _log.Info(TravelLog, "Essai : aucune des deux touches n'ouvre la carte. Fin.");
                return;
            }
            _settings.MapKeyScanCode = mapKey;
            _settings.Save(LocalSettings.DefaultPath);
            _log.Info(TravelLog, $"Essai : touche de la carte = 0x{mapKey:X2} (gardée dans les réglages).");
            // 3. Cible à 5 cases qui passe le contrôle du relief.
            var start = _lastSnapshot!.Position!.Value;
            (double E, double N)? target = null;
            foreach (var (de, dn) in new[] { (0, 5), (5, 0), (0, -5), (-5, 0), (4, 4), (4, -4), (-4, -4), (-4, 4) })
            {
                var e = Math.Round(start.ScreenEast + de);
                var n = Math.Round(start.ScreenNorth + dn);
                var check = CheckTravel(e, n);
                _log.Detail(TravelLog, $"Essai : cible E {e:0}, N {n:0} : {check?.Detail}.");
                if (check is { Allowed: true })
                {
                    target = (e, n);
                    break;
                }
            }
            if (target is not { } t)
            {
                _log.Info(TravelLog, "Essai : aucune cible à 5 cases sans escalade ni nage. Fin.");
                return;
            }
            await Task.Delay(3000);
            var went = await TravelToAsync(t.E, t.N, flee: false);
            await SaveTravelCaptureAsync("04-aller");
            if (DateTime.Now < deadline && went && !_travelStop)
            {
                await Task.Delay(3000);
                await TravelToAsync(Math.Round(start.ScreenEast), Math.Round(start.ScreenNorth), flee: false);
                await SaveTravelCaptureAsync("05-retour");
            }
            _log.Info(TravelLog, $"Essai déplacement fini : aller {(went ? "réussi" : "raté")}. Le bot rend la main.");
        }
        catch (Exception ex)
        {
            _log.Erreur(TravelLog, $"Essai du déplacement : {ex.Message}");
        }
    }
}
