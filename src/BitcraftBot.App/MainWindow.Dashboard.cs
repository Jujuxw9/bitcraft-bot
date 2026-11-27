using System.Windows.Controls;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Tableau de bord de l'onglet Bot (v0.3.17, allégé en v0.3.18) : l'essentiel, d'un coup d'œil.
// Source : le serveur d'abord (D-044) ; l'écran seulement pour ce que le serveur ne donne pas (cible sélectionnée).
public partial class MainWindow
{
    private static readonly TimeSpan SkillActiveWindow = TimeSpan.FromSeconds(20);
    // v0.4.20 : durée pendant laquelle la dernière action reste affichée après sa fin (enchaînement des coups).
    private static readonly TimeSpan ActionHold = TimeSpan.FromSeconds(2.5);
    private string? _lastActionFrench;
    private DateTime _lastActionSeen;
    private bool _lastActionWasCraft;

    // Part faite de la fabrication en cours (v0.4.31 : recette et dernière part retenues jusqu'à la fin de l'affichage).
    private readonly CraftBarTracker _craftBar = new();

    private double? CraftDone(PlayerSnapshot s) =>
        _recipes?.CraftActions is { } actions ? _craftBar.Update(s.Action, s.Crafts, actions) : null;

    private readonly SkillTracker _skillTracker = new();

