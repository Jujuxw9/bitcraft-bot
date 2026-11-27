using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using BitcraftBot.Core.Logging;
using BitcraftBot.Core.Monitoring;
using BitcraftBot.Core.Safety;
using BitcraftBot.GameData;
using BitcraftBot.Input.Keyboard;
using BitcraftBot.Input.Native;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;
using BitcraftBot.Perception.Ocr;
using BitcraftBot.Perception.Screen;
using Frame = BitcraftBot.Perception.Capture.Frame;

namespace BitcraftBot.App;

// Fenêtre de supervision (D-005). Onglets (v0.3.16) : Bot, Personnage, Carte, Paramètres… (onglet Recettes retiré en v0.4.26, D-083) :
// affiche le jeu en direct, lit la cible sélectionnée à l'écran (le reste vient du serveur), gère la sécurité, écrit le journal.
// Seule action envoyée au jeu : Maj + E, si le repas automatique est coché (D-042).
public partial class MainWindow : Window
{
    private const int GraphPoints = 120;
    private static readonly TimeSpan ResourceDetailInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HudInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan LocateInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan UiInterval = TimeSpan.FromMilliseconds(250);

    private readonly string _version;
    private readonly LocalSettings _settings;
    private readonly SessionLog _log;
    private readonly SafetyMonitor _safety;
    private readonly GameWindowLocator _locator = new();
    private readonly ScreenCapturer _capturer = new();
    private readonly DispatcherTimer _uiTimer;
    private readonly HudChangeLogger _hudLogger;
    private readonly HudReader? _hudReader;
    private readonly GameDataLoader _gameData;
    private bool _gameVisible = true;
    private DateTime _lastCaptureKeyAt = DateTime.MinValue;
    private GlobalKeyboardHook? _keyboardHook;

    // Journal affiché et surveillance des ressources (onglet Système).
    private readonly LogView _logView = new();
    private readonly ResourceMonitor _resourceMonitor = new();
    private readonly ResourceGuard _resourceGuard;
    private readonly DispatcherTimer _resourceTimer;
    private readonly List<double> _cpuHistory = [];
    private readonly List<double> _ramHistory = [];
    private ResourceSample? _lastResource;
    private DateTime _lastResourceDetail = DateTime.MinValue;
    private bool _resourceSampling;

    // Banc d'essai des mouvements (D-035).
    private readonly InputSender _inputSender = new();

    private CancellationTokenSource? _sessionCts;
    private WriteableBitmap? _bitmap;
    private volatile Frame? _latestFrame;
    private int _displayPending;
    private GameWindow? _gameWindow;
    private string _lastLocateMessage = string.Empty;

    // Mesures de cadence (mises à jour par le fil de capture, lues par l'interface).
    private int _framesCounted;
    private DateTime _fpsWindowStart = DateTime.Now;
    private double _measuredFps;
    private double _captureMsTotal;
    private int _captureMsCount;
    private int _consecutiveCaptureErrors;

    // Image du jeu visible dans l'onglet Bot ? Sinon la capture tombe à 2 images/s, juste pour la lecture du HUD (v0.3.19).
    private volatile bool _imageShown = true;
    private static readonly TimeSpan HiddenCaptureInterval = TimeSpan.FromMilliseconds(500);

