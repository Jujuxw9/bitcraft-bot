# Q-026 — Changer 4 réglages du jeu pour fiabiliser la visée ?

- **Statut** : Résolue → D-056 (2026-11-23) : collision et coffres à 0, herbe à 0, caméra adoucie passée à non par Julien (à constater au prochain journal)
- **Priorité** : Haute
- **Créée le** : 2026-11-22
- **Liens** : D-052, D-050, D-006 ; réglages lus dans `technique/reglages-du-jeu.md`

## Question
Le bot a lu les réglages du jeu (registre de Windows, lecture seule). Quatre réglages gênent la visée. Julien veut-il les changer **dans le menu du jeu** (le bot ne touche jamais aux réglages) ?

| Réglage (nom interne) | Aujourd'hui | Conseillé | Pourquoi |
|---|---|---|---|
| Collision de la caméra (`CameraObjectCollision`) | 1 (oui) | 0 (non) | Avec la collision, la caméra avance quand un arbre est entre elle et le personnage : la vue change toute seule. Cause probable de la vue très rapprochée (À vérifier). |
| Caméra adoucie (`CameraPanSmoothing`) | 1 | 0 | W ramènerait sans doute la caméra d'un coup, au lieu de 1 s en douceur (D-053). |
| Ouvrir les coffres proches (`AutoOpenStoragesInRange`) | 1 | 0 | Des fenêtres s'ouvrent seules près d'un coffre et cachent la vue. À rallumer pour la Banque si besoin. |
| Herbe (`GrassDensity`) | 100 | 0 (ou bas) | L'herbe haute cache le pied des arbres et le contour vert du survol ; moins de travail pour la carte graphique. |

Le nom exact de ces réglages dans le menu du jeu est **À vérifier** (seuls les noms internes sont connus).

## Ce que le bot vérifie déjà (v0.4.2)
Au démarrage : langue anglaise, interface à l'échelle 1, 1920 × 1080, mini-carte orientée nord, zoom de la mini-carte (tous bons), et les 4 ci-dessus (ligne « [Réglages du jeu] » du journal).

## Relecture du 2026-11-23 (demande de Julien : « recheck les settings »)
- **Herbe** : 0 (avant : 100) : fait.
- **Collision de la caméra**, **caméra adoucie**, **ouvrir les coffres proches** : toujours à 1. Soit Julien ne les a pas changés, soit il ne les a pas trouvés dans le menu (noms du menu À vérifier).
- Aucun réglage de hauteur ou de distance de la caméra dans le registre (seulement ces deux réglages de caméra et la distance des noms, `NameTagsMaxDistance` 400).

## Relecture du 2026-11-23 au soir
Julien pense que ces réglages sont bons, mais le registre dit toujours 1 pour collision de la caméra, caméra adoucie et ouverture automatique des coffres (herbe à 0). **Incohérence à signaler** : soit le menu les nomme à l'envers (ex. « désactiver la collision »), soit le changement n'a pas été gardé. Demande : une capture Num 0 de la page des réglages de la caméra.

## Captures du menu du 2026-11-23, 22:34 à 22:35 (17 captures Num 0, `images/analyse/20261123_analyse_capture-bot_2234*` et `_2235*`)
- Onglet Général : « Camera collision detection » **non**, « Open nearby deploy storage » **non** : bons (le registre dit 0 depuis 21:46). L'incohérence venait d'un changement fait entre 20:48 et 21:46.
- Onglet Commandes (icône manette) : « **Camera pan smoothing** » **oui** = caméra adoucie à 1. Seul réglage de caméra restant. Aussi : sensibilité caméra 0,5, molette 0,5, axes non inversés.
- **Aucun réglage de hauteur ou de distance de la caméra** dans le menu (réponse au point 5 de Q-025).
- Zoom de la mini-carte : pas dans le menu ; ce sont les boutons + / − de la mini-carte. Le registre dit 250 depuis le 23/11 (500 avant).
- Touches : W = Reset Camera ; ZQSD et flèches = tourner la caméra ; bouton du milieu = Camera Pan ; X = cacher les arbres proches ; A = Target ; Tab = cible suivante ; Interact : **aucune touche** ; pas de déplacement au clavier ; Entrée = chat ; Maj + E = manger.
- Constat : la seule ligne « W : cap du cône » (22:32:17) vient d'un W **tapé dans le chat** (« chop down ») : elle ne prouve rien sur la caméra. Le bot doit ignorer W quand le chat est ouvert.

## Réponse de Julien (2026-11-23, 22:45) → D-056
- Caméra adoucie : il la passe à **non**. À constater dans le journal du prochain lancement (ligne « [Réglages du jeu] »).
- Zoom de la mini-carte à 250 : le cône se lit bien ; pas de changement demandé.
