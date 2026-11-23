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
| Zoom de la mini-carte (`MinimapRadius`) | 250 depuis le 23/11 (500 avant) | Boutons + et − de la mini-carte (pas dans le menu). Le cône se lit bien à 250 |
| Collision de la caméra (`CameraObjectCollision`) | 0 (depuis le 23/11 au soir) | Bon. Menu : Général → « Camera collision detection » |
| Caméra adoucie (`CameraPanSmoothing`) | 1 (23/11 au soir) | Julien la passe à 0 (D-056). Menu : Commandes (icône manette) → « Camera pan smoothing » |
| Ouvrir les coffres proches (`AutoOpenStoragesInRange`) | 0 (depuis le 23/11 au soir) | Bon. Menu : Général → « Open nearby deploy storage » |
| Herbe (`GrassDensity`) | 0 (le 23/11 ; 100 le 22/11) | Bon : baissée par Julien (Q-026) |
| Étiquettes au survol (`ShowLabelsOnHover`) | 1 | À garder |
| Riposter si attaqué (`RetaliateWhenAttacked`) | 1 | À revoir avec R7 (ennemis) |
| Images/s max (`MaxFPS`) / hors focus (`UnfocusedFPS`) | 165 / 60 | Sans effet sur le bot |
| Notifications de butin (`ShowLootNotifications`) | 1 | Peuvent cacher le HUD (Q-018) : à surveiller |
| Positions des fenêtres du jeu (`WindowOffset` X / Y) | ex. Banque (1054, 672) | Utile pour la détection des fenêtres ouvertes (D-052) |
| Raccourcis (`BindingOverrides_…`) | vides | Raccourcis **par défaut** du jeu : rien de modifié |

## Menu du jeu vu le 2026-11-23 (17 captures Num 0, 22:34 à 22:35)
- Pas de réglage de hauteur ni de distance de la caméra. Commandes : sensibilité caméra 0,5, molette 0,5, axes non inversés.
- Raccourcis (clavier AZERTY) : W = Reset Camera ; Z Q S D et flèches = tourner la caméra ; bouton du milieu = Camera Pan ; X = cacher les arbres proches ; A = Target ; Tab = cible suivante ; **Interact : M** (donnée par Julien le 23/11 à 23:00, case vide avant) ; pas de déplacement au clavier ; Entrée = chat (Maj + Entrée = chat agrandi) ; Maj + E = manger ; I = inventaire ; « . » = carte ; F = escalade rapide.
- Le registre dit « raccourcis par défaut » : ce sont donc les touches par défaut du jeu en AZERTY.
- 2026-11-23, 23:00 : Julien a **changé sa barre d'action** (nouvelles actions) et donné **M à Interact**. Le registre (`BindingOverrides_…`) n'est donc plus vide : à relire (liste blanche, jamais le jeton). Nouvelles actions de la barre : à lire dans le prochain journal (« Exemple brut de action_bar_state / ability_state ») ou sur une capture Num 0.
