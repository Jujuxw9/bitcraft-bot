# Analyse des captures du 2026-11-16 (lot 1)

80 captures de Julien, renommées et indexées dans [images/README.md](../../images/README.md) (dossier `images/analyse/`). Analyse faite par Claude Code.

## 1. Réglages du jeu (vérifiés)

| Réglage | Valeur | Conforme ? |
|---|---|---|
| Mode d'affichage | Borderless, 1920×1080 165 Hz | Oui (D-006) |
| Qualité | High, distance d'affichage 36, densité d'herbe 100 | — |
| Langue | English | Oui (D-015) |
| Clavier physique | AZERTY (Julien) ; le jeu affiche les touches AZERTY | — |
| Minicarte | « Click to move on minimap » **activé** | Utile pour Q-002 |
| Build affiché | `Early-Access-2-082daa0e` | **À vérifier** : le menu annonce « 1.0 World is now available ». Le libellé du build ne suit peut-être pas. |

## 2. Touches (résout l'incohérence de Q-011)

| Action | Touche |
|---|---|
| Se déplacer vers le curseur | Clic gauche |
| **Cibler + se déplacer + interagir** | **Clic droit** |
| Cibler + menu contextuel | Maj + clic droit |
| Cibler | A |
| Changer de cible | Tab |
| Se déplacer haut / gauche / bas / droite | **Non assignées** |
| Tourner la caméra | Z Q S D et flèches |
| Déplacer la caméra (pan) | Clic molette |
| **Recentrer la caméra** | **W** |
| **Cacher les arbres proches** | **X** (le bot ne doit jamais l'envoyer) |
| Escalade rapide | F |
| Barre d'action 1 à 0 | & é " ' ( - è _ ç à (rangée des chiffres AZERTY) |
| Inventaire / Carte / Compendium / Banque perso (Vault) | I / (non lisible) / C / V |
| Chat | Entrée |

Conclusion : ZQSD tourne la caméra, W la recentre. Ce sont deux choses différentes, il n'y a pas d'incohérence. Les touches de déplacement au clavier ne sont pas assignées.

## 3. HUD

- **En haut à droite** : horloge, minicarte ronde, coordonnées `N 8968, E 6730`, zone `SAFE MEADOWS`, région `KELDAN (R18)`, objectif suivi (`Giant Bountiful Strawberry Bush`). Texte net, majuscules, fond variable → OCR faisable.
- **L'horloge affiche l'heure du PC**, pas l'heure du jeu (`00:51AM` sur une capture prise à 00:51). Correction de D-018. L'icône soleil + « 2h » est sans doute le cycle jour/nuit (**À vérifier**).
- **En bas au centre** : barre de vie verte `100/100`, barre d'endurance jaune `86/100` (texte lisible), barre d'action 1 à `=`, deux emplacements à droite (outils ?).
- **En bas à gauche** : chat (messages de région, joueurs).

## 4. Inventaire et Banque

- Inventaire (I) : 1 emplacement en haut (outil ou sac ?) + **20 emplacements** (4 lignes × 5). Chaque pile affiche un nombre et un badge de tier. Boutons Sort / Transfer.
- « Inventaire plein » : à détecter en comptant les emplacements vides. **À vérifier** : une pile peut-elle se remplir sans prendre d'emplacement neuf (piles de bois) ?
- Banque : ville de **Snowrest**, bâtiment à dôme, étiquette « **Bank** » (nom confirmé, D-016). Interface « BANK » ouverte à côté de l'inventaire, bouton **Transfer** (dépôt rapide possible).
- Position de la Banque : environ `N 8937, E 6719`. Point de départ des captures : `N 8968, E 6730`. Écart d'environ 30 unités.

## 5. Survol (contour vert) — clé de Q-001

- Tout objet interactif survolé est entouré d'un **contour vert vif**, parfois avec son nom : plante, buisson, arbre, bâtiments, Banque.
- Le vert du contour (vert saturé, fin) se distingue du vert de l'herbe (plus sombre, texture). Détection couleur probablement simple. **À vérifier** sur des pixels mesurés.
- Le curseur Windows n'apparaît pas sur les captures (outil de capture). Le bot connaîtra toujours la position de sa souris.

Méthode possible, sans réseau de neurones :
1. Déplacer la souris sur des points candidats de l'écran.
2. Regarder si un contour vert apparaît sous le curseur.
3. Lire l'étiquette (OCR) ou l'info-bulle pour savoir si c'est un arbre.
4. Clic droit = le personnage va vers l'arbre et récolte (« Target + Move + Interact »).

Limite : balayer l'écran avec la souris est lent. Un détecteur d'arbres (YOLO) pourrait proposer les points candidats. Les deux se combinent.

## 6. Difficultés vues

- **Cycle jour/nuit** : la nuit, l'image est très sombre (bleu-vert). La détection par couleur doit le supporter, ou le bot ne joue que de jour (**À décider**).
- **Brouillard au loin**, arbres de plusieurs tailles et espèces.
- **Joueurs et PNJ** avec étiquettes de nom, chat qui défile : sources de faux positifs pour l'OCR.
- Les captures font entre 1885 et 1919 px de large (découpées à la main). Le bot, lui, capturera la fenêtre entière.

## 7. Ce qui manque (lot 2)

| # | Capture manquante | Pourquoi |
|---|---|---|
| M1 | Un arbre survolé de près, en prairie, de jour (souris dessus) | Mesurer le contour vert sur un arbre |
| M2 | Pendant la coupe d'un arbre (barre de progression, animation) | Savoir quand ça récolte |
| M3 | Après la coupe : souche, arbre disparu, bois dans l'inventaire | Fin de récolte |
| M4 | Inventaire **plein** | Condition de retour à la Banque |
| M5 | Endurance basse ou vide pendant la récolte | Pause endurance |
| M6 | Message d'erreur (trop loin, mauvais outil, inventaire plein) | Anomalies |
| M7 | **Carte du monde** ouverte (grande carte) | Q-002 |
| M8 | Infos de l'arbre au survol long ou avec Alt (« Show Worldspace Tooltips ») | Identifier l'espèce d'arbre |
| M9 | Réglages de touches : section « Window shortcuts » en entier (touche de la carte) | Ouvrir la carte |
