# Session 13 — 2026-11-19 — v0.3.18 : contrôleur d'actions, thème BitCraft

**Agent** : Claude Code

## Réalisé
- Essai v0.3.17 analysé : formats réels des 5 nouvelles tables **vérifiés** ; % du métier calculé sur le serveur = jauge du jeu (Forestry 29 à 87,5 % / 87 % ; Mining 22 à 24,9 % / 26 %).
- Contrôleur d'actions commun (D-045), 10 tests. Repas auto branché dessus.
- Num 5 toujours actif (case supprimée). Sécurité déplacée dans Paramètres, avec « Arrêt si la souris bouge » (actif).
- Repas auto actif à chaque démarrage, alerte sans nourriture (D-042 modifiée).
- Thème BitCraft, textes allégés, mise en page adaptative (D-046).
- Déconnexion garantie à la fermeture de la fenêtre (et à l'arrêt de Windows).
- Raccourci « BitCraft Bot » sur le bureau (dernière version de `artifacts/release`).
- v0.3.18 fabriquée, 264 tests OK.

## Écarts notés
- « Supprime le bouton d'arrêt d'urgence Num 5 » : interprété comme **supprimer la case**, la touche reste toujours active (sécurité principale). À confirmer par Julien.
- « Activé de base » : le repas auto est remis à « actif » **à chaque démarrage**, même s'il avait été coupé la fois d'avant.
- Pas d'essai réel de la fenêtre : Julien jouait (BitCraft au premier plan). Vérifié par rendu hors écran seulement.
- La grille de Paramètres donne la même hauteur à toutes les cartes (un peu de vide sous Sécurité et Affichage).

## Décisions prises
- D-045 (contrôleur d'actions), D-046 (interface BitCraft).

## Prochaines étapes
- Essai de Julien : `echanges/2026-11-19-essai-v0318.md`.
- H3 (aller au meilleur arbre) avec le contrôleur.
