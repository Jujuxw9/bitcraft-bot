# D-080 — v0.4.24 : reprise automatique des récoltes, onglet Personnage réorganisé

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (demande du 26/11, 21:20, après l'essai de la v0.4.23)
- **Liens** : D-069 (capacités automatiques), D-079, [architecture-suite.md](../technique/architecture-suite.md) section 28

## Contexte
Essai de la v0.4.23 : carte « beaucoup mieux », ressource recherchée « très bien », ceinture OK. Julien coupe un arbre (Mature Birch Tree, 22 000 points de vie) : l'endurance s'épuise plusieurs fois, le personnage s'arrête et il doit relancer à la main. M (Interact) seul prend la ressource la plus proche (journal : une Lavender à 21:17).

## Décision
1. **Reprise des récoltes** (interrupteur dans OPTIONS, allumé par défaut) : seulement les récoltes (arbres, buissons, plantes…), jamais les fabrications. Quand une récolte s'arrête avec l'endurance sous 15 % et la ressource pas épuisée, le bot attend 95 % d'endurance (le repas auto continue), puis appuie sur **Tab** jusqu'à ce que le serveur montre la bonne cible sélectionnée (`target_state`, 8 appuis au plus), puis sur **M** (Interact). Il vérifie sur le serveur que c'est bien cette ressource qui est frappée.
2. Arrêts : ressource épuisée, Julien bouge (plus d'une case) ou fait autre chose, autre ressource frappée (alerte), M sans effet 2 fois (alerte, couvre aussi l'inventaire plein, sac et chariot), ressource pas sélectionnable avec Tab (alerte). Le bot ne bouge jamais le personnage.
3. **Onglet Personnage** : ceinture d'outils en bandeau sur toute la largeur (peu haut), sac et banques dessous ; la banque prend 3/5 de la largeur.

## À vérifier (premier essai de Julien)
- Tab sélectionne-t-il les ressources (pas seulement les ennemis) ? Le journal note chaque cible sélectionnée (« Cible sélectionnée dans le jeu »).
- M agit-il sur la cible sélectionnée, ou toujours sur la ressource la plus proche ?
- Si l'un des deux est faux, autre méthode : viser à la souris avec le contour vert (analyse du 22/11).

## Conséquences
- Tests 640/640. Essai réel sans le jeu (21:24) : abonnement à `target_state` accepté par le serveur.
