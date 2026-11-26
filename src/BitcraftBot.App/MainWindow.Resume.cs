using System.Windows;
using BitcraftBot.Input.Actions;
using BitcraftBot.Server;
using BitcraftBot.Server.World;

namespace BitcraftBot.App;

// Reprise automatique des récoltes (v0.4.24, demande de Julien) : voir HarvestResume. Ici, l'interrupteur, le journal
// et l'envoi des touches (Tab, Interact) par le contrôleur d'actions, avec toutes ses sécurités
// (jeu au premier plan, écran « en jeu », chat fermé, pause quand Julien tape).
public partial class MainWindow
{
    private readonly HarvestResume _resume = new();
    private string _resumeState = "";
    private ulong? _loggedGameTarget;

    private void InitResume()
    {
        ResumeCheck.IsChecked = _settings.AutoResumeEnabled;
        _log.Info("Reprise", _settings.AutoResumeEnabled
            ? "Reprise des récoltes activée : une récolte arrêtée faute d'endurance est relancée quand l'endurance revient (Tab puis M, sans bouger)."
            : "Reprise des récoltes coupée (interrupteur dans OPTIONS).");
    }

    private void ResumeCheck_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoResumeEnabled = ResumeCheck.IsChecked == true;
        _settings.Save(LocalSettings.DefaultPath);
        _log.Info("Reprise", _settings.AutoResumeEnabled ? "Reprise des récoltes activée (case cochée)." : "Reprise des récoltes coupée (case décochée).");
        if (_settings.AutoResumeEnabled && _actions.IsHalted)
        {
            _actions.Resume("reprise des récoltes réactivée");
        }
    }

    // Appelé toutes les 500 ms avec la dernière lecture du serveur.
    private void RunResume(ServerReader reader, PlayerSnapshot s)
    {
        var connected = reader.State == ReaderState.Connecte && s.SignedIn == true;
        // Journal : cible sélectionnée dans le jeu (pour vérifier ce que fait Tab sur les ressources).
        if (connected && s.Target != _loggedGameTarget)
        {
            _loggedGameTarget = s.Target;
            var name = s.Target is { } t ? s.Resources.FirstOrDefault(r => r.EntityId == t)?.Name ?? $"entité {t} (pas une ressource de la zone)" : "aucune";
            _log.Detail("Reprise", $"Cible sélectionnée dans le jeu : {name}.");
        }
        var decision = _resume.Update(new ResumeInput
        {
            Now = DateTimeOffset.UtcNow,
            Enabled = _settings.AutoResumeEnabled && connected,
            Action = connected ? s.Action : null,
            LastExtractTarget = s.LastExtract?.Target,
            Position = s.Position,
            Stamina = s.Stamina,
            MaxStamina = s.MaxStamina,
            Target = s.Target,
            Resources = s.Resources,
            // Sac plein : pas bloquant (le chariot peut prendre la suite) ; un M sans effet deux fois arrête la reprise.
            BagFull = false,
        });
        if (decision.Log is { } line)
        {
            if (decision.Alert) _log.Alerte("Reprise", line);
            else _log.Info("Reprise", line);
        }
        var state = decision.State;
        if (decision.Step != ResumeStep.Rien)
        {
            if (_actions.WhyNot() is { } wait)
            {
                // Le contrôleur refuse pour l'instant : on refera l'essai au prochain passage (Tab compté quand même).
                state = $"{decision.State} attend : {wait}";
            }
            else
            {
                var (key, label) = decision.Step == ResumeStep.Tab ? (GameKeys.Tab, "Reprise : cible suivante (Tab)") : (GameKeys.Interact, "Reprise : Interact (M)");
                _ = PressResumeAsync(label, key);
            }
        }
        if (state != _resumeState)
        {
            _resumeState = state;
            ResumeStatusText.Text = _settings.AutoResumeEnabled ? state : "Coupé.";
            _log.Detail("Reprise", state);
        }
    }

    private async Task PressResumeAsync(string label, ushort key)
    {
        var result = await _actions.PressAsync(label, key);
        if (!result.Done)
        {
            _log.Info("Reprise", $"Touche pas envoyée ({label}) : {result.Detail}.");
        }
    }
}
