# D-088 — v0.4.31 : cible lue sur le nouveau panneau sombre, barre de fabrication jusqu'au bout, conseil pêche / minage

- **Statut** : Validée (tâche de nuit T-13, choix prudents ; à revoir avec Julien)
- **Date** : 2026-11-27
- **Validée par** : tâche T-13 d'après sa fiche (`taches-planifiees/T-13-v0431-petits-defauts.md`) et les règles de la nuit (D-081)
- **Liens** : D-078 (progression des fabrications), D-079 (consigne de Julien sur les capacités), D-069 (capacités auto), [architecture-suite.md](../technique/architecture-suite.md) section 35

## Contexte
Trois petits défauts : « Perception : cible aucune » en permanence depuis la v0.4.5 ; Julien pas sûr que la barre de fabrication aille au bout pour les petites fabrications ; pas de rappel quand Julien pêche ou mine sans ses capacités.

## Décision
1. **Cible** : cause trouvée. Une mise à jour du jeu (entre le 23/11 20:04 et 23:01) a changé le panneau de la cible : **fond sombre, textes clairs**, aux mêmes places. Le bot cherchait le fond crème : il ne voyait plus jamais le panneau. Il reconnaît maintenant les deux apparences (cadre gris-violet + fond sombre, ce qui exclut les menus plein écran) et lit le texte clair. Un « 1 » lu « I » dans « Tier 1 » est corrigé.
2. **Barre de fabrication** : le serveur garde la fabrication à 100 % jusqu'au ramassage (code serveur vérifié). Deux cas arrêtaient la barre avant la fin : d'autres fabrications en cours dans d'autres ateliers (à la fin, le bot ne savait plus laquelle suivre) et objets ramassés tout de suite (plus rien à afficher). Le bot retient la recette suivie et la dernière part lue ; à la fin, la ligne affiche « terminée ».
3. **Conseil** : en fin de **minage** ou de **pêche** d'au moins 20 s, si aucune capacité du métier présente sur la barre n'est partie (lu sur le serveur : début de recharge), une ligne `[Conseil]` dans le journal : « Minage de 3 min 05 s sans Heavy Swings : pensez à l'allumer (Capacités auto). » Rien si aucune capacité du métier n'est sur la barre, ou si les capacités n'ont pas été lues.

## Conséquences
- Tests 730/730 (22 nouveaux, `V0431Tests`, dont 5 sur de vraies captures recadrées : `images/reference-ui/cible-sombre/`, bande du haut seulement, sans chat ni nom de joueur).
- À vérifier en jeu par Julien : « Perception : cible … » dans le journal en coupant un arbre ; barre « Fabrication · terminée » ; ligne `[Conseil]` après un minage sans Heavy Swings.