    public MainWindow()
    {
        InitializeComponent();

        _version = ReadVersion();
        Title = $"BitCraft Bot — v{SessionLog.ShortVersion(_version)}";
        VersionText.Text = $"Version {_version}";

        _settings = LocalSettings.Load(LocalSettings.DefaultPath);
        // v0.4.22 : seuils RAM relevés (le bot tourne vers 550-600 Mo) ; 1000 / 1600 Mo au minimum, même si un réglage plus bas était enregistré.
        if (_settings.Limits.BotRamWarningMb < BitcraftBot.Core.Monitoring.ResourceLimits.DefaultRamWarningMb || _settings.Limits.BotRamCriticalMb < BitcraftBot.Core.Monitoring.ResourceLimits.DefaultRamCriticalMb)
        {
            _settings.Limits.BotRamWarningMb = Math.Max(_settings.Limits.BotRamWarningMb, BitcraftBot.Core.Monitoring.ResourceLimits.DefaultRamWarningMb);
            _settings.Limits.BotRamCriticalMb = Math.Max(_settings.Limits.BotRamCriticalMb, BitcraftBot.Core.Monitoring.ResourceLimits.DefaultRamCriticalMb);
            _settings.Save(LocalSettings.DefaultPath);
        }
        _settings.TargetFps = Math.Clamp(_settings.TargetFps, 1, 15);
        ApplyWindowSettings();

        _log = new SessionLog(AppContext.BaseDirectory, _version);
        _log.EntryWritten += entry => Dispatcher.BeginInvoke(() => AppendLogLine(entry));
        LogList.ItemsSource = _logView.Visible;
        LogList.IsVisibleChanged += (_, _) => ScrollLogToEnd();
        _resourceGuard = new ResourceGuard(_settings.Limits);
        _hudLogger = new HudChangeLogger(_log);

        var ocr = OcrReader.TryCreate();
        _ocr = ocr;
        _hudReader = ocr is null ? null : new HudReader(ocr, KnownNames.Resources);
        _gameData = new GameDataLoader(_log);
        _gameData.Loaded += store =>
        {
            if (_hudReader is not null)
            {
                _hudReader.KnownTargetNames = store.ResourceNames();
            }
            _hudLogger.GameData = store;
        };

        // Plus de touche d'arrêt d'urgence (D-047, v0.3.19) : bouton « Arrêter », souris bougée, durée max.
        _safety = new SafetyMonitor(new SafetyOptions
        {
            MaxDurationEnabled = _settings.MaxDurationEnabled,
            MaxDuration = TimeSpan.FromMinutes(Math.Clamp(_settings.MaxDurationMinutes, 1, 240)),
        });
        _safety.StopRequested += OnStopRequested;
        _actions = CreateActionController();
        MouseStopCheck.IsChecked = _settings.StopOnMouseMove;
        MaxDurationCheck.IsChecked = _safety.Options.MaxDurationEnabled;
        MaxDurationBox.Text = ((int)_safety.Options.MaxDuration.TotalMinutes).ToString();
        TargetFpsBox.Text = _settings.TargetFps.ToString();
        ShowZonesCheck.IsChecked = _settings.ShowZones;
        AnomalyCheck.IsChecked = _settings.AnomalyStopEnabled;
        ShowLimits();

        _resourceTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _resourceTimer.Tick += async (_, _) => await OnResourceTickAsync();

        _uiTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = UiInterval };
        _uiTimer.Tick += (_, _) => OnUiTick();

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _log.Info("Session", $"Démarrage du programme. Version {_version}. Poste : {Environment.MachineName}, {Environment.OSVersion}.");
        _log.Info("Session", $"Journal : {_log.FilePath}");
        _log.Info("Session", "Version 0.4.28 : carte, affichage : carte plus grande (carrée, toute la hauteur), tableau plus étroit (famille, âge et position au survol), fond de carte calculé hors de l’interface (rapide à 200 cases), points plus petits quand on dézoome, clic droit préparé.");
        InitServerTab();
        InitTaskJournal();
        InitCamera();
        InitScreenWatch(_ocr);
        InitOverlay();
        InitInventoryColumns();
        InitStartupButtons();
        InitMeals();
        InitGpt();
        InitAbilities();
        InitResume();
        InitChatArchive();
        InitLayout();
        _gameData.Loaded += _ => Dispatcher.BeginInvoke(LoadRecipesInBackground);
        _ = _gameData.StartAsync();
        _log.Info("Système", $"Surveillance CPU/RAM active. Seuils : RAM du bot {_settings.Limits.BotRamWarningMb:0} Mo (alerte) / {_settings.Limits.BotRamCriticalMb:0} Mo (arrêt), " +
                             $"CPU du bot {_settings.Limits.BotCpuWarningPercent:0} %, RAM libre du PC {_settings.Limits.SystemRamAvailableWarningPercent:0} %.");
        _resourceTimer.Start();
        _log.Info("Perception", _hudReader is null
            ? "Lecture de texte (OCR) indisponible : installer la langue anglaise dans Windows. Le HUD ne sera pas lu."
            : "Lecture de texte (OCR) prête, en anglais.");

