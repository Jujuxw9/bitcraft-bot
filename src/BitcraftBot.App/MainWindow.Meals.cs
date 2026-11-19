using System.Windows;
using System.Windows.Media;
using BitcraftBot.GameData;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Repas automatique (D-042, demande de Julien du 2026-11-19) : Maj + E quand la faim passe sous 30 %
// ou quand le bonus de nourriture est fini. Un aliment à la fois, avec une pause, vérifié sur le serveur.
// Sécurités : option cochée, BitCraft fenêtre active et visible, personnage connecté dans le jeu,
// Julien ne tape pas au clavier, au plus 8 aliments en 10 minutes, Num 5 coupe tout.
public partial class MainWindow
{
    private const ushort ShiftScanCode = 0x2A;
    private const ushort EScanCode = 0x12;
    private const int MaxMealsPer10Min = 8;
    private static readonly TimeSpan MealConfirmTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MealFailurePause = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan KeyboardQuietTime = TimeSpan.FromSeconds(3);

    private readonly Random _mealRandom = new();
    private readonly Queue<DateTime> _mealTimes = new();
    private FoodBook? _foods;
    private bool _foodsLoading;
    private bool _mealSeries;
    private DateTime _mealNextAllowed = DateTime.MinValue;
    private DateTime _mealPausedUntil = DateTime.MinValue;
    private int _mealFailures;
    private PendingMeal? _pendingMeal;
    private string _mealLastLogged = "";
    private DateTime _lastPhysicalKey = DateTime.MinValue;
    private Brush _mealOverlayBrush = OffBrush;

    private string MealOverlayText { get; set; } = "Repas auto : actif";

    private sealed record PendingMeal(DateTime At, double? Satiation, int? Regen, string Food, int FoodCount);

    private void InitMeals()
    {
        AutoEatCheck.IsChecked = _settings.AutoEatEnabled;
        _gameData.Loaded += _ => Dispatcher.BeginInvoke(LoadFoodsInBackground);
    }

    private void LoadFoodsInBackground()
    {
        if (_foodsLoading || _foods is not null || !FoodBook.CanLoad(GameDataLoader.Folder))
        {
            return;
        }
        _foodsLoading = true;
        _ = Task.Run(() =>
        {
            try
            {
                var book = FoodBook.Load(GameDataLoader.Folder);
                Dispatcher.BeginInvoke(() =>
                {
                    _foods = book;
                    _log.Info("Données", $"Aliments chargés : {book.Foods.Count} aliments, {book.FoodRegenBuffIds.Count} niveaux de bonus de nourriture.");
                });
            }
            catch (Exception ex)
            {
                _log.Erreur("Données", $"Aliments illisibles : {ex.Message}");
            }
            finally
            {
                _foodsLoading = false;
            }
        });
    }

    private void AutoEatCheck_Click(object sender, RoutedEventArgs e) => SetAutoEat(AutoEatCheck.IsChecked == true, "case cochée par Julien");

    private void SetAutoEat(bool enabled, string why)
    {
        _settings.AutoEatEnabled = enabled;
        AutoEatCheck.IsChecked = enabled;
        _mealSeries = false;
        _pendingMeal = null;
        _mealFailures = 0;
        _mealPausedUntil = DateTime.MinValue;
        _mealLastLogged = "";
        _log.Info("Repas", enabled ? $"Repas automatique activé ({why})." : $"Repas automatique désactivé ({why}).");
        AutoEatStatusText.Text = enabled ? "Activé." : "Désactivé.";
        UpdateOverlay();
    }

    // Appelé par le crochet clavier : Julien tape (touche physique, pas simulée).
    private void NotePhysicalKey() => _lastPhysicalKey = DateTime.Now;

