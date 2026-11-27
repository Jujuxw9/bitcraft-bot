# Q-028 — Le bot peut-il faire seul les deux clics de démarrage du jeu ?

- **Statut** : Ouverte
- **Priorité** : Moyenne (utile pour les essais de nuit, pas pour le jeu de tous les jours)
- **Créée le** : 2026-11-27 (tâche T-12, d'après l'essai raté de T-11)
- **Liens** : D-052, D-066 (démarrage en séquence), D-086, D-087 ; `journal/2026-11-27-tache-T11-essai-reel.md`

## Question
Quand le bot lance le jeu lui-même (Steam), le jeu s'arrête sur l'écran d'accueil « Press any button to start », puis sur l'écran de connexion (« Enter BitCraft »). D-052 interdit au bot toute action hors de l'écran « en jeu ». En tâche planifiée, Claude n'a pas non plus accès à l'écran : personne ne peut faire ces deux clics.

1. Le bot peut-il faire ces deux clics lui-même, **seulement quand c'est lui qui a lancé le jeu** (exception limitée à D-052, comme celle de la grande carte en v0.4.29) ? Jamais « Quit ».
2. Sinon : ajouter « BitCraft Online » aux applications permises des tâches planifiées (réglage de Julien) ?

## Pistes / options
- A. Exception codée : clic au centre de la fenêtre sur l'accueil, puis clic sur « Enter BitCraft » lu à l'écran ; coupée par défaut, allumée par une option ou par `--essai-deplacement`.
- B. Pas de code : Julien fait les deux clics, ou permet l'accès à l'écran en tâche planifiée.

## Ce qui bloque
La réponse de Julien. T-12 n'a rien codé (choix prudent).
