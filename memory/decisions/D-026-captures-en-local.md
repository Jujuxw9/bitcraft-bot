# D-026 — Captures d'écran en local, pas dans Git

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-013 (complétée), Q-010

## Contexte
Le lot 1 pèse 116 Mo. Julien va envoyer beaucoup plus de captures. Git garde tout pour toujours, et le repo est public (pseudos de joueurs et chat visibles sur les captures).

## Décision
- Les images restent **sur le PC uniquement** : `images/analyse/`, `entrainement/`, `tests/`, `sauvegarde/`, `archives/` sont ignorés par Git.
- **Sont versionnés** : `images/README.md` (index et descriptions) et `images/reference-ui/` (petits découpages d'interface dont le code a besoin).
- La structure des dossiers et le nommage de D-013 ne changent pas.

## Alternatives écartées
- Tout versionner (D-013 d'origine) : repo trop lourd.
- Git LFS : quota limité, inutile pour des images de travail.

## Conséquences
- **Pas de sauvegarde automatique.** Si le disque meurt, les captures sont perdues. Conseil : copier `images/` de temps en temps sur un disque externe ou un cloud.
- Les 96 captures déjà poussées restent dans l'historique Git (116 Mo). Les retirer demande de réécrire l'historique (voir Q-012).
- Un autre PC ou un autre agent n'aura pas les images : l'index `images/README.md` décrit chaque capture.
