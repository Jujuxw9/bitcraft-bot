# Q-016 — Quel est le meilleur moyen de lire les coordonnées ?
- **Statut** : Résolue (modèles de chiffres, validés par T-20261116-04 : position lue 98-100 %)
- **Priorité** : Haute
- **Créée le** : 2026-11-16 (demande de Julien : « ouvrir un point de réflexion »)

## Question
La position (`N 8933, E 6692`) est la base de toute la navigation (Q-002). L'OCR de Windows la lit bien à certains endroits (100 %) et pas du tout à d'autres (le **2** lu « Q »). Comment la lire de façon **fiable, rapide, de jour comme de nuit** ?

## Pistes / options

| Méthode | Principe | Fiabilité | Vitesse | Points faibles |
|---|---|---|---|---|
| **A. OCR Windows** (v0.2) | Moteur de lecture généraliste | Moyenne : confusions 2/Q, 8/6, espaces parasites | ~10 ms | Fait pour du texte quelconque, pas pour une police fixe |
| **B. Modèles de chiffres** | Le HUD utilise toujours la même police et la même taille. On découpe le texte en caractères, puis on compare chaque caractère à 10 modèles (0 à 9) | **Très haute** (comparaison au pixel près) | < 1 ms | Il faut un modèle pour chaque chiffre de 0 à 9 (à extraire des captures) ; à refaire si un patch change la police |
| **C. Grande carte** (idée de Julien) | Carte ouverte : fond sombre, texte bien contrasté, coordonnées sous la souris | Haute | Lente : il faut ouvrir la carte | Bloque la vue du jeu ; utile **en secours** ou pour choisir une destination |
| **D. Contrôle de cohérence** | Le personnage se déplace lentement : une position qui « saute » de 2 000 unités en 0,5 s est fausse | Filtre, pas une lecture | immédiat | À combiner avec A ou B |
| **E. Données du serveur ou mémoire du jeu** | Lire la position directement | — | — | **Interdit** (règles du projet) |

## Recommandation de Claude
1. **B (modèles de chiffres) comme lecture principale** : c'est la méthode la plus fiable pour des chiffres dans une police qui ne change pas. Sans bibliothèque supplémentaire, donc sans alourdir le `.exe` (limite GitHub 100 Mo).
2. **D en garde-fou** : rejeter les sauts impossibles.
3. **A (OCR) en secours**, et pour les textes (zone, région, noms).
4. **C (grande carte)** réservée à la navigation longue (Set destination) et en secours si B et A échouent.

Même principe possible plus tard pour les autres nombres du HUD (vie, endurance, santé de la cible).

## Ce qui bloque
- Chiffres vus dans les captures : 0, 1, 2, 3, 4, 6, 7, 8, 9. **Le 5 manque** (pas vu de façon nette) → captures demandées (lot 5, C1).
- Captures de **nuit** pour vérifier que le texte reste lisible.

## Mise en œuvre (2026-11-16, session 08)
- **B** : modèles des caractères 0-9, N, E appris sur 26 découpes étiquetées (`images/reference-ui/coordonnees/`, dont 1 de nuit). Contrôle croisé 26/26, lecture en direct 60/60, ~1 ms.
- **D** : saut de position impossible refusé tant qu'une 2e lecture ne le confirme pas (téléportation).
- **A** : l'OCR ne sert plus qu'en secours.
- Le 5 manquant : fourni par le lot 5 (C1). Un seul exemple de 3 au départ ; un 2e ajouté depuis une capture du bot en direct.
