# D-030 — Procédure de dépôt à la Banque et repas

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-016, D-024

## Dépôt à la Banque (méthode de Julien)
1. Survoler le bâtiment de la Banque jusqu'au **contour vert**.
2. **Clic droit** : le personnage marche jusqu'à portée. L'interface de la Banque s'ouvre **toute seule**. Pas besoin d'entrer dans le bâtiment.
3. Cliquer sur **Transfer** du côté **inventaire** (de l'inventaire vers la Banque).
4. **Aucune fenêtre de carte ne doit être ouverte** pendant le transfert, sinon tout échoue.

## Repas
- Le bot **doit manger** quand la faim baisse.
- Méthode : **un clic** sur un aliment placé dans la **barre d'action** en bas de l'écran.
- Seuil et emplacement fixés par D-032 (faim sous 50 %, case 5), **remplacée par D-042** (Maj + E, faim sous 30 % ou plus de bonus).

## Constat du 2026-11-16 (lot 4)
Manger donne le bonus « Level 1 Food Regen » : endurance +5/min, vie +3/min, environ 14 min. Visible par une icône verte avec la durée au-dessus des barres.

## Conséquences
- Le bot doit fermer la carte avant chaque transfert.
- Il faut toujours de la nourriture dans la barre d'action : le bot vérifie qu'il en reste.
