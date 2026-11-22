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
| [T-20261118-06](T-20261118-06-v0311-essai-reel-h2.md) | 2026-11-18 | session réelle | Essai H2 de Julien (v0.3.11) | OK : carte, santé, zone, journal ; défauts : cible instable → v0.3.12 |
| [T-20261119-01](T-20261119-01-v0312-essai-long.md) | 2026-11-19 | session réelle (42 min) | Essai long v0.3.12 | OK : liaison, mémoire stable, familles, cible ; défauts : Num 0 / Num 5 hors jeu, lecture d'une fenêtre devant le jeu → v0.3.13 |
| T-20261119-03 (tests `V0318Tests` + rendu hors écran) | 2026-11-19 | unitaire + rendu | v0.3.18 : contrôleur d'actions (10 cas), fenêtre rendue à 1920 / 1200 / 800 px | OK (264 tests) ; 2 défauts trouvés au rendu et corrigés ; essai réel : `echanges/2026-11-19-essai-v0318.md` |
| T-20261119-02 (tests `V0314Tests`) | 2026-11-19 | unitaire + vraies données | v0.3.14 : poches (3 formats), requêtes d'inventaire, livre des recettes (données de test et vraies données) | OK (229 tests) ; essai réel : `echanges/2026-11-19-essai-v0314.md` |
| T-20261119-04 (tests `V040Tests` + banc hors écran) | 2026-11-19 | unitaire + banc | v0.4.0 (R1) : formule de la caméra sur caméra simulée, faux indices, appariement clic / récolte ; carte Caméra, enregistrement des clics, écoute de la souris | OK (277 tests) ; essai réel : `echanges/2026-11-19-essai-v040.md` |
| T-20261122-01 (essai réel R1 + tests `V041Tests` + banc) | 2026-11-22 | session réelle + unitaire + banc | Essai R1 de Julien (17 clics) ; v0.4.1 : ce que voit le bot (capture en jeu, chargement réel, situations), précision « pas fiable » | R1 : **KO** (4 clics gardés sur 17, écarts 148 à 3 375 px : W ne fixe pas la direction) ; v0.4.1 : OK (300 tests) ; essai : `echanges/2026-11-22-essai-v041.md` |
| T-20261123-01 (essai réel v0.4.2 + tests `V043Tests` + planche + banc) | 2026-11-23 | session réelle + unitaire + visuel + banc | Journal de l'essai v0.4.2 de Julien ; v0.4.3 : direction de la caméra sur 5 mini-cartes de référence et une planche de 48, fenêtres du jeu sur toutes les captures, écrans accueil / connexion / jeu avec bandeau, lectures de texte simultanées | v0.4.2 : connexion **KO**, reste OK ; v0.4.3 : OK (336 tests), démarrage hors écran OK ; **pas essayé en direct dans le jeu** ; essai : `echanges/2026-11-23-essai-v043.md` |

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
