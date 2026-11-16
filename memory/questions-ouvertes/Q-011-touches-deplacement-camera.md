# Q-011 — Touches de déplacement et de caméra
- **Statut** : Ouverte (réponse proposée : D-024)
- **Priorité** : Haute
- **Créée le** : 2026-11-16
## Question
Réponses de Julien (2026-11-16) :
- Déplacement actuel : **clic au sol**. Passage possible en ZQSD/WASD, que Julien juge plus simple.
- Actuellement, ZQSD/WASD **bouge la caméra**.
- La touche **W** (Julien parle en QWERTY) **recentre la caméra**.

**Incohérence** : si W fait partie des touches qui bougent la caméra, il ne peut pas aussi la recentrer. Il faut les vrais réglages.

À éclaircir :
1. Disposition physique du clavier de Julien : AZERTY ou QWERTY ? (le bot envoie des touches physiques, ça compte)
2. Liste exacte des touches : déplacement, caméra, recentrage, interaction, inventaire, carte.
3. Mode de déplacement retenu pour le bot : clic au sol ou ZQSD/WASD.
## Pistes / options
- Clic au sol : le bot clique sur un point de l'écran. Simple à envoyer, mais il faut viser un sol libre.
- ZQSD/WASD : le bot maintient une touche. Plus prévisible, mais il faut gérer l'orientation de la caméra.
## Réponse du 2026-11-16 ([analyse des captures](../technique/analyse-captures-2026-11-16.md))
- Clavier physique : **AZERTY**.
- ZQSD = rotation caméra, W = recentrer la caméra : pas d'incohérence, ce sont deux actions distinctes.
- Déplacement au clavier : non assigné. Clic gauche = aller au curseur, clic droit = cibler + aller + interagir.
- Proposition : D-024 (tout à la souris).
## Ce qui bloque
Validation de D-024 par Julien.
