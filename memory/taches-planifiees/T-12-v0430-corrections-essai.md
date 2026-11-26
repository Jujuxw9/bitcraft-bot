# T-12 (08:00) — v0.4.30 : corrections après l'essai réel

Règles communes : `nuit-2026-11-27-regles.md`. **Lis les sections T-10 et T-11 du bilan** et le journal de T-11 (`memory/journal/2026-11-27-tache-T11-essai-reel.md`), puis le journal du bot de l'essai (`artifacts/release/journaux/<version>/2026-11-27/`) et les captures `images/analyse/20261127_essai-deplacement_*`.

## Pourquoi
T-11 a essayé « Se déplacer ici » dans le vrai jeu. Corriger tout ce qui a raté, pour que Julien puisse l'essayer au réveil.

## À faire
1. Liste des défauts trouvés par T-11, classés : bloquant / gênant / détail. Corriger les bloquants et les gênants (OCR de la barre du bas, échelle, clic, fermeture de la carte, vérification serveur, sécurités…).
2. Pour chaque correction : un test automatique qui reproduit le cas (à partir des lignes de journal ou des captures si possible : la perception a déjà des tests sur images, voir `tests/`).
3. Si T-11 n'a pas pu faire l'essai (conditions, quota, Julien levé) : relire le code de T-10 avec les yeux de l'essai (relire étape par étape ce qui peut rater en vrai : délais d'ouverture de la carte, OCR, focus de la fenêtre), corriger ce qui est sûr, et le dire.
4. **Pas de nouvel essai en jeu** (seule T-11 en avait le droit).

## Fini quand
- v0.4.30 publiée, tests verts, essai sans le jeu OK, bilan : liste des corrections + ce que Julien doit essayer en premier (« Se déplacer ici » à 5-10 cases, regarder le journal « Déplacement »).

## Si rien à corriger
- Le dire, ne pas publier de version vide ; utiliser le temps pour renforcer les tests du déplacement.
