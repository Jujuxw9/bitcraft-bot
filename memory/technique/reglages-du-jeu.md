# Réglages du jeu BitCraft (lus le 2026-11-22, relus le 2026-11-23)

Où : registre de Windows, `HKCU\Software\ClockworkLabs\BitCraft` (réglages Unity, un nom par réglage suivi de `_hNNNN`). Le jeu écrit aussi `Player.log` dans `%USERPROFILE%\AppData\LocalLow\ClockworkLabs\BitCraft\`.

**Attention** : le même endroit contient le **jeton de connexion** du jeu et l'**adresse e-mail** de Julien (dans le nom d'un réglage). Le bot ne lit qu'une **liste blanche** de réglages (`src/BitcraftBot.Perception/Screen/GameSettings.cs`) et n'écrit jamais rien. Ne jamais recopier ces valeurs ailleurs.

## Réglages utiles au bot

| Réglage | Valeur | Commentaire |
|---|---|---|
| Langue (`Language`) | en | Bon (D-015) |
| Échelle de l'interface (`UIScale`) | 1 | Bon (D-006) |
| Résolution | 1920 × 1080, plein écran fenêtré (`Fullscreen mode` 1), position (0, 0) | Bon (D-006) |
| Mini-carte verrouillée (`LockMinimap`) | 1 | Bon : toujours orientée nord (confirmé par Julien) |
| Zoom de la mini-carte (`MinimapRadius`) | 500 | À garder fixe (taille du cône de la caméra) |
| Collision de la caméra (`CameraObjectCollision`) | 1 (encore le 23/11) | À passer à 0 ? (Q-026) |
| Caméra adoucie (`CameraPanSmoothing`) | 1 (encore le 23/11) | À passer à 0 ? (Q-026) ; W revient en 1 s (D-053) |
| Ouvrir les coffres proches (`AutoOpenStoragesInRange`) | 1 (encore le 23/11) | À passer à 0 ? (Q-026) |
| Herbe (`GrassDensity`) | 0 (le 23/11 ; 100 le 22/11) | Bon : baissée par Julien (Q-026) |
| Étiquettes au survol (`ShowLabelsOnHover`) | 1 | À garder |
| Riposter si attaqué (`RetaliateWhenAttacked`) | 1 | À revoir avec R7 (ennemis) |
| Images/s max (`MaxFPS`) / hors focus (`UnfocusedFPS`) | 165 / 60 | Sans effet sur le bot |
| Notifications de butin (`ShowLootNotifications`) | 1 | Peuvent cacher le HUD (Q-018) : à surveiller |
| Positions des fenêtres du jeu (`WindowOffset` X / Y) | ex. Banque (1054, 672) | Utile pour la détection des fenêtres ouvertes (D-052) |
| Raccourcis (`BindingOverrides_…`) | vides | Raccourcis **par défaut** du jeu : rien de modifié |
