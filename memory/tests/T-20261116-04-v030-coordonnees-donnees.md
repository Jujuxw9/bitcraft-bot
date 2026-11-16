# T-20261116-04 — v0.3.1 : coordonnées par modèles, données du jeu, jauge de métier
- **Type** : perception
- **Version testée** : v0.3.1 (commit indiqué en première ligne du journal)
- **Durée** : 10 min

## Objectif
1. La position doit être lue **presque tout le temps**, de jour comme de nuit.
2. Les données du jeu doivent se télécharger et se vérifier seules.

## Conditions (résolution, réglages jeu, zone)
- Jeu 1920×1080, fenêtré sans bordure, anglais. Endroit calme.
- Bot : `artifacts/v0.3.1/BitcraftBot.exe`. Le bot **n'agit pas** sur le jeu (banc d'essai désactivé par défaut).

## Procédure
1. Lancer le bot. Dans le journal : « Données du jeu chargées … 878 ressources ».
2. Démarrer. **Marcher** dans plusieurs directions pendant 2 à 3 min (fonds variés : herbe, eau, rochers, ciel clair).
3. Couper un arbre 30 s. Le journal doit dire « Mature Oak Tree confirmé par les données du jeu » et « Action en cours : Forestry niv. …, … % ». À l'arrêt : « Fin d'action ».
4. Si possible : refaire 2 de **nuit**.
5. Si possible : une action d'un **autre métier** (miner, cueillir), avec une capture **écran complet** de la jauge, pour apprendre son icône.
6. Arrêter et dire « journal v0.3.1 prêt ».

## Résultat (OK / KO, métriques)
**Essais de Claude (2026-11-16, 18:55 à 19:07, de jour, jeu ouvert par Julien, lecture seule)** :
| Mesure | Résultat |
|---|---|
| Tests automatiques | 123 OK |
| Contrôle croisé des modèles (chaque image lue avec des modèles appris sans elle) | 26/26 justes |
| Lecture en direct (60 images en 15 s) | **60/60**, N 8943, E 6703 (vérifié à l'œil), **1,2 ms** par lecture |
| Téléchargement des données | 3 s la 1re fois (7 Mo), 40 ms ensuite (déjà à jour) |
| Lancement du .exe | OK : données téléchargées et vérifiées en 1 s |
| Lecture **pendant une session** (bouton Démarrer) | **Pas fait** par Claude (il faudrait cliquer dans la fenêtre du bot) : à faire par Julien |

## Observations et images (liens images/)
- Exemples de chiffres : `images/reference-ui/coordonnees/` (26 découpes, nom = valeur attendue).

## Suites
- Si un échec apparaît : récupérer l'image et l'ajouter aux exemples, puis relancer `tools/ModelesChiffres`.

**Ajout v0.3.1 (jauge de métier, essais de Claude de 19:13 à 19:20)** : 132 tests automatiques OK ; 4 exemples (Forestry 23 à 36 %, Forestry 25 à 95 % ×2, jauge absente) ; en direct « Forestry niv. 26, 24 % » vérifié à l'œil ; jauge absente quand Julien ne coupe pas. Position : une fois 10 lectures ratées sur 60 (cause inconnue, pas reproduite : 60/60 juste après).
