# Historique du stockage des images

## Ce qui s'est passé
- **2026-11-16 (matin)** : les 96 premières captures (lots 1 et 2, 116 Mo) ont été poussées sur GitHub, comme prévu à l'origine (D-013).
- **2026-11-16 (après-midi)** : Julien prévoit beaucoup plus de captures. Décision D-026 : les captures restent sur le PC. Elles ont été retirées du suivi Git (commit `d1ca223`).
- Les 96 captures restent visibles dans l'historique de GitHub (anciens commits). Choix de Julien : **on ne réécrit pas l'historique**. Le dépôt reste propre et simple (Q-012 résolue).

## Ce qui se passe maintenant
- Toutes les captures sont **sur le PC de Julien uniquement** (`images/analyse/`, `entrainement/`, `tests/`, `sauvegarde/`, `archives/`).
- Sur GitHub, il n'y a que :
  - [README.md](README.md) : règles de nommage et **index de chaque capture** (une ligne par image, avec sa description) ;
  - ce fichier ;
  - `reference-ui/` : petits découpages d'interface dont le bot a besoin pour fonctionner.
- Un agent IA qui n'a pas le PC de Julien peut lire l'index pour savoir ce que montre chaque capture.

## Conseil
Copier le dossier `images/` de temps en temps sur un disque externe ou un cloud : il n'y a pas d'autre sauvegarde.
