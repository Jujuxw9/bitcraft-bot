# Échange — 2026-11-16 — Feuille de route et informations demandées

- **Agent** : Claude Code
- **Participants** : Julien, Claude Code

## Décisions de l'échange
- D-019 : français partout, sauf les lignes de code (remplace D-012, résout Q-009).
- D-020 : le dernier `.exe` validé est versionné dans `executable/`, les anciens vont dans `executable/anciennes-versions/` (non poussé).

## Étape 1 — Réponses rapides de Julien

| # | Question | Pourquoi |
|---|---|---|
| R1 | Première ressource à récolter, zone, outil possédé (Q-006) | Cible du MVP |
| R2 | Comment le personnage se déplace : clic au sol, ZQSD/WASD, les deux ? | Choix de la méthode de navigation (Q-002) |
| R3 | Comment on récolte : clic sur la ressource, touche d'interaction (laquelle) ? maintenir ou cliquer une fois ? | Séquence d'actions du bot |
| R4 | Caméra : rotation à la souris (quel bouton), zoom molette, caméra libre ou fixe ? | Vision et navigation |
| R5 | Touche d'arrêt d'urgence voulue (proposition : `F12` ou `Pause`), à vérifier qu'elle n'est pas utilisée par le jeu | D-004 |
| R6 | Durée maximale d'une session de test (proposition : 15 min) | D-004 |
| R7 | Distance et temps de trajet entre la zone de récolte et la Banque | Faisabilité du MVP |
| R8 | Mode de travail : Claude code tout et explique, ou Julien écrit une partie du code ? | Projet d'apprentissage |

### Réponses de Julien (2026-11-16)
- R1 : arbres → bois, hache, zone au choix de Claude → D-021.
- R2 : clic au sol aujourd'hui ; ZQSD/WASD possible et sans doute plus simple → Q-011.
- R3 : survol (contour vert) puis **un** clic droit ; l'action continue tant qu'il y a endurance et place → D-021.
- R4 : ZQSD/WASD bouge la caméra, W recentre (incohérence à lever) → Q-011.
- R5 : pavé numérique, Claude choisit → `Num 5` (D-022).
- R6 : 15 min (D-022).
- R7 : inconnu, de 15 s à 2 min ; il faut savoir trouver la Banque → Q-002.
- R8 : Claude code tout, Julien suit l'architecture → D-023.
- Vidéo V1 : **abandonnée** pour l'instant, captures seules.
- Captures : déposées dans `images/analyse/` sans ordre ni nommage ; Claude trie, renomme et décrit.

## Étape 2 — Captures d'écran (dans `images/analyse/`)

Conditions : 1920×1080 fenêtré sans bordure, échelle UI par défaut, jeu en anglais, monde 1.0 (D-006, D-015). Format PNG, écran entier. Nommage : `20261116_analyse_sujet_001.png`.

| # | Capture | Sert à |
|---|---|---|
| C1 | Écran complet au repos, sans menu ouvert | Repérer tout le HUD |
| C2 | Même chose à 3 endroits différents (coordonnées et zone différentes) | Zone OCR position (D-018) |
| C3 | Inventaire ouvert : vide, à moitié plein, plein | Détecter « inventaire plein » |
| C4 | Ressource visée : sans survol, puis souris dessus (surbrillance, info-bulle) | Q-001 |
| C5 | Pendant la récolte (barre de progression, animation) et juste après (ressource épuisée) | Détecter fin de récolte |
| C6 | Message d'erreur typique (mauvais outil, trop loin, plus d'endurance) | Détection d'anomalie |
| C7 | Banque : vue extérieure de loin, entrée, interface de dépôt ouverte | D-016 |
| C8 | Carte du monde ouverte | Q-002 |
| C9 | Écrans anormaux : chargement, mort, déconnexion, menu Échap | Arrêt sur anomalie (D-004) |
| C10 | Page des réglages de touches du jeu | R2 à R5 |
| V1 | **Vidéo** de 30 s à 1 min : une récolte complète puis trajet vers la Banque | Vision d'ensemble, rythme |

## Étape 3 — Analyse (Claude)
- Décrire chaque capture dans `images/README.md`.
- Instruire Q-001 (perception 3D) et Q-002 (navigation) avec des preuves.
- Choisir le moteur OCR et la méthode de capture (nouvelles décisions).
- Chercher une source de valeurs de données (Q-004). **À vérifier** : exports communautaires existants.

## Étape 4 — Versions prévues

Chaque version est testée en jeu par Julien (fichier `T-…`), puis validée (D-020).

| Version | Contenu | Agit sur le jeu ? |
|---|---|---|
| v0.1 | Capture de la fenêtre du jeu affichée en direct dans la fenêtre de supervision, arrêt d'urgence, journal | Non |
| v0.2 | Lecture du HUD : position, zone, région, inventaire, affichés en direct | Non |
| v0.3 | Détection de la ressource choisie, encadrée sur la capture | Non |
| v0.4 | Première action : récolter une ressource à l'écran, une seule fois, sous supervision | **Oui** |
| v0.5 | Boucle de récolte jusqu'à inventaire plein | Oui |
| v0.6 | Trajet vers la Banque, dépôt, retour = **MVP** (D-002) | Oui |

Les 3 premières versions sont sans risque pour le compte : le bot regarde, il ne touche à rien.

## Questions que Julien peut poser à tout moment
- « Explique-moi comment marche tel module / telle version. »
- « Pourquoi ce choix plutôt qu'un autre ? »
- « Qu'est-ce que je dois tester et comment ? »
- « Où en est-on ? » (réponse dans `resume-global.md`)

## Mise à jour du plan (2026-11-16, fin de session 08)

| Version | Contenu | État |
|---|---|---|
| v0.3.0 à v0.3.4 | Lecture fiable : position par modèles de chiffres, jauge de métier, données du jeu (D-033), capture Num 0, diagnostics, notifications de niveau | **Fait** (`artifacts/v0.3.4/`) |
| v0.3.x (bêta) | Onglet **« Bêta : exploration »** : déplacements autonomes dans l'herbe, survol avant clic (jamais de contour vert), éviter l'eau, caméra avec W (Q-020) | **Prochaine**, après les réponses de Julien |
| v0.4 | Première vraie action : viser un vieil arbre (plus grand contour), clic droit, lire le nom, annuler si jeune (D-036), mouvements avec un but (D-037) | À venir |
