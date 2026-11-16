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

---

# Lot 2 (16 captures, récoltes en cours)

Fichiers `recolte-*`, `jour-riviere`, `jour-survol-objet` dans `images/analyse/`.

## 8. Pendant une récolte, l'écran dit tout

Deux panneaux apparaissent dès que la récolte commence :

**Panneau cible (en haut au centre)** :
- Nom de la ressource : `Mature Oak Tree`, catégorie `Tier 1 - Tree`.
- Barre de santé de la ressource : `6746/12000` (texte lisible).
- Conditions : `✓ Axe Tier 1`, `✓ Lv. 1 Forestry` (coche verte si remplie).

**Panneau d'action (au-dessus de la barre d'action)** :
- Nom + action : `Mature Oak Tree` / `Chop`.
- Même barre de santé, une valeur `10` (puissance ?), une durée `1.59s` (temps d'un coup ?).

Conséquence pour le bot : **l'OCR du panneau cible suffit à confirmer « je coupe bien un arbre »**. Si le panneau disparaît, la récolte est finie ou interrompue.

## 9. Autres signaux visibles
- **Gain d'objet** : bandeau à gauche de l'écran (`Knapped Flint 6`). Sert à compter ce qui est récolté.
- **Gain d'expérience** : texte flottant `+12 EXP` au-dessus du personnage.
- **Jauge verticale** à gauche de la minicarte : icône du métier en cours (pioche, fleur) + niveau (`13`). Probablement l'expérience du métier. **À vérifier**.
- **Montée de niveau** : grand bandeau `MINING LEVEL 13` en haut.
- **Endurance** : baisse pendant la récolte (100 → 82 sur les captures).
- **Souche** : un arbre coupé laisse une souche.

## 10. Inventaire plein : signal trouvé
Message au centre de l'écran : **« Some items couldn't fit in your inventory and were dropped on the ground »**.
- Attention : la récolte **continue** et les objets **tombent au sol**. Ce message doit déclencher immédiatement l'arrêt de la récolte puis le départ à la Banque.
- Le bot surveillera aussi les cases vides de l'inventaire pour partir **avant** ce message.

## 11. Durée de coupe d'un arbre (À vérifier)
Sur deux captures à 10 s d'écart, la santé de l'arbre passe de 6746 à 6716. Soit environ 3 points/s pour 12 000 points. Si c'est exact, un arbre ne tombe jamais pendant une session de 15 min : l'inventaire sera plein bien avant. Le bot restera donc longtemps sur le même arbre, ce qui simplifie le MVP.

## 12. État des captures demandées
| # | État |
|---|---|
| M1 arbre survolé de près | ✔ (`recolte-arbre`) |
| M2 coupe en cours | ✔ |
| M3 après la coupe | Partiel : souche visible, pas de capture du bois dans l'inventaire |
| M4 inventaire plein | Partiel : message ✔, inventaire plein ouvert ✘ |
| M5 endurance basse | ✘ (minimum vu : 82/100) |
| M6 message d'erreur | Partiel : message d'inventaire plein ✔ |
| M7 carte du monde | ✘ |
| M8 survol avec Alt | ✘ |
| M9 raccourcis de fenêtres | ✘ |
