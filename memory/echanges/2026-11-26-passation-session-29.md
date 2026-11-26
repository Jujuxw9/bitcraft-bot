# Passation de la session 29 (Opus 5.5, 26/11) pour la suite après /compact

## Phrase à donner (Julien la colle après le /compact ou dans un nouveau chat)

> Reprends le projet bitcraft-bot : lis CLAUDE.md, puis memory/echanges/2026-11-26-passation-session-29.md et suis-le. Ne lance aucune autre session ni aucun agent sans mon accord.

## Ta mission, dans l'ordre
1. Lire `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md` (D-068 à D-078 surtout), puis ce fichier.
2. `git pull --rebase` (ignorer « [SYNC PC-PERSO] » et les commits « [PERSO JULIEN] » : autre projet). **Une autre session (« site des recettes ») travaille dans le même dossier** : `git status` avant `git add`, ne commiter que ses propres fichiers ; numéros D-NNN : vérifier le dernier pris avant d'en créer un.
3. Analyser le journal de l'essai de la **v0.4.22** (`artifacts/release/journaux/v0.4.22/`) : zone de 30 cases acceptée par le serveur (pas de « Abonnement refusé ») ? barre de fabrication ? RAM ? mémoire de la carte (ligne « Mémoire de la carte : N morceaux ») ?
4. Faire le bilan à Julien, puis lui reposer les questions en attente (ci-dessous), **une à la fois au moindre doute**, et attendre son go avant de coder.

## Ce qui a été fait en session 29
- Bilan des tâches planifiées T-01 à T-04 (v0.4.16 à v0.4.18, rapport de la mémoire) ; corrections sûres du rapport ; T-04 écrite dans D-067.
- **v0.4.19** (D-071) : le serveur refusait les ressources (nouvelle limite d'estimation) ; requêtes par morceau de carte (`chunk_index`), repli qui ne retire que la partie en trop.
- **v0.4.20** (D-073) : fond de carte en hexagones (grandes cases), filtres simplifiés (pierre / minerai séparés), images du personnage × 1,75, chat en direct (récent en bas), activité tenue 2,5 s.
- **v0.4.21** (D-075) : zone de 25 cases, boutons T1 à T5, **Release GitHub v0.4.21**.
- **v0.4.22** (D-078) : carte à 30 cases, un cercle, zoom, mémoire du terrain (dossier `carte` à côté du .exe) et des ressources vues ; vraie progression des fabrications (`progressive_action_state`) ; ceinture en vignettes ; RAM 1000 / 1600 Mo, pics brefs ignorés. Tests 632/632. **.exe fabriqué, pas encore essayé par Julien, aucun essai serveur** (jeu ouvert).
- Décisions de Julien : D-072 (onglet Exploration, sessions 30 min à 4 h, fuite des ennemis), D-074 (déplacements par « Set destination »), **D-076 (Release seulement par palier : prochaine en 0.5.x)**.
- Site des recettes : prompt écrit (`echanges/2026-11-26-prompt-site-recettes.md`), **session à part lancée par Julien** (D-077 est à elle, `memory/site-recettes/`).

## Questions en attente
1. v0.4.22 : fond de carte à 30 cases ? zoom ? barre de fabrication jusqu'au bout ?
2. **Riverwood** (`/c1`, salon de Julien et 3 amis) : archiver et répondre aux « @gpt ». Salon dans le **module global** → liaison permanente à essayer avec Julien (risque d'éjection). Prévu pour la prochaine version. Détail : `echanges/2026-11-26-demande-deplacement-carte.md`.
3. Capacités : touche 8 (Heavy Swings) en minant pas encore essayée ; Break the Roots en boucle ? pêche ?
4. Carte : demander au serveur seulement les morceaux nouveaux (abonnements par morceau, protocole à vérifier) : à essayer avec Julien.

## Plus tard (à rappeler à Julien)
- **« Se déplacer ici »** depuis la carte du bot (clic droit, ou clic sur une ressource → y aller / la récolter jusqu'à épuisement), « Set destination » seulement, sans escalade : `echanges/2026-11-26-demande-deplacement-carte.md`.
- **Caméra** à reprendre ; **exploration automatique** (session à part).

## Méthode (rappel)
Français partout sauf le code ; Julien n'est pas technique : phrases courtes ; tester chaque livrable (tests + essai réel, **jeu fermé** pour tout essai serveur) ; rappeler en fin de message les questions sans réponse ; jamais de session ni d'agent sans son accord ; .exe local à chaque version, Release seulement par palier ; ne jamais fermer ses programmes ; le bot doit être fermé avant `publier-version.ps1`.
