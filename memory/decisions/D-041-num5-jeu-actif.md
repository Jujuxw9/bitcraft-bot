# D-041 — Num 5 n'agit que si BitCraft est la fenêtre active

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (« num 5 seulement en jeu, si autre programme il ne doit rien se passer »)
- **Liens** : complète D-022 ; résout Q-022

## Contexte
Essai long du 2026-11-19 : en tapant « 50 » au pavé numérique dans le Bloc-notes, Julien a déclenché l'arrêt d'urgence.

## Décision
- La touche **Num 5** arrête le bot **seulement si BitCraft est la fenêtre active** (au premier plan).
- Dans un autre programme (y compris la fenêtre du bot), elle ne fait rien. Le journal le note au niveau « détail ».
- Le bouton « Arrêter » de la fenêtre du bot reste toujours disponible.

## Conséquences
- Quand le bot agira sur le jeu (H3 et après), le jeu sera au premier plan : Num 5 marchera.
- Num 0 (capture) : ignorée si le jeu est caché par une autre fenêtre (v0.3.13).
