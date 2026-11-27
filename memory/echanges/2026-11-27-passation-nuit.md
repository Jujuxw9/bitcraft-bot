# Passation après la nuit du 27/11/2026 (tâches T-05 à T-16)

## Phrase à donner (Julien la colle dans un nouveau chat)

> Reprends le projet bitcraft-bot : lis CLAUDE.md, puis memory/echanges/2026-11-27-passation-nuit.md et suis-le. Ne lance aucune autre session ni aucun agent sans mon accord.

## Ta mission, dans l'ordre
1. Lire `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md` (D-081 à D-089 surtout), puis ce fichier, puis le **résumé en tête** de `memory/taches-planifiees/bilan-nuit-2026-11-27.md`.
2. `git pull --rebase` (ignorer « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet). `git status` avant `git add` : la session « site des recettes » écrit aussi dans le dossier (`website-recipes/`, `memory/site-recettes/`).
3. Faire le bilan de la nuit à Julien, **court** (le résumé du bilan suffit).
4. Faire avec lui les essais du résumé, **dans l'ordre**, avec la v0.4.33 (Julien fait les clics de démarrage du jeu). Lire le journal après chaque essai (`artifacts/release/journaux/v0.4.33/AAAA-MM-JJ/`, catégories « Déplacement », « Récolte guidée », « Reprise », « Perception »).
5. Corriger d'après les essais (prochaine version : **v0.4.34**), puis trancher Q-029 et Q-028 avec lui, point par point.

## État au 27/11 midi
- Dernière version : **v0.4.33** (`artifacts/release/BitcraftBot-v0.4.33.exe`), tests 756/756. Pas de Release GitHub (D-076 : prochaine en 0.5.x).
- Nuit : v0.4.25 (reprise à la souris, D-082), v0.4.26 (sans onglet Recettes, D-083), v0.4.27 et v0.4.28 (carte, D-084, D-085), étude du déplacement (`technique/deplacement-etude.md`), v0.4.29 et v0.4.30 (« Se déplacer ici », D-086, D-087), v0.4.31 (cible lue, fabrication, conseil, D-088), v0.4.32 (« Récolter jusqu'à épuisement », D-089), v0.4.33 (contrôle du dépôt, `technique/controle-depot-2026-11-27.md`).
- **Rien n'a été essayé dans le vrai jeu** : T-11 bloquée sur l'écran d'accueil (pas d'accès à l'écran en tâche planifiée ; le bot ne fait pas les clics de démarrage, D-052).
- Personnage : N 9041, E 6808, colonie d'une autre guilde, ennemis aux alentours (feu proche). Chariot et bateau rangés.

## À vérifier (jamais vu en jeu)
- Touche qui ouvre la grande carte (0x34 ou 0x53), ordre des nombres de la barre (nord d'abord d'après une capture), échelle, visée, destination du serveur égale au point cliqué ?
- Contour vert mesuré par la reprise à la souris.
- Lecture de la cible sur le panneau sombre en direct ; barre de fabrication jusqu'à 100 %.

## Questions en attente
Toutes regroupées dans le résumé du bilan (16 points) ; fiches **Q-029** (réglages du déplacement) et **Q-028** (clics de démarrage). Anciennes encore ouvertes : liste du `resume-global.md`, ligne 9.

## Plus tard (à rappeler à Julien)
- Premier coup automatique sur la ressource (après un essai réel).
- Riverwood (`/c1`) : liaison en direct à essayer ensemble. Colonies et banques lues une seule fois (abonnements séparés, à essayer ensemble).
- Grosses classes à découper (`ActionController.cs`, `MainWindow.Travel.cs`, `MainWindow.xaml.cs`, `ServerReader.cs`).
- Banque (v0.5), caméra, exploration automatique (session à part).

## Méthode (rappel)
Français partout sauf le code ; Julien n'est pas technique : phrases courtes ; tester chaque livrable (tests + essai réel) ; rappeler en fin de message les questions sans réponse ; jamais de session ni d'agent sans son accord ; .exe local à chaque version, Release seulement par palier ; ne jamais fermer ses programmes ; bot fermé avant `publier-version.ps1`.