    // Appelé toutes les 500 ms avec la dernière lecture du serveur.
    private void RunMeals(ServerReader reader, PlayerSnapshot s)
    {
        var now = DateTime.Now;
        var foods = _foods;
        var sac = s.EntityId is { } me && !reader.InventoryRefused
            ? reader.Inventories().FirstOrDefault(i => i.Owner == me && i.Index == 0)
            : null;
        var regen = foods is null ? null : MealPlanner.FoodRegenSeconds(s.Buffs ?? [], s.Buffs is not null, foods.FoodRegenBuffIds, DateTimeOffset.UtcNow);
        var (next, blocker) = sac is not null && foods is not null ? MealPlanner.FirstFood(sac.Stacks, foods.Foods) : (null, null);
        var max = s.MaxSatiation ?? MealPlanner.DefaultMaxSatiation;

        // Affichage (onglet Personnage) : faim, maximum et bonus.
        SrvSatiation.Text = s.Satiation is { } sa
            ? $"{sa:0.#} / {max:0}{(s.MaxSatiation is null ? " (max supposé)" : "")} · bonus de nourriture : {RegenText(regen)}"
            : "—";
        AutoEatFoodText.Text = sac is null || foods is null
            ? "Sac pas encore lu : le premier aliment est inconnu."
            : blocker is not null ? $"Attention : « {blocker.Name} » passe avant la nourriture dans le sac."
            : next is not null ? $"Prochain aliment (premier du sac) : « {next.Name} », rend {next.Hunger:0} de faim{(next.GivesFoodRegen ? " et donne le bonus" : "")} ({Count(sac, next.ItemId)} dans le sac)."
            : "Aucun aliment dans le sac.";

        if (!_settings.AutoEatEnabled)
        {
            return;
        }

        // 1. Un repas attend sa confirmation par le serveur.
        if (_pendingMeal is { } p)
        {
            var confirmed = (s.Satiation is { } after && p.Satiation is { } before && after > before + 0.5)
                            || (p.Regen == 0 && regen > 0)
                            || (sac is not null && next is not null && next.Name == p.Food && Count(sac, next.ItemId) < p.FoodCount);
            if (confirmed)
            {
                _log.Info("Repas", $"Mangé : « {p.Food} ». Faim {p.Satiation:0.#} → {s.Satiation:0.#} ; bonus de nourriture : {RegenText(regen)}.");
                _pendingMeal = null;
                _mealFailures = 0;
                _mealNextAllowed = now + TimeSpan.FromSeconds(8 + _mealRandom.NextDouble() * 6);
            }
            else if (now - p.At > MealConfirmTimeout)
            {
                _pendingMeal = null;
                _mealSeries = false;
                _mealFailures++;
                if (_mealFailures >= 2)
                {
                    _mealPausedUntil = now + MealFailurePause;
                    _mealFailures = 0;
                    _log.Alerte("Repas", $"Maj + E n'a rien donné deux fois de suite (pas de nourriture ? chat ouvert ? menu ouvert ?). Repas automatique en pause {MealFailurePause.TotalMinutes:0} min.");
                }
                else
                {
                    _log.Alerte("Repas", $"Maj + E envoyé, mais le serveur ne montre ni faim en hausse ni bonus après {MealConfirmTimeout.TotalSeconds:0} s.");
                }
            }
            ShowMealState("en train de manger…", WaitBrush);
            return;
        }
        if (now < _mealPausedUntil)
        {
            ShowMealState($"en pause jusqu'à {_mealPausedUntil:HH:mm} (échecs)", WaitBrush);
            return;
        }
        if (now < _mealNextAllowed)
        {
            ShowMealState("pause entre deux aliments", WaitBrush);
            return;
        }

        // 2. Faut-il manger ?
        if (reader.State != ReaderState.Connecte || s.SignedIn != true)
        {
            _mealSeries = false;
            ShowMealState("attend : personnage pas connecté dans le jeu (ou serveur pas lu)", OffBrush);
            return;
        }
        var decision = MealPlanner.Decide(new MealSituation(s.Satiation, s.MaxSatiation, regen, sac is not null && foods is not null, next, blocker, _mealSeries));
        if (decision.Action == MealAction.Rien)
        {
            _mealSeries = false;
            ShowMealState("actif, rien à manger : " + decision.Reason, OnBrush);
            return;
        }
        if (decision.Action == MealAction.Bloque)
        {
            _mealSeries = false;
            ShowMealState("bloqué : " + decision.Reason, WaitBrush, alert: true);
            return;
        }

        // 3. Conditions de sécurité avant d'appuyer sur une touche.
        var game = _gameWindow ?? new GameWindowLocator().Find();
        string? wait = game is null ? "jeu introuvable"
            : !_inputSender.IsForeground(game.Handle) ? "BitCraft n'est pas la fenêtre active"
            : !GameVisibility.IsUncovered(game) ? "le jeu est caché par une autre fenêtre"
            : now - _lastPhysicalKey < KeyboardQuietTime ? "vous tapez au clavier"
            : null;
        while (_mealTimes.Count > 0 && now - _mealTimes.Peek() > TimeSpan.FromMinutes(10))
        {
            _mealTimes.Dequeue();
        }
        wait ??= _mealTimes.Count >= MaxMealsPer10Min ? $"déjà {MaxMealsPer10Min} aliments en 10 min" : null;
        if (wait is not null)
        {
            ShowMealState($"veut manger ({decision.Reason}), attend : {wait}", WaitBrush);
            return;
        }

        _log.Info("Repas", $"Maj + E : {decision.Reason}.");
        _pendingMeal = new PendingMeal(now, s.Satiation, regen, next?.Name ?? "aliment inconnu", next is not null && sac is not null ? Count(sac, next.ItemId) : 0);
        _mealSeries = decision.ContinuesSeries;
        _mealTimes.Enqueue(now);
        ShowMealState("en train de manger…", WaitBrush);
        _ = PressShiftEAsync(game!.Handle);
    }

