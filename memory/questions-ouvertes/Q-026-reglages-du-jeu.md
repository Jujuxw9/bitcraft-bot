# Q-026 — Changer 4 réglages du jeu pour fiabiliser la visée ?

- **Statut** : En partie traitée (herbe à 0 le 2026-11-23) ; restent 3 réglages
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
