# D-024 — Commandes utilisées par le bot

- **Statut** : Validée ; Entrée remplacée en partie par D-054 (permise pour répondre dans le chat)
- **Date** : 2026-11-16
- **Validée par** : Julien (2026-11-16)
- **Liens** : Q-011, Q-002, D-021

## Contexte
Les réglages de touches (captures `reglages-touches`) montrent : ZQSD tourne la caméra, W la recentre, les touches de déplacement au clavier ne sont pas assignées. Le clic droit fait « Cibler + se déplacer + interagir ». Le clic sur la minicarte déplace le personnage. Clavier physique de Julien : AZERTY.

## Décision
- **Garder les réglages du jeu tels quels.** Ne pas assigner ZQSD au déplacement.
- Récolter un arbre : **clic droit** dessus. Le personnage marche jusqu'à l'arbre puis coupe.
- Trajets courts : **clic gauche** au sol.
- Trajets longs (Banque) : **clic sur la minicarte** (option activée), ou carte du monde.
- Caméra : **W** pour la recentrer avant chaque recherche, pour avoir une vue stable. **ZQSD** pour la faire tourner si besoin.
- Touches interdites au bot : **X** (cache les arbres), Entrée (chat). **Depuis D-054 : Entrée permise, seulement pour répondre dans le chat.**
- Le bot envoie les touches par leur position physique (scancode), donc indépendamment de AZERTY/QWERTY.

## Réserve de Julien (2026-11-16)
W recentre la caméra, mais le bot devra peut-être aussi la **tourner** avec ZQSD (par exemple pour voir un arbre caché ou chercher la Banque). Autorisé : ZQSD pour la caméra uniquement.

## Alternatives écartées
- ZQSD pour se déplacer : il faudrait réassigner la caméra, et le bot devrait gérer l'orientation. Plus complexe que le clic.

## Conséquences
- Presque tout passe par la souris : le bot n'a besoin que d'une position à l'écran.
- Si Julien change les réglages de touches, le bot doit être reconfiguré.
