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

**v0.1.0** : affichage du jeu en direct, arrêt d'urgence Num 5, durée max, journaux. N'agit pas sur le jeu.

| Version | Date de validation | Test associé |
|---|---|---|
| v0.1.0 | 2026-11-16 | [T-20261116-01](../memory/tests/T-20261116-01-v01-affichage.md) |
