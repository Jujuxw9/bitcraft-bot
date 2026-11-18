# Tests — index

| ID | Date | Type | Titre | Résultat |
|---|---|---|---|---|
| [T-20261116-01](T-20261116-01-v01-affichage.md) | 2026-11-16 | intégration | v0.1 : affichage du jeu et arrêt d'urgence | OK (Verr. Num désactivé non testé) |
| [T-20261116-02](T-20261116-02-v02-lecture-hud.md) | 2026-11-16 | perception | v0.2 : lecture du HUD | Essais Claude OK ; à vérifier par Julien (dont de nuit) |
| [T-20261116-03](T-20261116-03-v022-mouvements.md) | 2026-11-16 | session en jeu | v0.2.2 : mouvements souris et caméra | OK : gestes naturels ; destinations à rendre utiles (D-037) |
| [T-20261116-04](T-20261116-04-v030-coordonnees-donnees.md) | 2026-11-16 | perception | v0.3.1 : coordonnées par modèles, données du jeu, jauge de métier | OK (position 98-100 %, nuit OK) ; un trou de la jauge à comprendre |
| [T-20261118-01](T-20261118-01-h1-lecture-serveur.md) | 2026-11-18 | unitaire + intégration | H1 : lecture du serveur (essais de Claude, sans jeton) | OK ; essai réel à faire par Julien |
| T-20261118-02 (dans `echanges/2026-11-18-essai-h1.md`) | 2026-11-18 | session réelle | Essai H1 de Julien (v0.3.5) | KO : jeton OK, région 18 refuse la liaison → v0.3.6 |
| T-20261118-03 | 2026-11-18 | session réelle | Essai H1 de Julien (v0.3.6) | KO : modules `bitcraft-live-*` = ancien monde → v0.3.7 (`idc-*`) |
| [T-20261118-04](T-20261118-04-v037-v038-essai-reel-h1.md) | 2026-11-18 | session réelle | Essai H1 de Julien (v0.3.7, v0.3.8) | OK : lecture serveur et déconnexion sûre ; KO : connexion avec le jeu ouvert = jeu éjecté → v0.3.9 |
| T-20261118-05 (tests `H2MapTests`) | 2026-11-18 | unitaire | H2 (v0.3.11) : coordonnées N / E sur valeurs réelles, direction, âge, santé, requêtes de lecture | OK (200 tests) ; essai réel : `echanges/2026-11-18-essai-h2.md` |

## Gabarit

```markdown
# T-AAAAMMJJ-NN — Titre
- **Type** : unitaire | perception | intégration | session en jeu
- **Version testée** : vX.Y.Z (commit)
- **Durée** :
## Objectif
## Conditions (résolution, réglages jeu, zone)
## Procédure
## Résultat (OK / KO, métriques)
## Observations et images (liens images/)
## Suites
```
