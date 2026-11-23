using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// v0.4.4 (demandes de Julien) : sprint, barre d'action et sacs tombés au sol, lus sur le serveur (lecture seule).
// AUCUNE action : le bot affiche et écrit dans le journal. Le sprint sera lancé par le bot pendant les trajets (H3).
public partial class MainWindow
{
    private string _loggedBar = "";
    private string _loggedSprint = "";
    private readonly HashSet<ulong> _loggedBags = [];

    // Appelé à chaque lecture du serveur (500 ms), liaison active.
    private void TrackAround(PlayerSnapshot s)
    {
        var now = DateTimeOffset.Now;
        var sprint = SprintStatus.Of(s.ActionBar, s.Abilities, s.Buffs, now);

        // Barre d'action : écrite au premier passage et à chaque changement de contenu.
        var bar = s.ActionBar is null ? null : string.Join(" ; ", s.ActionBar.Where(x => x.Bar == 0).Select(x => $"{x.Key} = {AbilityName(x.Ability)}"));
        if (bar is not null && bar != _loggedBar)
        {
            _loggedBar = bar;
            var others = s.ActionBar!.Count(x => x.Bar != 0);
            _log.Info("Barre d'action", $"Barre 1 : {(bar.Length > 0 ? bar : "vide")}{(others > 0 ? $" ({others} case(s) sur les autres barres)" : "")}.");
        }

        // Sprint : chaque changement d'état (prêt, actif, en recharge).
        var state = !sprint.AbilityKnown ? "inconnu" : sprint.Active ? "actif" : sprint.CooldownSeconds > 0 ? "recharge" : "prêt";
        if (s.ActionBar is not null && state != _loggedSprint)
        {
            _loggedSprint = state;
            _log.Detail("Sprint", $"Sprint {sprint.French}{(sprint.Slot is { } k ? $", touche {k.Key}" : ", pas sur la barre 1")}.");
        }

        // Sacs au sol : chaque nouveau sac à moins de 20 cases.
        foreach (var b in s.GroundBags.Where(b => b.ScreenDistance <= 20))
        {
            if (_loggedBags.Add(b.EntityId))
            {
                _log.Info("Au sol", $"Sac au sol à {b.ScreenDistance:0.0} cases {b.Direction} : {Contents(b)} — {OwnerText(b, now)}.");
            }
        }
        _loggedBags.IntersectWith(s.GroundBags.Select(b => b.EntityId));

        SprintText.Text = s.ActionBar is null ? "Sprint : barre d'action pas lue" : $"Sprint : {sprint.French}{(sprint.Slot is { } slot ? $" (touche {slot.Key})" : "")}";
        SprintText.Foreground = Theme(sprint.Ready || sprint.Active ? "Good" : "Warn");
        ActionBarText.Text = s.ActionBar is null
            ? "—"
            : string.Join("\n", s.ActionBar.Where(x => x.Bar == 0).Select(x =>
                $"{x.Key} : {AbilityName(x.Ability)}{(x.Ability?.RemainingSeconds(now) is > 0 and var r ? $" (recharge {r:0} s)" : "")}"));
        GroundText.Text = s.GroundBags.Count == 0
            ? "aucun sac au sol autour"
            : string.Join("\n", s.GroundBags.Take(8).Select(b => $"{b.ScreenDistance:0.0} cases {b.Direction} : {Contents(b)} — {OwnerText(b, now)}"));
    }

    private string AbilityName(AbilityInfo? a)
    {
        if (a is null)
        {
            return "?";
        }
        var store = _gameData.Current;
        string Item(int id) => store?.Items.GetValueOrDefault(id) ?? $"objet n° {id}";
        return a.Kind switch
        {
            AbilityKind.Manger => $"manger {Item(a.Value)}",
            AbilityKind.Attaque => store?.CombatActions.GetValueOrDefault(a.Value) ?? $"attaque n° {a.Value}",
            AbilityKind.AttaqueAuto => "attaque automatique",
            AbilityKind.Speciale => store?.CustomAbilities.GetValueOrDefault(a.Value) ?? $"capacité n° {a.Value}",
            AbilityKind.Equiper => $"équiper {Item(a.Value)}",
            AbilityKind.Ceinture => $"ceinture : {Item(a.Value)}",
            AbilityKind.Placer => $"poser {Item(a.Value)}",
            AbilityKind.Preset => $"équipement n° {a.Value + 1}",
            AbilityKind.Emote => "émote",
            AbilityKind.Prospection => "prospection",
            AbilityKind.Deployer or AbilityKind.Deployable => "véhicule ou objet à déployer",
            _ => $"type {(int)a.Kind}",
        };
    }

    private string Contents(GroundBag b)
    {
        if (b.Stacks.Count == 0)
        {
            return "vide ou contenu pas lu";
        }
        var store = _gameData.Current;
        return string.Join(", ", b.Stacks.Select(x =>
            $"{x.Quantity} {(x.IsCargo ? store?.Cargos.GetValueOrDefault(x.ItemId) : store?.Items.GetValueOrDefault(x.ItemId)) ?? $"n° {x.ItemId}"}"));
    }

    private static string OwnerText(GroundBag b, DateTimeOffset now)
    {
        static string In(DateTimeOffset? at, DateTimeOffset now) =>
            at is { } t && t > now ? (t - now).TotalMinutes >= 1 ? $"{(t - now).TotalMinutes:0} min" : $"{(t - now).TotalSeconds:0} s" : "?";
        return b.Owner switch
        {
            BagOwner.AMoi => $"à toi (libre pour tous dans {In(b.PublicAt, now)})",
            BagOwner.AutreJoueur => $"verrouillé : à un autre joueur (libre dans {In(b.PublicAt, now)})",
            _ => $"libre, à tout le monde (disparaît dans {In(b.DespawnAt, now)})",
        };
    }
}
