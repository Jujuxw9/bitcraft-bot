# D-063 — v0.4.13 : écrans épurés, arbre de recettes en rectangles, images toutes en local, overlay déplacé, colonies lues

- **Statut** : Validée
- **Date** : 2026-11-24
- **Validée par** : Julien (retour sur la v0.4.12 et consignes du 2026-11-24 soir)
- **Liens** : D-043 (onglets par usage), D-046, D-062

## Décision
1. **Onglet BOT épuré** : bonus actifs en pastilles (logo dessiné par famille de bonus, nom, temps restant, effets), sans tableau ; image du jeu réduite à un quart de la fenêtre, en bas à gauche ; cartes « Le bot », « Sprint et barre d'action », « Sacs au sol » et textes d'état des options retirés de l'écran (les informations restent dans le journal ; les éléments sont cachés, pas supprimés, car le code les remplit).
2. **Onglet CHAT masqué** (inutile pour Julien) : le code du chat n'est pas touché.
3. **PERSONNAGE** : la carte « En direct » est retirée ; recherche en haut, les trois listes (sac, ceinture, banques) remontent.
4. **PARAMÈTRES** : région **R18 fixe** (réglage caché) ; texte sous « Système » et « Seuils » cachés (valeurs gardées).
5. **RECETTES** : « Comment l'obtenir » = toute l'arborescence en petits rectangles (ingrédients → atelier ou ressource → objet) jusqu'aux ressources de base ; conversions d'objets (`item_conversion_recipe_desc`) lues comme recettes.
6. **Images** : toutes téléchargées **une seule fois** en local (`donnees-jeu/icones`, hors Git), à la demande ensuite ; images introuvables notées dans `introuvables.txt` ; marqueur `tout-telecharge.txt` (le supprimer pour refaire).
7. **Overlay** : pastille de trois voyants (serveur, écran, repas) en bas au centre, au-dessus de la barre d'action (≈ 76 % de la hauteur de l'écran du jeu).
8. **Colonies (claims)** : tables `claim_state` et `claim_local_state` lues en lecture seule pour préparer la ville de Julien ; pas encore affichées. **À vérifier** : format réel de `location` (journal : « Exemple brut de claim_local_state »).
9. **Voyageurs** : inchangé (Julien : « très bien »).

## Limites connues
- Les logos des bonus du jeu sont des signes d'une police du jeu, indisponibles : logos dessinés (emoji sur rond coloré).
- Position de l'overlay estimée sur une capture 1920 × 1080 : **À vérifier** en jeu.
- Arbre de recettes et onglet Voyageurs : essayés par compilation et tests, pas vus à l'écran par Claude.
