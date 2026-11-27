# Tâche T-13 (nuit du 27/11, 09:00) — v0.4.31 : petits défauts

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Résultat** : v0.4.31 publiée (`artifacts/release/BitcraftBot-v0.4.31.exe`), tests 730/730, essai sans le jeu (voir plus bas). D-088.

## 1. Cible lue à l'écran : corrigé
- Journaux : la cible est lue jusqu'au 23/11 20:04 (v0.4.3, « Mature Oak Tree 11743/12000 »), plus jamais à partir de 23:01 (v0.4.5). Aucun changement du code de lecture entre les deux.
- Capture du 23/11 23:08 (récolte de lavande, `images/analyse/20261123_analyse_capture-bot_230804.png`) : le panneau de la cible est devenu **sombre avec des textes clairs** (mise à jour du jeu). Les textes sont aux mêmes places. Le bot cherchait le fond crème : panneau jamais vu.
- Correction : reconnaissance des deux apparences ; pour la sombre, 4 points (bord, cadre du haut, fond) avec leur couleur exacte, ce qui exclut les menus plein écran (fond uni, sans cadre). Texte lu en « clair ». « Tier I » → « Tier 1 ».
- Tests sur 4 bandes du haut de vraies captures (`images/reference-ui/cible-sombre/`, recadrées : ni chat ni nom de joueur) : Lavender 40/550 Tier 1 - Flower, Rough Hieroglyphs 1700/1700 Tier 1 - Research, Simple Fishing Station 120/120, et un menu (pas de cible). L'ancienne capture crème est toujours reconnue.

## 2. Barre de fabrication : cause probable trouvée, corrigée
- Code du serveur (`craft.rs`, `progressive_action_state.rs`, `craft_collect.rs`) : la fabrication reste à 100 % jusqu'au ramassage. La barre devait donc atteindre 100 %… sauf dans deux cas : 1) d'autres fabrications en cours dans d'autres ateliers : à la fin, l'action disparaît avec la recette, le bot ne savait plus laquelle suivre (« en cours ») ; 2) objets ramassés tout de suite : plus rien à afficher.
- Correction : recette et dernière part retenues jusqu'à la fin de l'affichage (2,5 s) ; « Fabrication · terminée ». Les journaux n'écrivent pas la barre : impossible de prouver lequel des cas Julien a vu (à vérifier en jeu).

## 3. Conseil pêche / minage sans capacités : fait
- Une capacité « est partie » si son début de recharge (serveur, `ability_state`) change pendant la tâche, ou tombe 5 s au plus avant le premier coup. Vu dans les journaux du 26/11 : une capacité ne coupe pas la tâche en cours.
- Fin d'un minage (Mining) ou d'une pêche (Fishing) d'au moins 20 s, aucune capacité du métier présente sur la barre partie → `[Conseil] Minage de 3 min 05 s sans Heavy Swings : pensez à l'allumer (Capacités auto).` Rien si aucune capacité du métier n'est sur la barre.

## Tests
- `dotnet build` : 0 avertissement. `dotnet test` : 730/730 (22 nouveaux, `V0431Tests`).
- Essai sans le jeu (09:09:24, `--sans-lancer-jeu`, 45 s, fermé proprement) : serveur connecté, capacités reçues (`ability_state` 14), barre lue (8 Heavy Swings, Mining ; 5 Reel In et 6 Let It Run, Fishing) ; aucun « Abonnement refusé », aucune ERREUR. Refus du module global à l'ouverture : connu, normal sans le jeu. Pas de capture de la fenêtre du bot (rien de visible sans le jeu : la cible et la barre ne s'affichent qu'en jeu).

## Incohérence signalée
- La fiche range Break the Roots (touche 0) avec le minage ; les données du jeu la rattachent au **bois (Forestry)**. Le conseil suit les données du jeu : pour le minage, seulement Heavy Swings.
