# Modèles de vision

Modèles `.onnx` pour la détection du monde 3D (Q-001). Inférence prévue via ONNX Runtime + DirectML (GPU AMD, pas de CUDA).

## Taille des fichiers

- Modèle **< 50 Mo** : dans `models/`, versionné.
- Modèle **> 50 Mo** : dans `models/large/` (ignoré par Git). Le distribuer autrement (Release GitHub, Git LFS — à décider).

Git ne sait pas ignorer un fichier selon sa taille : la convention `models/large/` remplace cette règle.

## Index

| Fichier | Date | Classes | Entraîné sur | Taille |
|---|---|---|---|---|
