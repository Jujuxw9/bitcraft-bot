using System.Windows;
using System.Windows.Controls;
using BitcraftBot.GameData;
using BitcraftBot.Input.Actions;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Capacités automatiques (v0.4.17, D-069) : pendant que Julien récolte lui-même, le bot appuie sur la touche
// de la capacité de la barre d'action qui va avec ce métier (ex. Hewing Strikes en coupant du bois),
// dès que la recharge est finie. Réserve d'endurance 30 %, une touche toutes les 2 s au plus,
// et toutes les sécurités du contrôleur d'actions (jeu au premier plan, écran « en jeu », chat fermé, Julien ne tape pas).
public partial class MainWindow
{
    private static readonly TimeSpan AbilityConfirmTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan AbilityFailurePause = TimeSpan.FromMinutes(5);

    private AbilityBook? _abilities;
    private bool _abilitiesLoading;
    private DateTimeOffset _abilityLastPress = DateTimeOffset.MinValue;
    private readonly Dictionary<int, DateTimeOffset> _abilityBlocked = [];
    private readonly Dictionary<int, int> _abilityFailures = [];
    private PendingAbility? _pendingAbility;
    private string _abilityListKey = "";
    private string _abilityLastLogged = "";
    private readonly Dictionary<int, (CheckBox Check, TextBlock State)> _abilityRows = [];

    private sealed record PendingAbility(DateTimeOffset At, int AbilityId, string Name, long CooldownStartMs);

    private void InitAbilities()
    {
        AbilitiesCheck.IsChecked = _settings.AutoAbilitiesEnabled;
        AbilitiesStatusText.Text = _settings.AutoAbilitiesEnabled ? "Activé." : "Coupé.";
        _log.Info("Capacités", _settings.AutoAbilitiesEnabled
            ? "Capacités automatiques activées (réglage gardé) : seulement pendant vos récoltes."
            : "Capacités automatiques coupées (interrupteur « Capacités auto » dans OPTIONS).");
        _gameData.Loaded += _ => Dispatcher.BeginInvoke(LoadAbilitiesInBackground);
        LoadAbilitiesInBackground();
    }

    private void LoadAbilitiesInBackground()
    {
        if (_abilitiesLoading || _abilities is not null || !AbilityBook.CanLoad(GameDataLoader.Folder))
        {
            return;
        }
        _abilitiesLoading = true;
        _ = Task.Run(() =>
        {
            try
            {
                var book = AbilityBook.Load(GameDataLoader.Folder);
                Dispatcher.BeginInvoke(() =>
                {
                    _abilities = book;
                    _log.Info("Données", $"Capacités chargées : {book.Abilities.Count} capacités, dont {book.Abilities.Values.Count(a => a.SkillId is not null)} liées à un métier.");
                });
            }
            catch (Exception ex)
            {
                _log.Erreur("Données", $"Capacités illisibles : {ex.Message}");
            }
            finally
            {
                _abilitiesLoading = false;
            }
        });
    }

