# D-036 — Choisir un vieil arbre : viser le plus grand, cliquer, lire, annuler si besoin

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien (« très bien ce que tu as fait », avec une précision)
- **Liens** : Q-019, D-034, D-033

## Contexte
Au survol, le jeu montre un contour vert mais pas le nom de l'arbre (Q-019).

## Décision
1. Le bot vise le **plus grand contour vert** proche (un vieil arbre est bien plus grand qu'un jeune).
2. **Clic droit** dessus : le personnage y va (même de loin) et le **panneau d'information s'affiche tout de suite en haut**, avant d'arriver (précision de Julien).
3. Le bot **lit le nom** : si c'est un vieil arbre (Mature Oak / Mature Beech, étiquette `Tree` dans les données), il laisse faire.
4. Sinon : **annuler** le trajet par un **clic gauche à côté du personnage**, puis chercher un autre arbre.
5. Jamais un « Wonder Resource » (ex. Maker's Tree).

## Alternatives écartées
- Taille du contour seule : trop dépendante de la distance et de l'angle.
- Couper n'importe quel arbre : contraire à D-034.

## Conséquences
- Utilisé en v0.4 (premières actions du bot). La lecture du panneau existe déjà (v0.2).