    // Appelé toutes les 500 ms, seulement quand l'onglet Bot est affiché (v0.3.19).
    // reader et s sont null si le bot n'est pas connecté au serveur.
    private void UpdateDashboard(ServerReader? reader, PlayerSnapshot? s, IReadOnlyList<InventoryInfo> inventories)
    {
        var now = DateTime.Now;
        var state = reader?.State ?? ReaderState.Deconnecte;
        DashServerText.Text = state switch
        {
            ReaderState.Connecte => s?.SignedIn == true ? "● Serveur : en jeu" : "● Serveur : hors ligne",
            ReaderState.Connexion => "◌ Serveur : connexion…",
            _ => "○ Serveur : pas connecté",
        };
        DashServerText.Foreground = state == ReaderState.Connecte ? Theme("Good") : state == ReaderState.Connexion ? Theme("Warn") : Theme("Muted");
        DashScreenText.Text = _safety.IsRunning ? (_gameVisible ? "● Écran : lu" : "◌ Écran : en pause") : "○ Écran : arrêté";
        DashScreenText.Foreground = _safety.IsRunning ? (_gameVisible ? Theme("Good") : Theme("Warn")) : Theme("Muted");
        DashMealText.Text = _settings.AutoEatEnabled ? "● Repas auto" : "○ Repas auto : coupé";
        DashMealText.Foreground = _settings.AutoEatEnabled ? Theme("Good") : Theme("Muted");

        if (s is null || state != ReaderState.Connecte)
        {
            return;
        }

        // Personnage.
        SetBar(DashHealthText, DashHealthBar, "Vie", s.Health, s.MaxHealth);
        SetBar(DashStaminaText, DashStaminaBar, "Endurance", s.Stamina, s.MaxStamina);
        SetBar(DashHungerText, DashHungerBar, "Faim", s.Satiation, s.MaxSatiation);
        var nowUtc = DateTimeOffset.UtcNow;
        var regen = _foods is { } foods ? MealPlanner.FoodRegenSeconds(s.Buffs ?? [], s.Buffs is not null, foods.FoodRegenBuffIds, nowUtc) : null;
        var active = s.Buffs?.Count(b => b.RemainingSeconds(nowUtc) != 0) ?? 0;
        DashBonusText.Text = $"Bonus nourriture : {RegenText(regen)} · {active} bonus";

        // Activité : action en cours, métier (la « jauge de métier », lue sur le serveur), ressource frappée.
        // v0.4.20 : entre deux coups de hache, le serveur dit « aucune action » une fraction de seconde : l'affichage
        // clignotait. On garde la dernière action affichée pendant ActionHold, en la marquant « en cours ».
        // v0.4.22 : pour une fabrication, la durée de l'action n'est que celle d'un geste (la barre montait à 12 % puis
        // retombait). On montre la vraie progression de la fabrication : gestes faits ÷ (gestes par objet × objets).
        if (s.Action is { Type: not "Craft" })
        {
            // Autre action (coupe, marche…) : la fabrication suivie est terminée.
            _craftBar.Reset();
        }
        var craft = CraftDone(s);
        if (s.Action is { } action)
        {
            var progress = action.Type == "Craft" && craft is { } c ? c : action.Progress(nowUtc);
            _lastActionFrench = action.French;
            _lastActionWasCraft = action.Type == "Craft";
            _lastActionSeen = DateTime.Now;
            DashActionText.Text = $"{action.French} · {progress:P0}";
            DashActionBar.Smooth(progress);
        }
        else if (_lastActionFrench is { } held && DateTime.Now - _lastActionSeen < ActionHold)
        {
            craft = _lastActionWasCraft ? craft : null;
            DashActionText.Text = craft is { } c ? $"{held} · {(c >= 1 ? "terminée" : c.ToString("P0"))}" : $"{held} · en cours";
            DashActionBar.Smooth(craft ?? 1);
        }
        else
        {
            _lastActionFrench = null;
            _craftBar.Reset();
            DashActionText.Text = s.Walking == true ? "En marche" : "Aucune action";
            DashActionBar.Smooth(0);
        }
        var skillNames = _gameData.Current?.Skills;
        string SkillName(int id) => skillNames?.GetValueOrDefault(id) ?? $"métier {id}";
        var activeSkill = _skillTracker.IsActive(now, SkillActiveWindow) && s.Skills is { } list
            ? list.FirstOrDefault(x => x.SkillId == _skillTracker.ActiveSkill)
            : null;
        if (activeSkill is not null)
        {
            DashSkillText.Text = $"{SkillName(activeSkill.SkillId)} niv. {activeSkill.Level} · {activeSkill.Progress:P0}";
            DashSkillBar.Smooth(activeSkill.Progress);
        }
        else
        {
            DashSkillText.Text = "Métier : —";
            DashSkillBar.Smooth(0);
        }
        var hit = s.LastExtract is { } extract ? s.Resources.FirstOrDefault(r => r.EntityId == extract.Target) : null;
        DashHitText.Text = hit is not null
            ? $"Cible : {hit.Name}, {ResourceRow.FormatHealth(hit.Health, hit.MaxHealth)}"
            : "Cible : —";

        // Sac et alentours.
        var sac = s.EntityId is { } me ? inventories.FirstOrDefault(i => i.Owner == me && i.Index == 0) : null;
        if (sac is { PocketCount: > 0 })
        {
            var full = sac.Stacks.Count >= sac.PocketCount;
            DashSacText.Text = $"Sac {sac.Stacks.Count} / {sac.PocketCount}{(full ? " · PLEIN" : "")}";
            DashSacBar.Smooth(sac.Stacks.Count / (double)sac.PocketCount);
        }
        else
        {
            DashSacText.Text = "Sac —";
            DashSacBar.Smooth(0);
        }
        DashPositionText.Text = s.Position is { } p ? $"{p.ScreenText} · {s.RegionName ?? "?"}" : "Position —";
        var best = s.Resources.FirstOrDefault(ResourceNames.IsHarvestTarget);
        DashBestTreeText.Text = best is null
            ? "Aucun arbre mûr autour"
            : $"Arbre : {best.Name}, {best.ScreenDistance:0} cases {best.Direction}";

        // Alertes simples.
        var alerts = new List<string>();
        if (sac is { PocketCount: > 0 } && sac.Stacks.Count >= sac.PocketCount) alerts.Add("Sac plein");
        if (s.Stamina is { } st && s.MaxStamina is { } maxSt && st < maxSt * 0.1) alerts.Add("Endurance vide");
        if (s.Health is { } h && s.MaxHealth is { } maxH && h < maxH * 0.5) alerts.Add("Vie < 50 %");
        if (_hadFood == false) alerts.Add("Pas de nourriture");
        DashAlertText.Text = string.Join(" · ", alerts);
    }

    private static void SetBar(TextBlock text, ProgressBar bar, string label, double? value, double? max)
    {
        if (value is not { } v)
        {
            text.Text = $"{label} —";
            bar.Smooth(0);
            return;
        }
        var m = max is > 0 ? max.Value : 100;
        text.Text = $"{label} {v:0} / {m:0}";
        bar.Smooth(Math.Clamp(v / m, 0, 1));
    }
}
