# Passation de la session 28 (Opus 5.5, 25/11 → 26/11) pour la session suivante

## Phrase à donner (Julien la colle au début du nouveau chat)

> Reprends le projet bitcraft-bot : lis CLAUDE.md, puis memory/echanges/2026-11-26-passation-session-28.md et suis-le. Ne lance aucune autre session ni aucun agent sans mon accord.

## Ta mission, dans l'ordre
1. Lire `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md` (D-064 à D-067 surtout), puis ce fichier.
2. `git pull --rebase` (ignorer « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet).
3. Lire **`memory/taches-planifiees/etat.md`** : quatre tâches planifiées ont tourné (ou devaient tourner) le 26/11 sans Julien. Faire le bilan à Julien, simplement : ce qui est fini, ce qui a échoué, ce qu'il doit essayer.
4. Si Julien donne le **rapport de vérification de la mémoire** (`artifacts\rapports\rapport-memoire-2026-11-26.md`, écrit par T-04) : juger chaque recommandation, appliquer les corrections sûres, poser à Julien les questions « à trancher », commit et push.
5. Analyser les journaux des essais de Julien (`artifacts/release/journaux/v0.4.15` et suivants), puis lui demander la suite. **Une question à la fois au moindre doute, et attendre son go avant de coder.**

## Ce qui a été fait en session 28
- Bilan des journaux v0.4.9 à v0.4.13 ; demandes de Julien du 25 et du 26/11 : `echanges/2026-11-25-demandes-julien-session-28.md` (sections 1 à 5).
- **v0.4.14** (D-065) : barres animées, PERSONNAGE (banques regroupées, lieux au survol, flèches de tri), curseur main seulement sur le cliquable, filtre par palier sur la CARTE, PARAMÈTRES refait par importance, overlay, touche tenue (W) comptée une fois.
- **v0.4.15** (D-066) : démarrage automatique en séquence (serveur → lecture de l'écran → jeu lancé par Steam, `steam://rungameid/3454650` ; jeu déjà lancé = rien ne démarre) ; overlay entre les icônes de gauche et la barre d'action ; PERSONNAGE (images, logos Sac / Ceinture / Banques, colonne « Tier », tris par défaut) ; VOYAGEURS (portraits du wiki, moins de texte, matières premières « À récolter », stock déduit à chaque niveau) ; jeton dans Compte ; zone de ressources 20 cases ; texte du bureau plus écrit au lancement du jeu. Options d'essai : `--sans-demarrage-auto`, `--sans-lancer-jeu`.
- Analyse de la carte de bitjita.com : `technique/analyse-carte-bitjita-2026-11-26.md`. Décision de Julien : dessiner à partir du serveur du jeu ; bitjita seulement en secours.
- **Tâches planifiées** (D-067, `memory/taches-planifiees/`) : T-01 v0.4.16 archive du chat (03:30), T-02 v0.4.17 capacités (08:45), T-03 v0.4.18 carte (13:15), T-04 vérification de la mémoire en lecture seule (14:00). Réglées par Julien en Opus 5.5, effort moyen.

## Pas encore vérifié par Julien (« À vérifier »)
- v0.4.15 : séquence de démarrage jusqu'au jeu (jamais essayée en vrai : le jeu était ouvert pendant l'essai de Claude), portraits, ligne « À récolter », position de l'overlay, charge du PC avec 20 cases.
- v0.4.14 : curseur main (test automatique impossible, écran verrouillé).
- Tout ce que les tâches T-01 à T-03 ont livré (voir `etat.md`).

## Plus tard (à rappeler à Julien)
- **Caméra** : à reprendre et améliorer (Julien l'a demandé).
- **Exploration automatique** (demande très importante de Julien, `echanges/2026-11-25-demandes-julien-session-28.md` section 5 point 8) : interrupteur de mode dans BOT (Normal / Exploration) ; exploration en spirale autour de Julien vers les zones non découvertes ; escalade (F ou clic) repérée à l'écran ; eau et relief lus sur le serveur (`terrain_chunk_state`) ; ennemis (`enemy_state`, `threat_state` : fuir en courant si un ennemi cible Julien). **Session à part**, à planifier avec Julien.
- Site des recettes sur le serveur Unraid de Julien, même dépôt, dossier à part (D-064) : session à part, prompt à préparer.

## Questions closes
- Clic inconnu du 24/11 à 00:03:48 : classé sans réponse. Message du 25/11 vers 18:49 : Julien ne sait plus.

## Méthode (rappel)
Français partout sauf le code ; Julien n'est pas technique : phrases courtes ; tester chaque livrable (tests + essai réel) ; rappeler en fin de message les questions sans réponse ; jamais de session ni d'agent sans son accord ; .exe local seulement, jamais de Release ni de tag ; ne jamais fermer ses programmes ; le jeu doit être fermé avant toute connexion du bot.