    private void AbilitiesCheck_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoAbilitiesEnabled = AbilitiesCheck.IsChecked == true;
        _pendingAbility = null;
        _abilityLastLogged = "";
        _settings.Save(LocalSettings.DefaultPath);
        _log.Info("Capacités", _settings.AutoAbilitiesEnabled ? "Capacités automatiques activées (case cochée)." : "Capacités automatiques coupées (case décochée).");
        if (_settings.AutoAbilitiesEnabled && _actions.IsHalted)
        {
            _actions.Resume("capacités automatiques réactivées");
        }
        AbilitiesStatusText.Text = _settings.AutoAbilitiesEnabled ? "Activé." : "Coupé.";
    }

    private bool AbilityAllowed(AbilityDef def) => _settings.AbilityChoices.TryGetValue(def.Id, out var on) ? on : def.DefaultOn;

    // Appelé toutes les 500 ms avec la dernière lecture du serveur.
    private void RunAbilities(ServerReader reader, PlayerSnapshot s)
    {
        var book = _abilities;
        if (book is null)
        {
            ShowAbilityState(_settings.AutoAbilitiesEnabled ? "attend les données du jeu" : "coupé");
            return;
        }
        var now = DateTimeOffset.UtcNow;
        var connected = reader.State == ReaderState.Connecte && s.SignedIn == true;
        var action = connected ? s.Action : null;
        var situation = new AbilitySituation
        {
            Enabled = _settings.AutoAbilitiesEnabled,
            Allowed = book.Abilities.Values.Where(AbilityAllowed).Select(a => a.Id).ToHashSet(),
            ActionType = action?.Type,
            ActionSkill = action is null ? null : ActionSkill(action),
            Bar = s.ActionBar,
            Buffs = s.Buffs,
            Stamina = s.Stamina,
            MaxStamina = s.MaxStamina,
            Now = now,
            LastPress = _abilityLastPress,
            BlockedUntil = _abilityBlocked,
        };
        var decision = AbilityPlanner.Decide(situation, book.Abilities);
        ShowAbilityList(decision.Slots);

        if (!_settings.AutoAbilitiesEnabled)
        {
            ShowAbilityState("coupé");
            return;
        }
        if (!connected)
        {
            _pendingAbility = null;
            ShowAbilityState("attend : personnage pas connecté dans le jeu (ou serveur pas lu)");
            return;
        }

        // 1. Une touche attend sa confirmation par le serveur (recharge relancée ou bonus apparu).
        if (_pendingAbility is { } p)
        {
            var slot = decision.Slots.FirstOrDefault(x => x.Def.Id == p.AbilityId);
            var ability = slot?.Slot.Ability;
            var def = book.Abilities.GetValueOrDefault(p.AbilityId);
            var confirmed = (ability is not null && (ability.CooldownStartMs != p.CooldownStartMs || ability.RemainingSeconds(now) > 0))
                            || (def is not null && s.Buffs?.Any(b => def.BuffIds.Contains(b.BuffId) && b.RemainingSeconds(now) != 0) == true);
            if (confirmed)
            {
                _log.Detail("Capacités", $"« {p.Name} » lancée (confirmé par le serveur).");
                _abilityFailures.Remove(p.AbilityId);
                _pendingAbility = null;
            }
            else if (now - p.At > AbilityConfirmTimeout)
            {
                _pendingAbility = null;
                var failures = _abilityFailures.GetValueOrDefault(p.AbilityId) + 1;
                _abilityFailures[p.AbilityId] = failures;
                if (failures >= 2)
                {
                    _abilityFailures.Remove(p.AbilityId);
                    _abilityBlocked[p.AbilityId] = now + AbilityFailurePause;
                    _log.Alerte("Capacités", $"« {p.Name} » : la touche n'a rien donné deux fois de suite (mauvais outil ? touche du jeu différente ?). Capacité en pause {AbilityFailurePause.TotalMinutes:0} min.");
                }
                else
                {
                    _log.Alerte("Capacités", $"« {p.Name} » : touche envoyée, mais le serveur ne montre ni recharge ni bonus après {AbilityConfirmTimeout.TotalSeconds:0} s.");
                }
            }
            else
            {
                ShowAbilityState($"lance {p.Name}…");
                return;
            }
        }

        if (decision.Press is not { } press)
        {
            ShowAbilityState(decision.Reason);
            return;
        }
        // v0.4.29 : cases « - » et « = » (chariot, bateau) jamais utilisées.
        if (GameKeys.ForSlot(press.Slot.Slot) is not { } key || GameKeys.Forbidden.Contains(key))
        {
            ShowAbilityState($"touche {press.Slot.Key} pas prise en charge");
            return;
        }

        // 2. Sécurités communes à toutes les actions (contrôleur d'actions).
        if (_actions.WhyNot() is { } wait)
        {
            ShowAbilityState($"veut lancer {press.Def.Name}, attend : {wait}");
            return;
        }

        _log.Info("Capacités", $"Capacité : {press.Def.Name} (touche {press.Slot.Key}), {AbilityPlanner.Activity(press.Def.SkillName)}.");
        _abilityLastPress = now;
        _pendingAbility = new PendingAbility(now, press.Def.Id, press.Def.Name, press.Slot.Ability?.CooldownStartMs ?? 0);
        ShowAbilityState($"lance {press.Def.Name}…");
        _ = PressAbilityAsync(press.Def.Name, key);
    }

    private async Task PressAbilityAsync(string name, ushort key)
    {
        var result = await _actions.PressAsync($"Capacité {name}", key);
        if (!result.Done)
        {
            _log.Info("Capacités", $"Touche de « {name} » pas envoyée : {result.Detail}.");
            _pendingAbility = null;
        }
    }

    // Liste des capacités de la barre : un interrupteur chacune ; reconstruite seulement quand la barre change.
    private void ShowAbilityList(IReadOnlyList<AbilitySlotState> slots)
    {
        var key = string.Join(";", slots.Select(x => $"{x.Slot.Slot}:{x.Def.Id}"));
        if (key != _abilityListKey)
        {
            _abilityListKey = key;
            _abilityRows.Clear();
            AbilityList.Children.Clear();
            foreach (var x in slots)
            {
                var def = x.Def;
                var check = new CheckBox
                {
                    Style = (Style)FindResource("SwitchCheck"),
                    Content = $"{x.Slot.Key} · {def.Name}",
                    IsChecked = def.SkillId is not null && AbilityAllowed(def),
                    IsEnabled = def.SkillId is not null,
                    Margin = new Thickness(0, 4, 0, 0),
                    ToolTip = AbilityTip(def),
                };
                check.Click += (_, _) =>
                {
                    _settings.AbilityChoices[def.Id] = check.IsChecked == true;
                    _settings.Save(LocalSettings.DefaultPath);
                    _abilityBlocked.Remove(def.Id);
                    _log.Info("Capacités", $"« {def.Name} » {(check.IsChecked == true ? "allumée" : "coupée")} (case de Julien).");
                };
                var state = new TextBlock { Style = (Style)FindResource("MutedText"), Margin = new Thickness(28, 0, 0, 0), FontSize = 11 };
                AbilityList.Children.Add(check);
                AbilityList.Children.Add(state);
                _abilityRows[def.Id] = (check, state);
            }
            if (slots.Count > 0)
            {
                _log.Info("Capacités", "Barre d'action : " + string.Join(" ; ", slots.Select(x =>
                    $"{x.Slot.Key} {x.Def.Name} ({(x.Def.SkillId is null ? "pas liée à une récolte" : (AbilityAllowed(x.Def) ? "allumée" : "coupée") + ", " + x.Def.SkillName)})")) + ".");
            }
            else
            {
                AbilityList.Children.Add(new TextBlock { Style = (Style)FindResource("MutedText"), Text = "Barre d'action pas encore lue.", FontSize = 11 });
            }
        }
        foreach (var x in slots)
        {
            if (_abilityRows.TryGetValue(x.Def.Id, out var row))
            {
                row.State.Text = x.Text;
            }
        }
    }

    private static string AbilityTip(AbilityDef def)
    {
        var parts = new List<string> { def.SkillName is { } skill ? $"Pour : {skill}" : "Pas liée à une récolte : jamais lancée par le bot" };
        parts.Add(def.StaminaCost > 0 ? $"Endurance : {def.StaminaCost:0}" : "Endurance : aucune");
        parts.Add($"Recharge : {def.CooldownSeconds:0.#} s");
        if (def.Effect.Length > 0)
        {
            parts.Add($"Effet : {def.Effect}{(def.BuffSeconds > 0 ? $" pendant {def.BuffSeconds} s" : "")}");
        }
        return string.Join("\n", parts);
    }

    private void ShowAbilityState(string text)
    {
        AbilitiesStatusText.Text = text[..1].ToUpperInvariant() + text[1..] + ".";
        // Journal : seulement quand l'état change (sans les chiffres).
        var key = new string(text.Where(c => !char.IsDigit(c)).ToArray());
        if (key != _abilityLastLogged)
        {
            _abilityLastLogged = key;
            _log.Detail("Capacités", text);
        }
    }
}
