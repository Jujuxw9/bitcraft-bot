# Site des recettes : méthode proposée (2026-11-26, 00:35, session 29)

Demande de Julien : un site des recettes « ultra fluide, ultra simple à utiliser », avec toutes les recettes, qui répond bien à quelqu'un qui cherche comment fabriquer un objet. Julien donnera les infos qui manquent. Cadre déjà validé : D-064 (Unraid, même dépôt, dossier à part, session à part).

## Méthode proposée par Claude (pas encore validée)
1. **Cadrage d'abord, sans code** : à qui sert le site, langue, téléphone ou PC, et 3 à 5 cas concrets (« je veux faire X : quoi récolter, où, combien »). Julien montre aussi des sites qu'il aime ou n'aime pas.
2. **Données** : reprendre celles que le bot a déjà (recettes lues sur le serveur du jeu, 2 339 images en local). Un script les exporte en un fichier de données pour le site, relancé à chaque mise à jour du jeu. Détail de l'extraction : « À vérifier ».
3. **Site statique** : toutes les données chargées dans le navigateur, recherche instantanée, pas de serveur à faire tourner à part un petit conteneur web sur Unraid. C'est ce qui donne la fluidité.
4. **Maquette d'abord** : une maquette cliquable avec de vraies données sur ~20 objets. Julien valide le parcours, puis on étend à tout le jeu.
5. **Fonctions pressenties** (à confirmer au cadrage) : recherche tolérante aux fautes (français et anglais), page objet avec l'arbre de fabrication, total des matières premières pour une quantité choisie, où les trouver, métier et palier requis, « utilisé dans » (recettes qui en ont besoin), soustraction de ce qu'on a déjà.
6. **Quand** : une session à part (D-064), lancée seulement avec l'accord de Julien, après les tâches planifiées du 26/11. Claude prépare le prompt une fois le cadrage fait.

## Questions pour Julien (une à la fois)
1. Le site est pour qui : toi seul, tes amis ou ta guilde, ou tous les joueurs (public sur Internet) ? **Posée le 26/11 à 00:35.**
2. Plus tard : langue(s), téléphone ou PC, accès depuis l'extérieur de chez toi, les 3 à 5 cas concrets, sites de référence.

## Autres réponses de Julien (26/11, 00:34)
- Bilan des questions : il attend les tâches planifiées.
- Essais : il ne testera peut-être pas la v0.4.15 ; il testera directement la dernière version sortie (v0.4.18 si tout va bien) et dira laquelle.
- T-04 : à régler en Opus 5.5. Le modèle d'une tâche planifiée ne se change pas par les outils de Claude : **Julien le règle dans l'app** (Tâches planifiées → T-04 → modèle).
- Points « plus tard » (caméra, exploration automatique) : à attaquer le 27/11 ou après.