        try
        {
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyDown += OnGlobalKeyDown;
            _log.Info("Sécurité", $"Écoute du clavier active (capture du jeu : {CaptureKey.DisplayName} ; une touche tapée par Julien bloque les actions du bot 3 s).");
        }
        catch (InvalidOperationException ex)
        {
            _log.Erreur("Sécurité", $"Écoute du clavier impossible : {ex.Message}. Le bouton « Arrêter » reste disponible.");
        }
        _ = RunStartupAsync();
    }

    private void OnGlobalKeyDown(int scanCode, bool isExtended, bool isInjected)
    {
        if (!isInjected)
        {
            Dispatcher.BeginInvoke(NotePhysicalKey);
            if (scanCode is Input.Actions.GameKeys.Enter or Input.Actions.GameKeys.Escape)
            {
                Dispatcher.BeginInvoke(() => OnChatKey(scanCode));
            }
        }
        // W, Q, D : état de la caméra (R1). Tapées par Julien ou, plus tard, par le bot.
        // Pendant que le bot écrit dans le chat, ses lettres (simulées) ne sont pas des touches de caméra (v0.4.5).
        if (!isExtended && !(isInjected && _gptSending))
        {
            Dispatcher.BeginInvoke(() => OnCameraKey(scanCode));
        }
        // Num 0 : capture de l'écran du jeu seul (1920×1080 exact), même avec 2 écrans.
        if (!isInjected && CaptureKey.IsCapture(scanCode, isExtended))
        {
            // Seulement si le jeu est visible : sinon Julien tape un 0 ailleurs (essai v0.3.12 : 4 captures du Bloc-notes).
            var game = CurrentGame();
            if (game is null || !GameVisibility.IsUncovered(game))
            {
                _log.Info("Capture", $"Touche {CaptureKey.DisplayName} ignorée : le jeu est caché par une autre fenêtre (touche tapée dans un autre programme ?).");
                return;
            }
            // Touche maintenue : Windows répète l'appui. Une seule capture par seconde (essai v0.3.16 : deux fois le même fichier).
            if (DateTime.Now - _lastCaptureKeyAt < TimeSpan.FromSeconds(1))
            {
                return;
            }
            _lastCaptureKeyAt = DateTime.Now;
            _ = Task.Run(SaveGameScreenshotAsync);
        }
    }

    // Capture du jeu seul, sans compression, rangée dans images/analyse du dépôt (sinon dans « captures » à côté du .exe).
    private async Task SaveGameScreenshotAsync()
    {
        try
        {
            var game = new GameWindowLocator().Find();
            if (game is null)
            {
                _log.Alerte("Capture", $"Touche {CaptureKey.DisplayName} : fenêtre du jeu introuvable, pas de capture.");
                return;
            }
            var frame = new ScreenCapturer().Capture(game.ClientArea);
            var folder = FindAnalysisFolder() ?? System.IO.Path.Combine(AppContext.BaseDirectory, "captures");
            var path = System.IO.Path.Combine(folder, $"{DateTime.Now:yyyyMMdd}_analyse_capture-bot_{DateTime.Now:HHmmss}.png");
            await FrameSaver.SavePngAsync(frame, path);
            _log.Info("Capture", $"Capture du jeu enregistrée ({frame.Width}×{frame.Height}) : {path}");
        }
        catch (Exception ex)
        {
            _log.Erreur("Capture", $"Capture impossible : {ex.Message}");
        }
    }

    // Dossier images/analyse du dépôt, en remontant depuis le .exe (artifacts/release/ ou artifacts/old/vX.Y.Z/).
    private static string? FindAnalysisFolder()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "BitcraftBot.sln")))
        {
            dir = dir.Parent;
        }
        return dir is null ? null : System.IO.Path.Combine(dir.FullName, "images", "analyse");
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) => StartSession();

    private void StopButton_Click(object sender, RoutedEventArgs e) => _safety.RequestStop(StopReason.Manuel);

    private void StartSession()
    {
        ReadInputs();
        _safety.Start();
        _hudLogger.Reset();
        _gameVisible = true;
        _consecutiveCaptureErrors = 0;
        _framesCounted = 0;
        _captureMsTotal = 0;
        _captureMsCount = 0;
        _fpsWindowStart = DateTime.Now;
        _latestFrame = null;
        _gameWindow = null;
        _lastLocateMessage = string.Empty;

        _sessionCts = new CancellationTokenSource();
        var token = _sessionCts.Token;
        var fps = _settings.TargetFps;
        // Fil à lui (v0.3.20) : la capture ne dépend plus des fils partagés, qu'une autre tâche peut occuper.
        new Thread(() => CaptureLoop(fps, token)) { IsBackground = true, Name = "Capture du jeu" }.Start();
        if (_hudReader is not null)
        {
            _ = Task.Run(() => HudLoopAsync(_hudReader, token), token);
        }
        _uiTimer.Start();

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        _actions.Resume("« Démarrer »");
        SetStatus("EN COURS", "Lecture de l'écran", "#1B4D33");
        // Réglages réellement appliqués à cette session (D-031).
        _log.Info("Session", $"Session démarrée. Réglages : arrêt si la souris bouge {(_actions.Options.StopOnMouseMove ? "actif" : "désactivé")}, " +
                             $"durée max {(_safety.Options.MaxDurationEnabled ? $"{_safety.Options.MaxDuration.TotalMinutes:0} min" : "désactivée")}, " +
                             $"{fps} images/s visées, lecture du HUD toutes les {HudInterval.TotalMilliseconds:0} ms, " +
                             $"arrêt sur anomalie {(_settings.AnomalyStopEnabled ? "actif" : "désactivé")}.");
        if (_lastResource is { } r)
        {
            _log.Info("Système", $"Au démarrage : {DescribeResources(r)}.");
        }
    }

    // Boucle de capture : cadence fixe, compensée (un retard n'est pas cumulé d'une image à l'autre).
    // Image cachée (autre onglet, fenêtre réduite) : 2 images/s seulement, juste pour la lecture du HUD.
    private void CaptureLoop(int fps, CancellationToken token)
    {
        var period = TimeSpan.FromSeconds(1.0 / fps);
        var clock = Stopwatch.StartNew();
        var next = TimeSpan.Zero;
        var lastLocate = DateTime.MinValue;
        var lastCapture = DateTime.MinValue;
        while (!token.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (_imageShown || now - lastCapture >= HiddenCaptureInterval - TimeSpan.FromMilliseconds(20))
            {
                lastCapture = now;
                CaptureOnce(now, ref lastLocate);
            }
            next += period;
            var wait = next - clock.Elapsed;
            if (wait < TimeSpan.Zero)
            {
                // En retard (PC chargé) : on repart de maintenant, sans rattraper les images manquées.
                next = clock.Elapsed;
                wait = TimeSpan.Zero;
            }
            token.WaitHandle.WaitOne(wait);
        }
    }

    private void CaptureOnce(DateTime now, ref DateTime lastLocate)
    {
        if (_gameWindow is null || now - lastLocate >= LocateInterval)
        {
            var found = _locator.Find();
            lastLocate = now;
            _ = Dispatcher.BeginInvoke(() => OnGameLocated(found));
            _gameWindow = found;
        }
        var area = _gameWindow?.ClientArea ?? GameWindowLocator.PrimaryScreen();
        var watch = Stopwatch.StartNew();
        try
        {
            var frame = _capturer.Capture(area);
            _captureMsTotal += watch.Elapsed.TotalMilliseconds;
            _captureMsCount++;
            _latestFrame = frame;
            _framesCounted++;
            _consecutiveCaptureErrors = 0;
            // Une seule image en attente d'affichage : si l'interface est en retard, on saute des images.
            if (_imageShown && Interlocked.Exchange(ref _displayPending, 1) == 0)
            {
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Render, () => ShowFrame(frame));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            var errors = ++_consecutiveCaptureErrors;
            if (errors == 1 || errors % 25 == 0)
            {
                _log.Erreur("Capture", $"Échec de capture ({errors} de suite) : {ex.Message}");
            }
        }
    }

    // Boucle de lecture du HUD : 2 fois par seconde, sur la dernière image capturée.
    private async Task HudLoopAsync(HudReader reader, CancellationToken token)
    {
        using var timer = new PeriodicTimer(HudInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                var frame = _latestFrame;
                if (frame is null)
                {
                    continue;
                }
                if (frame.Width != HudLayout.ReferenceWidth || frame.Height != HudLayout.ReferenceHeight)
                {
                    continue;
                }
                // Jeu caché par une autre fenêtre (ou introuvable) : on ne lit pas l'écran, pour ne pas lire
                // (ni enregistrer en image) une autre fenêtre de Julien. Cliquer sur le bot ne cache pas le jeu.
                var visible = _gameWindow is { } gw && GameVisibility.IsUncovered(gw);
                if (visible != _gameVisible)
                {
                    _gameVisible = visible;
                    _log.Info("Perception", visible
                        ? "Jeu de nouveau visible : lecture de l'écran reprise."
                        : "Jeu caché par une autre fenêtre (ou introuvable) : lecture de l'écran en pause, sans alerte ni image.");
                }
                if (!visible)
                {
                    continue;
                }
                // Accueil, connexion, chargement : pas de cible à lire (v0.4.1).
                if (_situation is { Place: Place.Jeu, Game: not GameScreen.EnJeu })
                {
                    continue;
                }
                try
                {
                    var hud = await reader.ReadAsync(frame);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    _hudLogger.OnHudRead(hud, _measuredFps);
                    _ = Dispatcher.BeginInvoke(() => ShowHud(hud));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.Erreur("Perception", $"Échec de lecture du HUD : {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Fin normale de session.
        }
    }

    private void OnStopRequested(StopReason reason)
    {
        _sessionCts?.Cancel();
        _uiTimer.Stop();
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        var description = SafetyMonitor.Describe(reason);
        var elapsed = FormatDuration(_safety.LastSessionDuration);
        var captureMs = _captureMsCount > 0 ? _captureMsTotal / _captureMsCount : 0;

        SetStatus("ARRÊTÉ", description, "#1F2F45");
        _log.Info("Session", $"Session arrêtée après {elapsed} : {description}.");
        _log.Info("Capture", $"Bilan : {_measuredFps:0.0} images/s mesurées, capture moyenne {captureMs:0} ms par image.");
        if (_lastResource is { } r)
        {
            _log.Info("Système", $"À l'arrêt : {DescribeResources(r)}.");
        }
    }

    private void OnUiTick()
    {
        _imageShown = Tabs.SelectedIndex == BotTab && WindowState != WindowState.Minimized && IsVisible;
        _safety.Tick();
        if (!_safety.IsRunning)
        {
            return;
        }
        ElapsedText.Text = _safety.Remaining is { } remaining
            ? $"{FormatDuration(_safety.Elapsed)} (reste {FormatDuration(remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining)})"
            : FormatDuration(_safety.Elapsed);

        var now = DateTime.Now;
        var window = now - _fpsWindowStart;
        // Mesure sur 5 s pour lisser les écarts d'une image.
        if (window >= TimeSpan.FromSeconds(5))
        {
            _measuredFps = _framesCounted / window.TotalSeconds;
            var captureMs = _captureMsCount > 0 ? _captureMsTotal / _captureMsCount : 0;
            FpsText.Text = $"{_measuredFps:0.0} (capture {captureMs:0} ms)";
            _framesCounted = 0;
            _fpsWindowStart = now;
        }
    }

    private void OnGameLocated(GameWindow? found)
    {
        string message;
        if (found is null)
        {
            message = "Fenêtre du jeu pas trouvée (jeu fermé ou réduit) : capture de l'écran principal à la place.";
            GameWindowText.Text = "○ Jeu introuvable";
        }
        else
        {
            message = $"Fenêtre du jeu trouvée : « {found.Title} » (processus {found.ProcessName}), zone {found.ClientArea}.";
            GameWindowText.Text = $"● Jeu {found.ClientArea.Width}×{found.ClientArea.Height}";
            if (found.ClientArea.Width != HudLayout.ReferenceWidth || found.ClientArea.Height != HudLayout.ReferenceHeight)
            {
                message += " Attention : la taille attendue est 1920×1080 (D-006), le HUD ne sera pas lu.";
            }
        }
        if (message != _lastLocateMessage)
        {
            // On n'écrit dans le journal que quand la situation change.
            if (found is null)
            {
                // Pas une alerte : la ligne « [Écran] » dit déjà où est Julien (bureau, autre programme, jeu réduit).
                _log.Info("Capture", message);
            }
            else
            {
                _log.Info("Capture", message);
            }
            _lastLocateMessage = message;
        }
    }

    private void ShowFrame(Frame frame)
    {
        Interlocked.Exchange(ref _displayPending, 0);
        if (!_safety.IsRunning)
        {
            return;
        }
        if (_bitmap is null || _bitmap.PixelWidth != frame.Width || _bitmap.PixelHeight != frame.Height)
        {
            _bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
            GameImage.Source = _bitmap;
            FrameHost.Width = frame.Width;
            FrameHost.Height = frame.Height;
        }
        _bitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), frame.Pixels, frame.Stride, 0);
        NoImageText.Visibility = Visibility.Collapsed;
    }

    private void ShowHud(HudState hud)
    {
        if (!_safety.IsRunning)
        {
            return;
        }
        RememberHudTarget(hud);
        if (hud.Target is null)
        {
            HudTarget.Text = "aucune";
            HudTargetHealth.Text = "—";
        }
        else
        {
            HudTarget.Text = $"{hud.Target.Name ?? "?"} ({hud.Target.Category ?? "?"})";
            HudTargetHealth.Text = hud.Target.Health is not null ? $"{hud.Target.Health}/{hud.Target.MaxHealth}" : "?";
        }
        DrawZones(hud);
    }

    // Dessine les zones lues sur l'image : vert = lu, orange = pas lu.
    private void DrawZones(HudState hud)
    {
        OverlayCanvas.Children.Clear();
        if (ShowZonesCheck.IsChecked != true || _bitmap is null)
        {
            return;
        }
        foreach (var (name, rect) in HudLayout.AllZones)
        {
            if (hud.Target is null)
            {
                continue;
            }
            var ok = hud.ZonesRead.Contains(name);
            var color = ok ? Color.FromRgb(61, 245, 138) : Color.FromRgb(255, 165, 0);
            // Trait fin (demande de Julien, v0.3.16) : 2 px de l'image, juste autour de la zone lue.
            var box = new Rectangle
            {
                Width = rect.Width + 4,
                Height = rect.Height + 4,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
            };
            Canvas.SetLeft(box, rect.X - 2);
            Canvas.SetTop(box, rect.Y - 2);
            OverlayCanvas.Children.Add(box);
        }
    }

    private void ShowZonesCheck_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowZones = ShowZonesCheck.IsChecked == true;
        if (!_settings.ShowZones)
        {
            OverlayCanvas.Children.Clear();
        }
    }

    private void SafetyCheck_Click(object sender, RoutedEventArgs e)
    {
        ReadInputs();
        _log.Info("Config", $"Sécurité modifiée : arrêt si la souris bouge {(_actions.Options.StopOnMouseMove ? "actif" : "désactivé")}, " +
                            $"durée max {(_safety.Options.MaxDurationEnabled ? $"{_safety.Options.MaxDuration.TotalMinutes:0} min" : "désactivée")}, " +
                            $"arrêt sur anomalie {(_settings.AnomalyStopEnabled ? "actif" : "désactivé")}.");
    }

    private void MaxDurationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var before = _safety.Options.MaxDuration;
        ReadInputs();
        if (_safety.Options.MaxDuration != before)
        {
            _log.Info("Config", $"Durée max modifiée : {_safety.Options.MaxDuration.TotalMinutes:0} min.");
        }
    }

    private void TargetFpsBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var before = _settings.TargetFps;
        ReadInputs();
        if (_settings.TargetFps != before)
        {
            _log.Info("Config", $"Images/s visées : {_settings.TargetFps} (appliqué au prochain démarrage).");
        }
    }

    private void ReadInputs()
    {
        _settings.StopOnMouseMove = MouseStopCheck.IsChecked == true;
        _actions.Options.StopOnMouseMove = _settings.StopOnMouseMove;
        _safety.Options.MaxDurationEnabled = MaxDurationCheck.IsChecked == true;
        _settings.AnomalyStopEnabled = AnomalyCheck.IsChecked == true;
        if (int.TryParse(MaxDurationBox.Text, out var minutes) && minutes is >= 1 and <= 240)
        {
            _safety.Options.MaxDuration = TimeSpan.FromMinutes(minutes);
        }
        else
        {
            MaxDurationBox.Text = ((int)_safety.Options.MaxDuration.TotalMinutes).ToString();
        }
        if (int.TryParse(TargetFpsBox.Text, out var fps) && fps is >= 1 and <= 15)
        {
            _settings.TargetFps = fps;
        }
        else
        {
            TargetFpsBox.Text = _settings.TargetFps.ToString();
        }
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = System.IO.Path.GetDirectoryName(_log.FilePath)!;
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    private void AppendLogLine(LogEntry entry)
    {
        var line = _logView.Add(entry);
        if (_logView.Visible.Count > 0 && ReferenceEquals(_logView.Visible[^1], line))
        {
            ScrollLogToEnd();
        }
        // Bandeau du bas de l’onglet Bot : dernier événement (sauf les détails).
        if (entry.Level != LogLevel.Detail)
        {
            LastEventText.Text = line.Text;
            LastEventText.Foreground = line.Color;
        }
        AlertCounters.Text = $"Alertes : {_logView.AlertCount}   Erreurs : {_logView.ErrorCount}";
    }

    private void LogFilter_Click(object sender, RoutedEventArgs e)
    {
        _logView.SetVisible(LogLevel.Erreur, FilterErreur.IsChecked == true);
        _logView.SetVisible(LogLevel.Alerte, FilterAlerte.IsChecked == true);
        _logView.SetVisible(LogLevel.Info, FilterInfo.IsChecked == true);
        _logView.SetVisible(LogLevel.Detail, FilterDetail.IsChecked == true);
        ScrollLogToEnd();
    }

    // Le journal montre toujours la dernière ligne (en bas) : à chaque ajout, après un filtre,
    // et à l'ouverture de l'onglet (ScrollIntoView ne fait rien tant que la liste est cachée).
    private void ScrollLogToEnd()
    {
        if (_logScrollPending)
        {
            return;
        }
        _logScrollPending = true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
        {
            _logScrollPending = false;
            if (LogList.Items.Count > 0 && LogList.IsVisible)
            {
                LogList.ScrollIntoView(LogList.Items[^1]);
            }
        });
    }

    private bool _logScrollPending;

    // Mesure CPU/RAM chaque seconde, compare aux seuils, met à jour l'onglet Système.
    private async Task OnResourceTickAsync()
    {
        if (_resourceSampling)
        {
            return;
        }
        _resourceSampling = true;
        try
        {
            var sample = await Task.Run(_resourceMonitor.Sample);
            _lastResource = sample;
            foreach (var ev in _resourceGuard.Evaluate(sample))
            {
                switch (ev.Level)
                {
                    case ResourceLevel.Critique:
                        _log.Erreur("Système", ev.Message);
                        if (_safety.IsRunning && _settings.AnomalyStopEnabled)
                        {
                            _safety.RequestStop(StopReason.Anomalie);
                        }
                        break;
                    case ResourceLevel.Alerte:
                        _log.Alerte("Système", ev.Message);
                        break;
                    default:
                        _log.Info("Système", ev.Message);
                        break;
                }
            }
            if (sample.At - _lastResourceDetail >= ResourceDetailInterval)
            {
                _log.Detail("Système", DescribeResources(sample));
                _lastResourceDetail = sample.At;
            }
            ShowResources(sample);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _log.Erreur("Système", $"Mesure CPU/RAM impossible : {ex.Message}");
        }
        finally
        {
            _resourceSampling = false;
        }
    }

    // Dernière mesure CPU/RAM (reprise par l'overlay, v0.4.23).
    private ResourceSample? _lastResourceSample;

    private void ShowResources(ResourceSample s)
    {
        _lastResourceSample = s;
        BotCpuText.Text = $"Bot : CPU {s.BotCpuPercent:0.0} %";
        BotRamText.Text = $"Bot : RAM {s.BotRamMb:0} Mo";
        GameCpuText.Text = s.GameRamMb is { } gr ? $"Jeu : CPU {s.GameCpuPercent ?? 0:0} %, RAM {gr / 1024:0.0} Go" : "Jeu : fermé";
        SystemCpuText.Text = $"PC : CPU {s.SystemCpuPercent:0} %, RAM libre {s.SystemRamAvailableMb / 1024:0.0} Go ({s.SystemRamAvailablePercent:0} %)";
        ResourceStateText.Text = _resourceGuard.BotRamLevel switch
        {
            ResourceLevel.Critique => "CRITIQUE (RAM du bot)",
            ResourceLevel.Alerte => "Alerte (RAM du bot)",
            _ => "Normal",
        };
        ResourceStateText.Foreground = _resourceGuard.BotRamLevel switch
        {
            ResourceLevel.Critique => Theme("Bad"),
            ResourceLevel.Alerte => Theme("Warn"),
            _ => Theme("Good"),
        };

        Push(_cpuHistory, s.BotCpuPercent);
        Push(_ramHistory, s.BotRamMb);
        DrawGraph(BotCpuGraph, _cpuHistory, 100, null);
        DrawGraph(BotRamGraph, _ramHistory, Math.Max(_settings.Limits.BotRamCriticalMb, _ramHistory.Max()) * 1.1,
            _settings.Limits.BotRamWarningMb);
    }

    private static void Push(List<double> history, double value)
    {
        history.Add(value);
        if (history.Count > GraphPoints)
        {
            history.RemoveAt(0);
        }
    }

    // Petit graphique : courbe des 2 dernières minutes, et ligne du seuil d'alerte s'il y en a un.
    private static void DrawGraph(Canvas canvas, List<double> values, double max, double? warning)
    {
        canvas.Children.Clear();
        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        if (width <= 0 || height <= 0 || values.Count < 2 || max <= 0)
        {
            return;
        }
        if (warning is { } w)
        {
            var y = height - w / max * height;
            canvas.Children.Add(new Line { X1 = 0, X2 = width, Y1 = y, Y2 = y, Stroke = new SolidColorBrush(Color.FromRgb(210, 182, 93)), StrokeThickness = 1, StrokeDashArray = [4, 3] });
        }
        var line = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(79, 209, 197)), StrokeThickness = 1.5 };
        for (var i = 0; i < values.Count; i++)
        {
            var x = width * i / (GraphPoints - 1);
            var y = height - Math.Clamp(values[i] / max, 0, 1) * height;
            line.Points.Add(new Point(x, y));
        }
        canvas.Children.Add(line);
    }

    private static string DescribeResources(ResourceSample s) =>
        $"bot CPU {s.BotCpuPercent:0.0} %, RAM {s.BotRamMb:0} Mo ; " +
        $"jeu {(s.GameRamMb is { } g ? $"CPU {s.GameCpuPercent ?? 0:0.0} %, RAM {g:0} Mo" : "non lancé")} ; " +
        $"PC CPU {s.SystemCpuPercent:0} %, RAM libre {s.SystemRamAvailableMb / 1024:0.0} Go ({s.SystemRamAvailablePercent:0} %)";

    private void ShowLimits()
    {
        var l = _settings.Limits;
        RamWarningBox.Text = l.BotRamWarningMb.ToString("0");
        RamCriticalBox.Text = l.BotRamCriticalMb.ToString("0");
        CpuWarningBox.Text = l.BotCpuWarningPercent.ToString("0");
        SystemRamWarningBox.Text = l.SystemRamAvailableWarningPercent.ToString("0");
    }

    private void LimitBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var l = _settings.Limits;
        var before = (l.BotRamWarningMb, l.BotRamCriticalMb, l.BotCpuWarningPercent, l.SystemRamAvailableWarningPercent);
        if (double.TryParse(RamWarningBox.Text, out var warn) && warn is >= 50 and <= 16000) l.BotRamWarningMb = warn;
        if (double.TryParse(RamCriticalBox.Text, out var crit) && crit > l.BotRamWarningMb && crit <= 32000) l.BotRamCriticalMb = crit;
        if (double.TryParse(CpuWarningBox.Text, out var cpu) && cpu is >= 1 and <= 100) l.BotCpuWarningPercent = cpu;
        if (double.TryParse(SystemRamWarningBox.Text, out var sys) && sys is >= 1 and <= 90) l.SystemRamAvailableWarningPercent = sys;
        ShowLimits();
        if (before != (l.BotRamWarningMb, l.BotRamCriticalMb, l.BotCpuWarningPercent, l.SystemRamAvailableWarningPercent))
        {
            _log.Info("Config", $"Seuils modifiés : RAM du bot {l.BotRamWarningMb:0}/{l.BotRamCriticalMb:0} Mo, CPU du bot {l.BotCpuWarningPercent:0} %, RAM libre du PC {l.SystemRamAvailableWarningPercent:0} %.");
        }
    }

    private void SetStatus(string title, string detail, string color)
    {
        StatusText.Text = title;
        StatusDetail.Text = detail;
        StatusBorder.Background = (Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private void ApplyWindowSettings()
    {
        Width = Math.Max(MinWidth, _settings.WindowWidth);
        Height = Math.Max(MinHeight, _settings.WindowHeight);
        if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop)
            && IsOnScreen(_settings.WindowLeft, _settings.WindowTop))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        // Toujours agrandie (demande de Julien, 2026-11-18), sur l'écran où elle se trouvait
        // (la position ci-dessus choisit l'écran).
        SourceInitialized += (_, _) => WindowState = WindowState.Maximized;
    }

    // Évite de rouvrir la fenêtre hors de l'écran (par exemple si le 2e écran est débranché).
    private static bool IsOnScreen(double left, double top) =>
        left >= SystemParameters.VirtualScreenLeft - 50
        && top >= SystemParameters.VirtualScreenTop - 50
        && left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100
        && top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100;

    // Fermeture (demande de Julien, v0.3.18) : le bot se déconnecte TOUJOURS du serveur avant de fermer la fenêtre.
    // 1er passage : on annule la fermeture, on se déconnecte (en attendant la fin), puis on referme.
    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_closeReady)
        {
            e.Cancel = true;
            if (_closeStarted)
            {
                return;
            }
            _closeStarted = true;
            IsEnabled = false;
            Title += " — déconnexion…";
            if (_safety.IsRunning)
            {
                _safety.RequestStop(StopReason.Manuel);
            }
            _actions.Halt("fermeture du bot");
            // v0.4.27 : la mémoire des ressources est écrite une dernière fois avant de fermer.
            await Task.Run(SaveResourceMemory);
            await CloseServerReaderAsync();
            _closeReady = true;
            _ = Dispatcher.BeginInvoke(Close);
            return;
        }
        _keyboardHook?.Dispose();
        CloseChatArchive();
        CloseCamera();
        CloseScreenWatch();
        _resourceTimer.Stop();
        CloseOverlay();

        // Position « normale » de la fenêtre (même si elle est agrandie), pour retrouver le bon écran.
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (!bounds.IsEmpty)
        {
            _settings.WindowLeft = bounds.Left;
            _settings.WindowTop = bounds.Top;
            _settings.WindowWidth = bounds.Width;
            _settings.WindowHeight = bounds.Height;
        }
        _settings.MaxDurationEnabled = _safety.Options.MaxDurationEnabled;
        _settings.MaxDurationMinutes = (int)_safety.Options.MaxDuration.TotalMinutes;
        _settings.Save(LocalSettings.DefaultPath);

        _log.Info("Session", "Fermeture du programme.");
        _log.Dispose();
    }

    private static string ReadVersion() =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"mm\:ss");
}
