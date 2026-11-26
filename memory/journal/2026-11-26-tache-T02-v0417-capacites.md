# Tâche planifiée T-02 — 2026-11-26 — v0.4.17 : capacités automatiques

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Durée / poste** : 08:45 → ~09:10, PC de Julien (jeu fermé, bot de Julien fermé)

## Objectif
Quand Julien récolte lui-même, lancer la capacité de la barre d'action qui va avec le métier dès que sa recharge est finie, avec une réserve d'endurance de 30 % (prompt : `taches-planifiees/T-02-v0417-capacites.md`).

## Réalisé
- Recherche dans les données du jeu : le champ métier des capacités vaut toujours « ANY » ; métier déduit des effets du bonus. Tableau dans [capacites.md](../technique/capacites.md). Wiki pas consulté (les données suffisaient).
- Logique testable `AbilityPlanner` (récolte en cours, bon métier, recharge, bonus actif, réserve 30 %, une touche toutes les 2 s, pause 5 min après deux échecs).
- Interface : interrupteur « Capacités auto » dans BOT › OPTIONS, liste des capacités de la barre avec un interrupteur et un état chacune, détail au survol. Journal : liste de la barre au démarrage, une ligne par appui.
- Touches de la rangée du haut (1 à 0, -, =) ajoutées aux touches autorisées du contrôleur d'actions.
- Tests : 16 nouveaux (`V0417Tests.cs`) ; **610 réussis sur 610** ; build 0 avertissement.
- Essai réel (`--sans-lancer-jeu`, bot d'essai de `bin\Debug`, 08:55) : barre lue sur le serveur, journal « Barre d'action : 4 Sprint (pas liée à une récolte) ; 5 Reel In (coupée, Fishing) ; 6 Let It Run (coupée, Fishing) ; 7 Hewing Strikes (allumée, Forestry) ; 8 Heavy Swings (allumée, Mining) ; 9 Equip Bone Tipped Arrows (coupée, Hunting) ; 0 Break the Roots (coupée, Forestry) ». Capture PrintWindow de la fenêtre du bot réussie : interrupteurs affichés, Heavy Swings marquée « endurance (réserve 30 %) » (endurance 50/100, coût 25). Bot d'essai fermé par son numéro.
- .exe `BitcraftBot-v0.4.17.exe` publié dans `artifacts/release` (v0.4.16 rangée dans `artifacts/old`). Pas de Release ni de tag.

## Choix prudents faits seul
- Interrupteur général **coupé** au premier démarrage (aucune touche n'a pu être essayée en jeu).
- Allumées par défaut : seulement Hewing Strikes et Heavy Swings (recharge longue). Break the Roots, Reel In, Let It Run (1 s) et les flèches coupées.
- Seule la première barre d'action est utilisée.

## Décisions prises (liens D-NNN)
- [D-069](../decisions/D-069-v0417-capacites-auto.md).

## Questions ouvertes créées (liens Q-NNN)
Aucune. Questions pour Julien (aussi dans `taches-planifiees/etat.md`) :
1. La touche est envoyée par position physique (`&` pour 1, `è` pour 7 en AZERTY) : le jeu lance-t-il bien la capacité ?
2. Veux-tu Break the Roots (recharge 1 s, 10 d'endurance, +1 % de vitesse) lancée en boucle en coupant du bois ? Coupée pour l'instant.
3. Pêche : faut-il automatiser Reel In / Let It Run (mini-jeu) ? Coupées pour l'instant.
