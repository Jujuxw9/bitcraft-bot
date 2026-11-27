# Tâche T-14 — v0.4.32 : aller à une ressource, la récolter jusqu'à épuisement

- **Date** : 2026-11-27, 10:00 à 10:35 (tâche planifiée, sans Julien)
- **Agent** : Claude Code (Opus 5.5)

## Fait
- Menu sur une ressource (carte, tableau, bouton « Y aller… » de la plus proche) : « Aller à cette ressource », « Récolter jusqu'à épuisement ».
- Trajet vers une case voisine (2 cases) avec les refus de « Se déplacer ici ».
- Récolte : premier coup laissé à Julien (aucune méthode sûre sans essai réel), puis la reprise des récoltes continue ; suivi jusqu'à « épuisée » ; fuite si la vie baisse. D-089, architecture-suite §36.

## Testé
- Build 0 avertissement, tests 742/742 (12 nouveaux, `V0432Tests`).
- v0.4.32 publiée ; essai sans le jeu (10:05, 55 s, `--sans-lancer-jeu --essai-menu-deplacement`) : menu « Aller à cette ressource : Daisy (T1) / Récolter jusqu'à épuisement » à 3,7 cases, case voisine choisie, trajet refusé (jeu fermé) ; aucun « Abonnement refusé », aucune ERREUR.

## Pas fait
- Premier coup automatique (à faire après un essai réel). Pas de capture de la fenêtre du bot.

## Questions pour Julien
- 2 cases de la ressource : assez près pour récolter ?
- Le premier coup automatique (W, contour vert, cible du serveur) : à essayer ensemble ?