    // Maj + E avec des durées d'appui humaines. Les touches sont toujours relâchées, même en cas d'erreur.
    private async Task PressShiftEAsync(IntPtr gameHandle)
    {
        var shiftDown = false;
        var eDown = false;
        try
        {
            if (!_inputSender.IsForeground(gameHandle))
            {
                _log.Info("Repas", "Maj + E annulé : BitCraft n'est plus la fenêtre active.");
                _pendingMeal = null;
                return;
            }
            _inputSender.KeyDown(ShiftScanCode);
            shiftDown = true;
            await Task.Delay(40 + _mealRandom.Next(60));
            _inputSender.KeyDown(EScanCode);
            eDown = true;
            await Task.Delay(60 + _mealRandom.Next(80));
        }
        catch (Exception ex)
        {
            _log.Erreur("Repas", $"Envoi de Maj + E impossible : {ex.Message}");
            _pendingMeal = null;
        }
        finally
        {
            try
            {
                if (eDown) _inputSender.KeyUp(EScanCode);
                await Task.Delay(30 + _mealRandom.Next(40));
                if (shiftDown) _inputSender.KeyUp(ShiftScanCode);
            }
            catch (Exception ex)
            {
                _log.Erreur("Repas", $"Relâchement des touches impossible : {ex.Message}");
            }
        }
    }

    // Num 5 : le repas automatique s'arrête (il faut recocher la case).
    private void StopMealsForEmergency()
    {
        if (_settings.AutoEatEnabled)
        {
            SetAutoEat(false, $"touche {Input.Keyboard.EmergencyKey.DisplayName}");
        }
    }

    private void ShowMealState(string text, Brush color, bool alert = false)
    {
        AutoEatStatusText.Text = text[..1].ToUpperInvariant() + text[1..] + ".";
        MealOverlayText = "Repas auto : " + (text.Length > 46 ? text[..46] + "…" : text);
        _mealOverlayBrush = color;
        // Journal : seulement quand l'état change (sans les chiffres qui bougent toutes les minutes).
        var key = new string(text.Where(c => !char.IsDigit(c)).ToArray());
        if (key != _mealLastLogged)
        {
            _mealLastLogged = key;
            if (alert) _log.Alerte("Repas", text);
            else _log.Detail("Repas", text);
        }
    }

    private static string RegenText(int? seconds) => seconds switch
    {
        null => "inconnu",
        0 => "aucun",
        int.MaxValue => "permanent",
        < 90 => $"{seconds} s",
        _ => $"{seconds / 60} min",
    };

    private static int Count(InventoryInfo sac, int itemId) => sac.Stacks.Where(st => !st.IsCargo && st.ItemId == itemId).Sum(st => st.Quantity);
}
