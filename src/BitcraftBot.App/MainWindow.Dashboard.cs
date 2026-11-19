using System.Windows.Controls;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Tableau de bord de l'onglet Bot (v0.3.17, demande de Julien) : tout ce qui compte, d'un coup d'œil.
// Source : le serveur d'abord (D-044) ; l'écran seulement pour ce que le serveur ne donne pas (cible sélectionnée).
public partial class MainWindow
{
    private static readonly TimeSpan SkillActiveWindow = TimeSpan.FromSeconds(20);

    private readonly SkillTracker _skillTracker = new();

    // Appelé toutes les 500 ms. reader et s sont null si le bot n'est pas connecté au serveur.
    private void UpdateDashboard(ServerReader? reader, PlayerSnapshot? s)
    {
        var now = DateTime.Now;
        var state = reader?.State ?? ReaderState.Deconnecte;
        DashServerText.Text = state switch
        {
            ReaderState.Connecte => s?.SignedIn == true ? "● Serveur : connecté, personnage en jeu" : "● Serveur : connecté, personnage hors ligne",
            ReaderState.Connexion => "◌ Serveur : connexion…",
            _ => "○ Serveur : pas connecté (onglet Paramètres)",
        };
        DashScreenText.Text = _safety.IsRunning
            ? (_gameVisible ? "● Lecture de l’écran : en cours" : "◌ Lecture de l’écran : en pause (jeu caché)")
            : "○ Lecture de l’écran : arrêtée (« Démarrer »)";
        DashMealText.Text = _settings.AutoEatEnabled ? "● Repas auto : " + AutoEatStatusText.Text : "○ Repas auto : désactivé";

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
        DashBonusText.Text = $"Bonus de nourriture : {RegenText(regen)} · {active} bonus actif(s) (onglet Personnage)";
        var misc = new List<string>();
        if (s.TeleportEnergy is { } tp) misc.Add($"énergie de téléportation {tp:0}");
        if (s.TimePlayedSeconds is { } played) misc.Add($"temps de jeu {played / 3600} h");
        DashMiscText.Text = string.Join(" · ", misc);

        // Activité : action en cours, métier (la « jauge de métier », lue sur le serveur), ressource frappée.
        _skillTracker.Update(s.Skills, now);
        if (s.Action is { } action)
        {
            var progress = action.Progress(nowUtc);
            DashActionText.Text = $"Action : {action.French} ({progress:P0})";
            DashActionBar.Value = progress;
        }
        else
        {
            DashActionText.Text = s.Walking == true ? "Action : en marche" : "Action : aucune";
            DashActionBar.Value = 0;
        }
        var skillNames = _gameData.Current?.Skills;
        string SkillName(int id) => skillNames?.GetValueOrDefault(id) ?? $"métier {id}";
        var activeSkill = _skillTracker.IsActive(now, SkillActiveWindow) && s.Skills is { } list
            ? list.FirstOrDefault(x => x.SkillId == _skillTracker.ActiveSkill)
            : null;
        if (activeSkill is not null)
        {
            DashSkillText.Text = $"Métier : {SkillName(activeSkill.SkillId)} niv. {activeSkill.Level}, {activeSkill.Progress:P0} (encore {activeSkill.ExperienceToNextLevel:N0} XP)";
            DashSkillBar.Value = activeSkill.Progress;
        }
        else
        {
            DashSkillText.Text = s.Skills is null ? "Métier : expérience pas encore lue" : "Métier : aucun gain d'expérience depuis 20 s";
            DashSkillBar.Value = 0;
        }
        if (s.Skills is { Count: > 0 } skills)
        {
            DashSkillsText.Text = "Niveaux : " + string.Join(", ", skills
                .Where(x => skillNames?.GetValueOrDefault(x.SkillId) is not (null or "ANY"))
                .OrderByDescending(x => x.Experience)
                .Take(8)
                .Select(x => $"{SkillName(x.SkillId)} {x.Level}"));
        }
        var hit = s.LastExtract is { } extract ? s.Resources.FirstOrDefault(r => r.EntityId == extract.Target) : null;
        DashHitText.Text = hit is not null
            ? $"Ressource frappée : {hit.Name}, santé {ResourceRow.FormatHealth(hit.Health, hit.MaxHealth)}, à {hit.ScreenDistance:0.0} cases (dernier coup : {s.LastExtract!.Damage})"
            : s.LastExtract is not null ? "Ressource frappée : hors de la zone lue" : "Ressource frappée : aucune";

        // Sac et alentours.
        var sac = s.EntityId is { } me ? reader!.Inventories().FirstOrDefault(i => i.Owner == me && i.Index == 0) : null;
        if (sac is { PocketCount: > 0 })
        {
            DashSacText.Text = $"Sac : {sac.Stacks.Count} cases pleines sur {sac.PocketCount}{(sac.Stacks.Count >= sac.PocketCount ? " — PLEIN" : "")}";
            DashSacBar.Value = sac.Stacks.Count / (double)sac.PocketCount;
        }
        else
        {
            DashSacText.Text = "Sac : pas encore lu";
            DashSacBar.Value = 0;
        }
        DashPositionText.Text = s.Position is { } p ? $"Position : {p.ScreenText} (±1), {s.RegionName ?? "région ?"}" : "Position : —";
        var best = s.Resources.FirstOrDefault(ResourceNames.IsHarvestTarget);
        DashBestTreeText.Text = best is null
            ? "Meilleur arbre : aucun arbre mûr autour"
            : $"Meilleur arbre : {best.Name}, à {best.ScreenDistance:0} cases ({best.Direction})";
        DashTreesText.Text = $"{s.Resources.Count(ResourceNames.IsHarvestTarget)} arbres mûrs et {s.Resources.Count} ressources dans la zone lue (onglet Carte).";

        // Alertes simples.
        var alerts = new List<string>();
        if (sac is { PocketCount: > 0 } && sac.Stacks.Count >= sac.PocketCount) alerts.Add("Sac plein.");
        if (s.Stamina is { } st && s.MaxStamina is { } maxSt && st < maxSt * 0.1) alerts.Add("Endurance presque vide.");
        if (s.Health is { } h && s.MaxHealth is { } maxH && h < maxH * 0.5) alerts.Add("Vie sous 50 %.");
        DashAlertText.Text = string.Join(" ", alerts);
    }

    private static void SetBar(TextBlock text, ProgressBar bar, string label, double? value, double? max)
    {
        if (value is not { } v)
        {
            text.Text = $"{label} : —";
            bar.Value = 0;
            return;
        }
        var m = max is > 0 ? max.Value : 100;
        text.Text = $"{label} : {v:0} / {m:0}";
        bar.Value = Math.Clamp(v / m, 0, 1);
    }
}
