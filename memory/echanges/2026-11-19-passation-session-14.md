# Passation vers la session 14 (2026-11-19)

État : v0.3.18 commitée et poussée (pas de Release : Julien n'en veut pas pour le moment, seulement push + sync GitHub).
Essai réel de la v0.3.18 pas encore fait par Julien (`echanges/2026-11-19-essai-v0318.md`).

## Demandes de Julien à traiter (dans cet ordre)

### 1. Num 5 : supprimer la fonction
« Num5 supprime la fonction associée à la touche. » → retirer l'arrêt d'urgence par Num 5 (écoute de la touche, `EmergencyKey`, appels `_actions.Halt("touche Num 5")` et `StopMealsForEmergency`, rappel dans l'overlay et dans la carte Sécurité). **Écart à signaler et à faire valider** : c'était la sécurité principale (D-022, D-041, D-045). Proposer ce qui la remplace (bouton « Arrêter », arrêt si la souris bouge, durée max). Créer une décision qui remplace D-041, mettre D-022 et D-045 à jour. Garder Num 0 (capture).

### 2. Plus d'informations dans le journal
- **Passage de niveau** dans un métier : le détecter sur le serveur (`experience_state` : le niveau calculé par `SkillLevels.LevelFor` augmente) → `[Métier] Forestry passe niveau 30`. L'écran le détecte déjà (`HudChangeLogger`, « Niveau gagné »), mais le serveur doit devenir la source.
- **Début et fin de tâche** : miner, couper, cueillir, fabriquer… Sur le serveur : `player_action_state` (couche « Base », `action_type`, `recipe_id`, `target`) et `extract_outcome_state` (ressource frappée). Écrire au début : type d'action, ressource ou recette, métier, position ; à la fin : durée, nombre de coups, XP gagnée, objets gagnés (différence du sac).
- But : que Claude ait un meilleur retour pour analyser les essais. Garder le journal lisible : une ligne par événement, pas une ligne par seconde.

### 3. Supprimer la lecture à l'écran de la vie, de l'endurance, de la faim et des niveaux
Question de Julien : « est-ce que tu analyses encore la nourriture et la stamina à l'écran ? tu n'en as plus besoin ? on peut enlever ? pareil pour les niveaux ? »
Réponse : **oui, c'est encore lu** (HUD toutes les 500 ms : vie, endurance, faim, jauge de métier), mais ça ne sert plus qu'en secours sans serveur (`ShowScreenVitals`) et dans `HudChangeLogger`. Le serveur donne tout (D-044). → retirer ces lectures de `HudReader` / `HudLayout` (zones), de `HudChangeLogger` et du tableau de bord ; garder position (contrôle croisé), cible sélectionnée et menus. Moins d'OCR = moins de CPU. Mettre à jour les tests concernés et `technique/hybride/donnees-serveur.md` (section 3).

### 4. Optimiser CPU, RAM, vitesse (peu de temps, petits gains sûrs)
Pistes repérées :
- Le point 3 (moins de zones lues par OCR).
- `OnServerTick` (500 ms) refait tout à chaque passage : carte, tableau des ressources, sac, bonus, tableau de bord, même onglet caché. Ne mettre à jour que l'onglet visible, et le sac / les bonus seulement quand le serveur a changé (`TransactionCount`).
- `reader.Inventories()` appelé plusieurs fois par tick (repas, tableau de bord, onglet Personnage) : le calculer une fois.
- `GameWindowLocator().Find()` appelé à plusieurs endroits (overlay, capture Num 0, contrôleur) : un seul cache partagé.
- Capture d'écran à 5 images/s même sans afficher l'image (onglet Bot caché) : baisser la cadence quand l'image n'est pas visible.
- Journal : messages bruts du serveur (« Message n° X ») en DÉTAIL à chaque transaction : en garder un échantillon.
- Mesurer avant / après avec la carte Système (CPU et RAM du bot) et le journal (« Système »).

## Rappels
- Lire d'abord : `CLAUDE.md`, `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md`.
- Julien joue souvent : vérifier que BitCraft n'est pas au premier plan avant d'ouvrir une fenêtre du bot ; sinon vérifier l'interface par un rendu hors écran.
- Ne pas commiter `memory/technique/recettes-liste-2026-11-19.md` (une autre session le modifie). Regarder `git status` avant `git add`.
- Pas de Release ni de tag pour le moment : commit, push, sync GitHub.
