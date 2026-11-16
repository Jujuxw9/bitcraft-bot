using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BitcraftBot.Core.Logging;
using BitcraftBot.Core.Safety;
using BitcraftBot.Input.Keyboard;
using BitcraftBot.Perception.Capture;

namespace BitcraftBot.App;

// Fenêtre de supervision (D-005), version 0.1 :
// affiche le jeu en direct, gère l'arrêt d'urgence et la durée max, écrit le journal.
// Aucune action n'est envoyée au jeu dans cette version.
public partial class MainWindow : Window
{
    private const int MaxLogLines = 500;
    private static readonly TimeSpan CaptureInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan LocateInterval = TimeSpan.FromSeconds(2);

    private readonly string _version;
    private readonly LocalSettings _settings;
    private readonly SessionLog _log;
    private readonly SafetyMonitor _safety;
    private readonly GameWindowLocator _locator = new();
    private readonly ScreenCapturer _capturer = new();
    private readonly DispatcherTimer _timer;
    private GlobalKeyboardHook? _keyboardHook;

    private WriteableBitmap? _bitmap;
    private GameWindow? _gameWindow;
    private DateTime _lastLocate = DateTime.MinValue;
    private string _lastLocateMessage = string.Empty;
    private int _framesThisSecond;
    private DateTime _fpsWindowStart = DateTime.Now;
    private int _consecutiveCaptureErrors;

    public MainWindow()
    {
        InitializeComponent();

        _version = ReadVersion();
        Title = $"BitCraft Bot — v{SessionLog.ShortVersion(_version)}";
        VersionText.Text = $"Version {_version}";

        _settings = LocalSettings.Load(LocalSettings.DefaultPath);
        ApplyWindowSettings();

        _log = new SessionLog(AppContext.BaseDirectory, _version);
        _log.EntryWritten += entry => Dispatcher.BeginInvoke(() => AppendLogLine(entry));

        _safety = new SafetyMonitor(new SafetyOptions
        {
            EmergencyKeyEnabled = _settings.EmergencyKeyEnabled,
            MaxDurationEnabled = _settings.MaxDurationEnabled,
            MaxDuration = TimeSpan.FromMinutes(Math.Clamp(_settings.MaxDurationMinutes, 1, 240)),
        });
        _safety.StopRequested += OnStopRequested;
        EmergencyKeyCheck.IsChecked = _safety.Options.EmergencyKeyEnabled;
        MaxDurationCheck.IsChecked = _safety.Options.MaxDurationEnabled;
        MaxDurationBox.Text = ((int)_safety.Options.MaxDuration.TotalMinutes).ToString();

        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = CaptureInterval };
        _timer.Tick += (_, _) => OnTick();

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _log.Info("Session", $"Démarrage du programme. Version {_version}. Poste : {Environment.MachineName}, {Environment.OSVersion}.");
        _log.Info("Session", $"Journal : {_log.FilePath}");
        _log.Info("Config", $"Sécurité : touche d'urgence {(_safety.Options.EmergencyKeyEnabled ? "active" : "désactivée")}, " +
                            $"durée max {(_safety.Options.MaxDurationEnabled ? $"{_safety.Options.MaxDuration.TotalMinutes:0} min" : "désactivée")}.");
        _log.Info("Session", "Version 0.1 : affichage seul, aucune action sur le jeu.");

