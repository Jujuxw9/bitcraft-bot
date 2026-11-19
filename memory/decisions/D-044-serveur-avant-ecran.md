# D-044 — Le serveur d'abord, l'écran en contrôle

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Claude, sur la question de Julien (« est-ce toujours utile de lire la vie et l'endurance à l'écran ? »). À confirmer par Julien.
- **Liens** : D-039 (voie hybride), D-042

## Décision
- **Vie, endurance, faim, position, bonus** : le bot prend les valeurs du **serveur** (exactes, sans erreur de lecture). L'onglet Bot les affiche avec « (serveur) ».
- L'écran reste utilisé pour :
  - la **cible** (ce que le joueur a sélectionné : le serveur ne le dit pas) ;
  - la **jauge de métier** (action en cours, progression) et la montée de niveau ;
  - le **contrôle** : comparer la position écran et serveur, détecter un menu ou une fenêtre qui cache le jeu.
- Sans connexion au serveur, l'écran sert de secours (affiché « (écran) »).
- L'alerte « faim sous 50 % » lue à l'écran est supprimée (fausse alerte sur l'écran titre ; le repas automatique s'en charge).

## Conséquences
- La lecture de l'écran coûte peu (≈ 9 ms par image) : on la garde entière pour le moment, sans l'utiliser pour les décisions sur la vie et l'endurance.
- Plus tard, possible : ne lire que la cible et la jauge, pour économiser du CPU.
