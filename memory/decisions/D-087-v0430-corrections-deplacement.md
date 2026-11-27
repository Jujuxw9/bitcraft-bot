# D-087 — v0.4.30 : corrections de « Se déplacer ici » après relecture

- **Statut** : Validée (tâche de nuit T-12, choix prudents ; à revoir avec Julien après son premier essai)
- **Date** : 2026-11-27
- **Validée par** : tâche T-12 d'après sa fiche (`taches-planifiees/T-12-v0430-corrections-essai.md`) et les règles de la nuit (D-081)
- **Liens** : D-086 (v0.4.29), D-052 (aucune action hors « en jeu »), Q-028 (clics de démarrage), [architecture-suite.md](../technique/architecture-suite.md) section 34

## Contexte
L'essai en jeu de T-11 n'a pas pu se faire (jeu bloqué sur l'écran d'accueil). T-12 a relu le code de T-10 étape par étape, comme en vrai.

## Décision
1. **Défaut bloquant corrigé** : le trajet se lance par un clic dans la fenêtre du **bot** ; à cet instant le jeu n'est pas au premier plan, et la première touche (ouvrir la carte) était refusée tout de suite (« BitCraft n'est pas la fenêtre active »). Le bot **attend maintenant 20 s au plus** que Julien repasse sur le jeu (**Alt + Tab**, sans cliquer dans le jeu) et lâche souris et clavier. Le bot ne met **pas** le jeu au premier plan lui-même.
2. **Carte lente** : la barre « Set destination » est lue trois fois (0,6 s, 1 s, 1,4 s) avant de conclure ; la fermeture est vérifiée trois fois (0,5 s à 1,5 s) avant l'alerte « carte restée ouverte ».
3. **Touche de la carte jamais rappuyée pour rien** : si la carte n'est pas vue après la touche, le bot ne rappuie pas (en v0.4.29, il rappuyait « pour refermer », ce qui pouvait ouvrir la carte et tout bloquer).
4. **Zone de la barre** inchangée (pas de mesure en jeu), mais maintenant **vérifiée sur une vraie capture** de la grande carte (`images/reference-ui/fenetres/carte-du-monde_001.png` : « (9196, 7258) » et « Set destination » lus).
5. **Clics de démarrage du jeu (accueil, « Enter BitCraft »)** : **pas codés** (contraire à D-052 tel qu'écrit) → question Q-028.

## Conséquences
- Tests 708/708 (7 nouveaux, `V0430Tests`). Essai sans le jeu propre (08:05).
- Toujours rien d'essayé en jeu : premier essai avec Julien.
