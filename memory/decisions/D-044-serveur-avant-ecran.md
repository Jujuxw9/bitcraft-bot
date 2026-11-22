# D-044 — Le serveur d'abord, l'écran en contrôle
> Note (vérification de la mémoire, 2026-11-22) : la comparaison position écran / serveur n'existe plus depuis la v0.3.20 (D-048).

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien, 2026-11-19 (« oui, mais il faut en premier récupérer toutes les infos côté serveur, et ensuite voir s'il manque des choses, et donc les prendre en analyse écran »)
- **Liens** : D-039 (voie hybride), D-042

## Décision
- **Méthode** : d'abord récupérer **tout** ce que le serveur peut donner (inventaire dans `technique/hybride/donnees-serveur.md`), ensuite seulement lire à l'écran ce qui manque.
- **Vie, endurance, faim, position, bonus** : le bot prend les valeurs du **serveur** (exactes, sans erreur de lecture). L'onglet Bot les affiche avec « (serveur) ».
- L'écran reste utilisé pour :
  - la **cible** (ce que le joueur a sélectionné : le serveur ne le dit pas) ;
  - ~~la jauge de métier~~ : lue sur le serveur depuis la v0.3.17 (`experience_state`, formule des niveaux du serveur) ;
  - le **contrôle** : comparer la position écran et serveur, détecter un menu ou une fenêtre qui cache le jeu.
- ~~Sans connexion au serveur, l'écran sert de secours~~ : v0.3.19, vie, endurance, faim et jauge de métier ne sont **plus lues à l'écran** (demande de Julien). Sans serveur, le tableau de bord affiche « — ».
- L'alerte « faim sous 50 % » lue à l'écran est supprimée (fausse alerte sur l'écran titre ; le repas automatique s'en charge).

## Conséquences
- La lecture de l'écran coûte peu (≈ 9 ms par image) : on la garde entière pour le moment, sans l'utiliser pour les décisions sur la vie et l'endurance.
- Fait en v0.3.19 puis v0.3.20 (D-048) : l'écran ne lit plus que la cible sélectionnée.
