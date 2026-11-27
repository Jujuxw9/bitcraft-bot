# Demande de Julien : « Se déplacer ici » depuis la carte du bot (2026-11-26, plus tard)

**Faite dans la nuit du 27/11** : v0.4.29 à v0.4.32 (D-086, D-087, D-089), **pas encore essayée en jeu** (bilan `taches-planifiees/bilan-nuit-2026-11-27.md`). Demande notée pendant la session 29. **Prochaine étape** (Julien, 26/11 à 20:40 : « super important », dès que la carte est stable, D-079). Petit à petit, avec les essais de Julien.

## Ce que veut Julien
- **Clic droit sur la carte du bot** → petit menu → **« Se déplacer ici »**. Le bot dit au jeu d'aller à cet endroit.
- Le bot doit trouver **le bon chemin, sans escalader** pour l'instant.
- Mieux encore : clic sur une **ressource** → « Aller à cette ressource » ou « Récolter cette ressource » ; le bot y va (et la récolte).
- Pourquoi : quand Julien coupe du bois, il y a plein d'arbres autour, mais trouver le chemin est pénible.
- Méthode : commencer par les **petites distances** ; Julien note les bugs ; on avance pas à pas.

## Liens
- D-074 : « Set destination » sur la grande carte par défaut, marche visée pour les petits trajets.
- D-072 : le bot fuit toujours les ennemis.
- Relief et eau déjà lus sur le serveur (fond de carte, `terrain_chunk_state`) : base possible pour éviter l'eau et les pentes.
- Exploration automatique (`echanges/2026-11-25-demandes-julien-session-28.md`, section 5, point 8).

## Questions à poser avant de coder
1. Petites distances : marche visée (souris, caméra) ou « Set destination » sur la grande carte du jeu ?
2. « Récolter cette ressource » : récolter jusqu'à ce qu'elle soit épuisée, ou un seul coup ?

## Réponses de Julien (26/11 au soir)
- Petits trajets : **seulement « Set destination »** pour l'instant, pas de marche visée.
- « Récolter cette ressource » : **jusqu'à ce qu'elle soit épuisée**.

## Autre demande : salon « Riverwood » (pour la prochaine version)
- Salon de discussion de Julien et de trois amis. Commande du jeu : **`/c1`** (minuscule). Les autres : Local `/s`, Région `/y`, Trade `/t`, Looking For Group `/lfg`.
- Julien veut : **archiver** les messages de Riverwood et que le chatbot **réponde aux « @gpt »** écrits dans Riverwood (par `/c1`).
- Constat (code serveur) : les salons sont dans le **module global** (`chat_channel_state`, messages dans `chat_message_state` avec `target_id` = le salon), pas dans le module de région lu en direct. Aujourd'hui le bot ne lit le module global qu'au démarrage (messages privés). Suivre Riverwood en direct demande **une liaison permanente au module global**, ouverte avant le jeu : risque d'éjection à essayer avec Julien (« À vérifier »).

## Réponses de Julien sur la grande carte du jeu (26/11, 20:55, en jeu)
- Touche « . » : la grande carte s'ouvre **centrée sur le personnage**, avec le **dernier niveau de zoom** utilisé.
- Panneau « Map Options » sur le côté : Buildings, Claims, Deployables, Placeables, Travelers, Wind, Watchtowers (à cocher ou non).
- **Un clic gauche** sur un endroit de la carte : le personnage y va tout de suite. Pas de clic droit, pas de menu.
- À vérifier : fermeture de la carte (« . » ou Échap) et marche qui continue carte fermée.

## Compléments de Julien (27/11, 00:35)
- Fermer la grande carte : **Échap** (pas sûr) ou **« . »** (sûr). Le personnage **continue de marcher** carte fermée.
- **M (Interact) = un clic sur l'élément survolé** (celui entouré de vert). Si la souris n'est sur rien (herbe), **M et Tab ne font rien**.
- Journal v0.4.24 : l'arbre **reste sélectionné** quand l'endurance tombe à 0 (target_state = l'arbre, Tab inutile) ; M a marché 1 fois sur 4 (souris sur l'arbre ou pas). Défaut vu : compteur d'échecs pas remis à zéro (abandon après 1 essai à 00:29).
