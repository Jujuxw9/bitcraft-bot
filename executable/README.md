# Exécutable validé

Ce dossier contient **uniquement** le dernier `.exe` validé par Julien (D-020).

- Nom : `BitcraftBot-vX.Y.Z.exe`.
- Les anciennes versions vont dans `anciennes-versions/` (sur le PC uniquement, ignoré par Git).
- Pour valider une nouvelle version :

```bash
powershell -ExecutionPolicy Bypass -File tools/valider-exe.ps1 -Version 0.1.0
```

Puis commit `chore: exe validé v0.1.0`.

## Version actuelle

Aucune (la v0.0.1 est un squelette, pas un bot).

| Version | Date de validation | Test associé |
|---|---|---|