        try
        {
            _keyboardHook = new GlobalKeyboardHook();
            _keyboardHook.KeyDown += OnGlobalKeyDown;
            _log.Info("Sécurité", $"Écoute du clavier active (arrêt d'urgence : {EmergencyKey.DisplayName}).");
        }
        catch (InvalidOperationException ex)
        {
            _log.Erreur("Sécurité", $"Écoute du clavier impossible : {ex.Message}. Le bouton « Arrêter » reste disponible.");
        }
    }

    private void OnGlobalKeyDown(int scanCode, bool isExtended, bool isInjected)
    {
        // Une touche simulée par un programme ne compte jamais comme arrêt d'urgence.
        if (isInjected || !EmergencyKey.IsEmergencyStop(scanCode, isExtended))
        {
            return;
        }
        Dispatcher.BeginInvoke(() =>
        {
            if (_safety.IsRunning)
            {
                if (_safety.Options.EmergencyKeyEnabled)
                {
                    _safety.OnEmergencyKey();
                }
                else
                {
                    _log.Alerte("Sécurité", $"Touche {EmergencyKey.DisplayName} pressée, mais l'arrêt d'urgence est désactivé.");
                }
            }
            else
            {
                _log.Info("Sécurité", $"Touche {EmergencyKey.DisplayName} pressée (aucune session en cours).");
            }
        });
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) => StartSession();

    private void StopButton_Click(object sender, RoutedEventArgs e) => _safety.RequestStop(StopReason.Manuel);

    private void StartSession()
    {
        ReadSafetyInputs();
        _safety.Start();
        _lastLocate = DateTime.MinValue;
        _consecutiveCaptureErrors = 0;
        _framesThisSecond = 0;
        _fpsWindowStart = DateTime.Now;
        _timer.Start();

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        SetStatus("EN COURS", "Capture du jeu en direct. Aucune action envoyée au jeu.", "#1E4620");
        _log.Info("Session", "Session démarrée.");
    }

    private void OnStopRequested(StopReason reason)
    {
        _timer.Stop();
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        var description = SafetyMonitor.Describe(reason);
        var elapsed = FormatDuration(_safety.LastSessionDuration);

        if (reason == StopReason.ToucheUrgence)
        {
            SetStatus("ARRÊT D'URGENCE", $"Tout est arrêté ({description}).", "#8B1A1A");
            _log.Alerte("Sécurité", $"Session arrêtée après {elapsed} : {description}.");
        }
        else
        {
            SetStatus("ARRÊTÉ", $"Session terminée : {description}.", "#3C3F44");
            _log.Info("Session", $"Session arrêtée après {elapsed} : {description}.");
        }
    }

    private void OnTick()
    {
        _safety.Tick();
        if (!_safety.IsRunning)
        {
            return;
        }

        var now = DateTime.Now;
        if (_gameWindow is null || now - _lastLocate >= LocateInterval)
        {
            LocateGame();
            _lastLocate = now;
        }

        var area = _gameWindow?.ClientArea ?? GameWindowLocator.PrimaryScreen();
        try
        {
            var frame = _capturer.Capture(area);
            ShowFrame(frame);
            _consecutiveCaptureErrors = 0;
            _framesThisSecond++;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            _consecutiveCaptureErrors++;
            if (_consecutiveCaptureErrors == 1 || _consecutiveCaptureErrors % 25 == 0)
            {
                _log.Erreur("Capture", $"Échec de capture ({_consecutiveCaptureErrors} de suite) : {ex.Message}");
            }
        }

        UpdateCounters(now);
    }

    private void LocateGame()
    {
        var found = _locator.Find();
        string message;
        if (found is null)
        {
            message = "Jeu introuvable : capture de l'écran principal à la place.";
            GameWindowText.Text = "Fenêtre du jeu : introuvable (capture de l'écran principal).";
        }
        else
        {
            message = $"Fenêtre du jeu trouvée : « {found.Title} » (processus {found.ProcessName}), zone {found.ClientArea}.";
            GameWindowText.Text = $"« {found.Title} »\n{found.ClientArea}";
            if (found.ClientArea.Width != 1920 || found.ClientArea.Height != 1080)
            {
                message += " Attention : la taille attendue est 1920×1080 (D-006).";
            }
        }
        _gameWindow = found;
        if (message != _lastLocateMessage)
        {
            // On n'écrit dans le journal que quand la situation change.
            if (found is null)
            {
                _log.Alerte("Capture", message);
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
        if (_bitmap is null || _bitmap.PixelWidth != frame.Width || _bitmap.PixelHeight != frame.Height)
        {
            _bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
            GameImage.Source = _bitmap;
        }
        _bitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), frame.Pixels, frame.Stride, 0);
        NoImageText.Visibility = Visibility.Collapsed;
    }

    private void UpdateCounters(DateTime now)
    {
        ElapsedText.Text = $"Durée : {FormatDuration(_safety.Elapsed)}";
        RemainingText.Text = _safety.Remaining is { } remaining
            ? $"Restant : {FormatDuration(remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining)}"
            : "Restant : illimité";

        var window = now - _fpsWindowStart;
        if (window >= TimeSpan.FromSeconds(1))
        {
            FpsText.Text = $"Images par seconde : {_framesThisSecond / window.TotalSeconds:0.0}";
            _framesThisSecond = 0;
            _fpsWindowStart = now;
        }
    }

    private void SafetyCheck_Click(object sender, RoutedEventArgs e)
    {
        ReadSafetyInputs();
        _log.Info("Config", $"Sécurité modifiée : touche d'urgence {(_safety.Options.EmergencyKeyEnabled ? "active" : "désactivée")}, " +
                            $"durée max {(_safety.Options.MaxDurationEnabled ? $"{_safety.Options.MaxDuration.TotalMinutes:0} min" : "désactivée")}.");
    }

    private void MaxDurationBox_LostFocus(object sender, RoutedEventArgs e) => ReadSafetyInputs();

    private void ReadSafetyInputs()
    {
        _safety.Options.EmergencyKeyEnabled = EmergencyKeyCheck.IsChecked == true;
        _safety.Options.MaxDurationEnabled = MaxDurationCheck.IsChecked == true;
        if (int.TryParse(MaxDurationBox.Text, out var minutes) && minutes is >= 1 and <= 240)
        {
            _safety.Options.MaxDuration = TimeSpan.FromMinutes(minutes);
        }
        else
        {
            MaxDurationBox.Text = ((int)_safety.Options.MaxDuration.TotalMinutes).ToString();
        }
    }

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = Path.GetDirectoryName(_log.FilePath)!;
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    private void AppendLogLine(LogEntry entry)
    {
        var text = $"{entry.Time:HH:mm:ss} [{entry.Category}] {entry.Message}";
        LogList.Items.Add(text);
        while (LogList.Items.Count > MaxLogLines)
        {
            LogList.Items.RemoveAt(0);
        }
        LogList.ScrollIntoView(text);
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
    }

    // Évite de rouvrir la fenêtre hors de l'écran (par exemple si le 2e écran est débranché).
    private static bool IsOnScreen(double left, double top) =>
        left >= SystemParameters.VirtualScreenLeft - 50
        && top >= SystemParameters.VirtualScreenTop - 50
        && left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100
        && top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100;

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_safety.IsRunning)
        {
            _safety.RequestStop(StopReason.Manuel);
        }
        _keyboardHook?.Dispose();

        if (WindowState == WindowState.Normal)
        {
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
        }
        _settings.EmergencyKeyEnabled = _safety.Options.EmergencyKeyEnabled;
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
