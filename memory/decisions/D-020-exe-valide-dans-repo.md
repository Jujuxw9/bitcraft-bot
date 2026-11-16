# D-020 — Le .exe validé est versionné dans le repo

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-010, Q-010

## Contexte
Julien veut trouver directement dans le repo le dernier `.exe` qui fonctionne.

## Décision
- Dossier `executable/` : contient **uniquement** le dernier `.exe` validé par Julien (`BitcraftBot-vX.Y.Z.exe`).
- Dossier `executable/anciennes-versions/` : les `.exe` précédents y sont déplacés. Ce dossier est **ignoré par Git** (reste sur le PC, jamais poussé).
- Un `.exe` n'entre dans `executable/` qu'après validation par Julien (test en jeu OK).
- Le script `tools/valider-exe.ps1` fait l'opération : publication, archivage de l'ancien, copie du nouveau.
- Les GitHub Releases (D-010) restent en place.

## Alternatives écartées
- Releases seules : moins visible pour Julien.

## Conséquences
- Publication avec compression (`EnableCompressionInSingleFile`) obligatoire. Mesure du 2026-11-16 sur le squelette v0.0.1 : **125,5 Mo** brut (refusé par GitHub, limite 100 Mo par fichier), **58,3 Mo** compressé (accepté, avec avertissement au-delà de 50 Mo).
- OpenCvSharp et ONNX Runtime vont alourdir le `.exe`. Risque de dépasser 100 Mo. À surveiller à chaque version.
- Suivi de la taille : v0.1.0 = 62 Mo, v0.2.0 = 68 Mo (moteur OCR de Windows). Limite dure GitHub : 100 Mo (rappelée par Julien le 2026-11-16). Objectif : rester bien en dessous, en évitant les grosses bibliothèques quand une solution simple suffit.
- **Git garde tout l'historique** : un `.exe` supprimé ou déplacé reste dans l'historique. Chaque version validée ajoute ~60 Mo au clone, définitivement. Voir Q-010.
