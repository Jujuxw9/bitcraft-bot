# Tâche T-09 (nuit du 27/11, 05:00) — étude du déplacement automatique (sans code)

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Livrable** : `technique/deplacement-etude.md` (8 sections, plan pour T-10, questions pour Julien)

## Fait
- Lecture de la demande de Julien, D-072, D-074, règles de la nuit §3, analyse de la grande carte (§19, captures).
- Lecture du code serveur officiel : marche (`player_move`), escalade (`player_climb`), mort et réapparition (`player_death`, `player_respawn`), tables ennemis / menace / santé.
- Lecture du code du bot : contrôleur d'actions (touches permises), détection de la carte, OCR, relief gardé, position et destination lues.
- Registre du jeu (réglages de Julien) : seulement les valeurs « BindingOverrides » (touches redéfinies) ; jamais le jeton.

## Trouvé
- Le chemin est calculé par le jeu sur le PC ; le serveur ne fait que vérifier chaque pas. Ce que fait « Set destination » (contourner ou non) est à voir en jeu.
- La destination lue sur le serveur est la fin du pas en cours, peut-être pas le point cliqué.
- Escalade dès 5 de hauteur entre deux cases voisines ; nage dès 5 de profondeur d'eau. Base de la vérification du trajet.
- À terre = vie à 0 ; Get Up = relevé sur place avec 10 de vie ; Go Home = téléporté chez soi. La mort fait tomber le cargo.
- Le jeu lit les touches par position physique (Interact = `/Keyboard/semicolon` = M en AZERTY). Touche de la carte : 0x34 ou 0x53, à essayer.
- Les touches `-` et `=` (chariot, bateau) sont permises aujourd'hui par le contrôleur (cases 11 et 12) : T-10 doit les interdire.

## Pas fait
- Aucun code, aucune version, aucun essai en jeu (interdit par la fiche).
