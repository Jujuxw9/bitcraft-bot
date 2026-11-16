# D-029 — Zone de coupe : loin des ennemis, près du village

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-021, Q-002, Q-014

## Décision
- Pas de zone imposée. Critère unique : **pas d'ennemis à proximité**.
- Les ennemis apparaissent en **triangles rouges** sur la minicarte.
- Préférer les arbres **les plus proches du centre et du village** (Snowrest, Banque).

## Précision de Julien (2026-11-16)
Les ennemis (loups…) n'attaquent que si l'on s'approche. Un triangle rouge **loin** n'est pas un danger. Le bot garde une distance minimale avec chaque triangle (valeur à mesurer, Q-014).

## Conséquences
- Le bot lit la minicarte : s'il voit un triangle rouge près du personnage, il ne choisit pas cet arbre et s'éloigne (détail : Q-014).
- Le personnage peut mourir (animaux, monstres). Julien a déjà été attaqué.
