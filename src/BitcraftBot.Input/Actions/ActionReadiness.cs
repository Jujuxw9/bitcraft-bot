namespace BitcraftBot.Input.Actions;

// v0.4.30 (T-12) : attente avant un geste lancé depuis la fenêtre du bot (ex. « Se déplacer ici »).
// Au moment du clic dans le menu, c'est le bot qui est au premier plan : le contrôleur refuserait tout de suite
// (« BitCraft n'est pas la fenêtre active »). On attend que Julien repasse sur le jeu et lâche souris et clavier.
public static class ActionReadiness
{
    // Raisons qui ne passeront pas toutes seules : inutile d'attendre.
    public static bool IsFinal(string reason) =>
        reason.StartsWith("actions bloquées", StringComparison.Ordinal)
        || reason.StartsWith("jeu introuvable", StringComparison.Ordinal);

    // Rend null quand les actions sont permises, sinon la dernière raison du refus (temps dépassé, arrêt, raison définitive).
    public static async Task<string?> WaitAsync(Func<string?> whyNot, TimeSpan max, TimeSpan step, Func<bool>? stop = null, Func<TimeSpan, Task>? delay = null)
    {
        delay ??= Task.Delay;
        var waited = TimeSpan.Zero;
        while (true)
        {
            var why = whyNot();
            if (why is null)
            {
                return null;
            }
            if (IsFinal(why))
            {
                return why;
            }
            if (stop?.Invoke() == true)
            {
                return "arrêt demandé";
            }
            if (waited >= max)
            {
                return why;
            }
            await delay(step);
            waited += step;
        }
    }

    // Répète une vérification jusqu'à ce qu'elle réussisse (ex. carte vue fermée), au plus « tries » fois.
    public static async Task<bool> UntilAsync(Func<Task<bool>> check, int tries, TimeSpan step, Func<TimeSpan, Task>? delay = null)
    {
        delay ??= Task.Delay;
        for (var i = 0; i < tries; i++)
        {
            if (await check())
            {
                return true;
            }
            if (i < tries - 1)
            {
                await delay(step);
            }
        }
        return false;
    }
}
