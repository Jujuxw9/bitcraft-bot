# Architecture logicielle

> Document de référence pour Julien (D-023) : il décrit **comment le bot est organisé et pourquoi**, sans entrer dans le code.
> État : **v0.3.17** (2026-11-19). Le bot lit le **serveur** en lecture seule (voie hybride, D-039) et l'écran en contrôle (D-044). Seule action sur le jeu : Maj + E pour le repas automatique (D-042). Onglets : D-043. Depuis la v0.3.5, tout est décrit dans [hybride/architecture-serveur.md](hybride/architecture-serveur.md) (sections 17 et suivantes). Les sections ci-dessous racontent la v0.1 à la v0.3.4 (lecture de l'écran) :
> v0.2 : section 8. v0.2.1 (onglets, CPU/RAM) : section 9. Lecture de la position : section 10. v0.2.2 (banc d'essai des mouvements) : section 11. v0.3.0 (coordonnées par modèles, données du jeu) : section 12. v0.3.1 (jauge de métier) : section 13. v0.3.2 (capture Num 0, diagnostic) : section 14. v0.3.3 (jauge fiable) : section 15. v0.3.4 (montée de niveau) : section 16.

## 1. Le principe : une boucle « voir → décider → agir »

```
          ┌──────────────────────── jeu BitCraft (écran principal, 1920×1080) ◄──────────────┐
          │                                                                                  │
          ▼                                                                                  │
   ┌─────────────┐      ┌──────────────┐      ┌──────────────┐      ┌──────────────┐        │
   │   CAPTURE   │ ───► │  PERCEPTION  │ ───► │   DÉCISION   │ ───► │    ACTION    │ ───────┘
   │ copie de    │      │ lire le HUD, │      │ machine à    │      │ souris,      │
   │ l'écran     │      │ trouver un   │      │ états : que  │      │ clavier      │
   │             │      │ arbre…       │      │ faire ?      │      │              │
   └─────────────┘      └──────────────┘      └──────────────┘      └──────────────┘
     v0.1 ✔               v0.2 – v0.3            v0.4 et +             v0.4 et +

   ┌──────────────────────────────────────────────────────────────────────────────────┐
   │ SÉCURITÉ (au-dessus de tout) : touche Num 5, durée max, souris bougée, anomalie    │  v0.1 ✔ (2 sur 4)
   └──────────────────────────────────────────────────────────────────────────────────┘
   ┌──────────────────────────────────────────────────────────────────────────────────┐
   │ SUPERVISION (2e écran) : image du jeu, état, journal, réglages                    │  v0.1 ✔
   └──────────────────────────────────────────────────────────────────────────────────┘
```

La sécurité peut couper la boucle à tout moment, quelle que soit l'étape en cours.

## 2. Les briques (projets .NET)

| Brique | Rôle | Contenu en v0.1 |
|---|---|---|
| **App** | La fenêtre que voit Julien (WPF) | 5 onglets (D-043) : Bot (tableau de bord, image du jeu, repas automatique, sécurité), Personnage, Carte, Recettes, Paramètres (avec le journal) |
| **Core** | Le « cerveau » : règles, sécurité, journal | Surveillance de sécurité (touche d'urgence, durée max), journal de session, horloge |
| **Perception** | Les « yeux » | Recherche de la fenêtre du jeu, capture de l'écran |
| **Input** | Les « mains » (et l'écoute du clavier) | Écoute de Num 5 et Num 0 ; **contrôleur d'actions commun** (D-045) : toute touche ou tout clic passe par lui, avec les mêmes sécurités ; mouvements humains de souris et de caméra (pour H3) |
| **GameData** | La « mémoire du jeu » (objets, recettes) | Vide en v0.1. Depuis la v0.3.0 : données BitCraft_GameData téléchargées et vérifiées (D-033, section 12) |
| **Server** | La lecture du serveur officiel (depuis la v0.3.5, D-039) | Lecture seule avec le jeton de Julien : voir [hybride/architecture-serveur.md](hybride/architecture-serveur.md) |

Règle de dépendance : **App** utilise toutes les briques. **Core** ne dépend d'aucune brique d'écran ou de souris, ce qui permet de tester ses règles automatiquement, sans le jeu.

## 3. Ce que fait la v0.1, étape par étape

1. **Au lancement** : la fenêtre s'ouvre (960 × 540, ou à la place et à la taille de la dernière fois). Un fichier journal est créé. L'écoute du clavier démarre.
2. **Démarrer** : toutes les 200 ms (5 images par seconde) :
   - toutes les 2 s, le bot vérifie où est la fenêtre du jeu (titre « BitCraft ») ;
   - il copie cette zone de l'écran et l'affiche dans sa fenêtre ;
   - il vérifie la durée de session.
3. **Arrêt**, de trois façons :
   - bouton **Arrêter** ;
   - touche **Num 5**, même quand le jeu est au premier plan → bandeau rouge « ARRÊT D'URGENCE » ;
   - **durée max** atteinte (15 min par défaut).
4. **Fermeture** : la position, la taille et les réglages de sécurité sont enregistrés dans `config.local.json`.

## 4. Choix techniques et raisons

| Choix | Pourquoi |
|---|---|
| Capture par copie de l'écran (méthode GDI) | Simple, sans bibliothèque externe, marche avec le jeu en fenêtré sans bordure. Ne lit **pas** la mémoire du jeu (interdit, voir règles et risques). |
| Fenêtre du jeu trouvée par son titre | Le titre réel est « BitCraft » (processus « BitCraft »), vérifié le 2026-11-16. Les navigateurs et éditeurs dont le titre contient « BitCraft » sont écartés. |
| Touche d'urgence reconnue par sa position physique | Marche en AZERTY comme en QWERTY, avec ou sans Verr. Num. Une touche simulée par un programme n'est jamais prise pour un arrêt d'urgence. |
| Mode « pixels physiques » (PerMonitorV2) | Si Windows agrandit l'affichage (125 %, 150 %), les positions restent justes. Indispensable quand le bot cliquera. |
| Un seul `.exe` de 62 Mo | Rien à installer (D-010). Compression activée pour rester sous la limite GitHub de 100 Mo (D-020). |
| Version avec le commit | Chaque journal indique la version exacte (ex. `0.1.0+6ceb481…`). On sait toujours quel code a tourné. |

## 5. Journaux (D-031)

```
journaux/                         (à côté du .exe, jamais envoyé sur GitHub)
└── v0.1.0/
    └── 2026-11-16/
        └── session_2026-11-16_14-30-05.log
```

- **Un fichier par lancement du programme.** Chaque Démarrer / Arrêter y est noté.
- Format : `2026-11-16 14:30:05.123 [INFO] [Sécurité] message`.
- Niveaux : `INFO`, `ALERTE`, `ERREUR`. Catégories : `Session`, `Sécurité`, `Capture`, `Config` (plus tard : `Perception`, `Action`).
- Bouton « Ouvrir le dossier » dans la fenêtre.

## 6. Tests automatiques

21 tests, lancés à chaque build et à chaque publication :
- sécurité : la durée max coupe à 15 min pile, pas avant ; la touche d'urgence coupe si elle est activée, et seulement dans ce cas ; un arrêt ne se déclenche qu'une fois ;
- touche d'urgence : seule Num 5 compte (pas le 5 du haut, pas X, pas Num 4) ;
- journal : bon dossier, bon nom de fichier, bon format de ligne ;
- fenêtre du jeu : « BitCraft » est reconnu ; la fenêtre du bot, VS Code et Chrome ne le sont pas.

## 7. Points à vérifier

- **À vérifier** : l'image capturée est-elle correcte (pas noire, pas figée) ? Test T-20261116-01.
- **À vérifier** : moteur de lecture de texte (OCR) pour la v0.2 : Windows.Media.Ocr (intégré à Windows) ou Tesseract.
- **À vérifier** : support DirectML / Windows ML en 2026 (seulement si un détecteur d'arbres devient nécessaire).

## 8. v0.2 : lecture du HUD

### Ce qui est lu, et comment

| Élément | Où | Méthode |
|---|---|---|
| Position `N 8927, E 6707` | sous la minicarte | Texte (OCR) lu **deux fois** (agrandissements ×2 et ×4). Gardé seulement si les deux lectures sont identiques. |
| Zone, région | sous la position | Texte (OCR) |
| Vie, endurance `65/100` | texte dans les barres du bas | Texte (OCR), **contrôlé par la longueur de la barre** (écart max 15 %, sinon rejeté) |
| Faim | fine ligne blanche à gauche du losange | Longueur de la ligne (estimation, pas de texte) |
| Cible : nom, catégorie, santé | panneau crème en haut au centre | Panneau repéré par sa couleur, puis texte (OCR). Le nom est corrigé avec la liste des noms connus (GameData). |

- **OCR** : moteur intégré à Windows, en anglais. Avant lecture, chaque zone est passée en noir sur blanc, agrandie avec lissage et entourée d'une marge. Réglages trouvés par essais sur une vraie capture.
- Zones mesurées au pixel près sur une capture réelle en 1920×1080 (`HudLayout`). Si la fenêtre du jeu n'a pas cette taille, le HUD n'est pas lu.

### Deux boucles indépendantes

```
Capture  : 5 images/s, cadence fixe et compensée ──► affichage + dernière image gardée
Lecture  : 2 fois/s, sur la dernière image ─────────► colonne « LECTURE DU HUD » + journal
```

- La capture garde son rythme même si la lecture prend du temps.
- Si l'interface est en retard, des images sont sautées à l'affichage (jamais d'accumulation).
- Cadence réglable dans la fenêtre (« Images/s visées », de 1 à 15). Utile plus tard pour suivre un déplacement ou la minicarte.

### Journal
- Au démarrage : réglages réellement appliqués (corrige le défaut de la v0.1).
- Seulement les **changements** : zone, région, vie qui baisse (ALERTE), endurance à 0 ou pleine, faim sous 50 % (ALERTE), cible qui apparaît, change ou disparaît, position illisible 10 fois de suite (ALERTE).
- Un **résumé toutes les 30 s** : toutes les valeurs, temps de lecture, taux de lecture de la position, images/s.
- À l'arrêt : bilan de cadence et temps de capture.

### Mesures (2026-11-16, essai réel)
Capture 18-19 ms par image, lecture complète du HUD 37-47 ms, position lue 100 % du temps sur 30 s, 5,0 images/s.

### Tests automatiques
60 tests, dont une lecture complète d'une **vraie capture du jeu** (`images/reference-ui/`) : position, zone, région, vie, endurance, faim et cible doivent toutes être justes.

## 9. v0.2.1 : onglets et surveillance CPU/RAM

- **Fenêtre** : s'ouvre agrandie sur l'écran où elle était (2e écran). Position « normale » mémorisée.
- **Onglets** :
  - **Supervision** : image du jeu, lecture du HUD, commandes, sécurité. En bas : dernier événement et compteurs d'alertes / erreurs.
  - **Journal** : tout le journal en couleur, filtrable par niveau (Erreurs, Alertes, Infos, Détails).
  - **Système** : CPU et RAM du bot (avec courbes sur 2 min), du jeu (processus « BitCraft ») et du PC. Seuils réglables.
- **Niveaux du journal** : `ERREUR`, `ALERTE`, `INFO`, `DÉTAIL` (le détail : mesure CPU/RAM chaque minute).
- **Surveillance** (`ResourceMonitor`, `ResourceGuard` dans Core), chaque seconde, en lecture seule :
  | Mesure | Seuil par défaut | Effet |
  |---|---|---|
  | RAM du bot | 600 Mo | ALERTE |
  | RAM du bot | 1 200 Mo | ERREUR + **arrêt de la session** (si « Arrêt sur anomalie » coché) |
  | CPU du bot | 25 % pendant 10 s | ALERTE |
  | RAM libre du PC | 10 % | ALERTE |
  Le journal ne note que les **changements d'état** (passage en alerte, retour à la normale).
- **Mesures réelles** (2026-11-16) : bot 2 à 5 % de CPU, 175 Mo au lancement puis environ 300 Mo en session (images de 8 Mo capturées 5 fois par seconde). Jeu : environ 2,5 Go de RAM. PC : 32 Go, environ 14 Go libres.
- **À améliorer** : réutiliser les mêmes zones mémoire pour les images capturées, pour stabiliser la RAM du bot.

## 10. Lecture de la position

Jusqu'à la v0.2.2, l'OCR de Windows lisait souvent le **2** et le **0** comme un « Q » : position lue seulement 7 à 21 % du temps (test du 2026-11-16 à 18:10). Une lecture douteuse était rejetée, jamais fausse.
**Résolu en v0.3.0** par la lecture par modèles de chiffres (section 12).

## 11. v0.2.2 : banc d'essai des mouvements (D-035)

Premier code qui **agit** sur le jeu, uniquement dans l'onglet **Tests**, **désactivé à chaque démarrage**.

### Les « mains » (brique Input)
| Élément | Rôle |
|---|---|
| `HumanMouse` | Fabrique un trajet humain : courbe (Bézier), durée selon la distance (loi de Fitts), vitesse en cloche, dépassement de la cible dans 30 % des cas puis correction, petits tremblements, pauses variables |
| `HumanCamera` | Fabrique une suite d'appuis Z / Q / S / D (rotation) et W (recentrer) de durées variables |
| `InputSender` | Envoie les mouvements à Windows (SendInput). Touches par position physique (AZERTY : Z = 0x11, Q = 0x1E, S = 0x1F, D = 0x20, W = 0x2C) |
| `MovementTester` | Joue un test (souris ou caméra) avec les sécurités |

### Sécurités, vérifiées avant **chaque** mouvement
- Le jeu doit être **au premier plan** : sinon arrêt (jamais d'action dans une autre application).
- Si le curseur n'est pas là où le bot l'a mis (écart > 25 px) : **Julien a bougé la souris**, donc arrêt.
- **Num 5** : arrêt immédiat. Les touches simulées par le bot ne peuvent pas déclencher l'arrêt d'urgence.
- Souris limitée au **centre de l'écran** (hors minicarte, panneau cible, chat, barres).
- Clics gauche (étape 2) seulement si cochés, dans un rayon d'environ 220 px autour du personnage. **Jamais de clic droit.**
- À la fin, quoi qu'il arrive, **bouton et touches relâchés**.
- Durée de 5 à 120 s (30 par défaut). Démarrage après 3 s de compte à rebours.

## 12. v0.3.0 : coordonnées par modèles de chiffres et données du jeu

### Lecture de la position par modèles (Q-016)
Le texte des coordonnées est toujours **blanc, même police, même taille**. Au lieu de l'OCR (moteur généraliste), le bot compare chaque caractère à des **modèles** :

```
 zone sous la minicarte ──► taches blanches ──► regroupées ──► chaque caractère ──► « N8943,E6703 »
 (290 × 34 px)              de la taille         en lignes       comparé aux 12        → (8943, 6703)
                            d'une lettre         (même bas)      modèles 0-9, N, E
```

1. **Taches** : pixels très blancs reliés entre eux. Le décor blanc (cercle de la minicarte, icônes) est écarté par sa taille.
2. **Lignes** : taches alignées sur le même bas ; un grand vide sépare deux textes.
3. **Empreinte** : chaque caractère est ramené à une grille de 7 × 10 cases (encre moyenne) + sa largeur.
4. **Comparaison** : modèle le plus proche ; si l'écart est trop grand, la lecture est **refusée** (jamais inventée).
5. **Secours** : si la ligne n'est pas reconnue, l'ancienne lecture OCR (double lecture) prend le relais.
6. **Garde-fou** : un saut de position impossible (ex. 2 000 unités en 0,5 s) est refusé, sauf s'il est confirmé à la lecture suivante (téléportation).

| Mesure | Résultat |
|---|---|
| Exemples d'apprentissage | 26 découpes dans `images/reference-ui/coordonnees/` (nom du fichier = valeur attendue), jour et nuit |
| Contrôle croisé | 26/26 (chaque image lue avec des modèles appris sans elle) |
| En direct | 60/60, ~1 ms par lecture (l'OCR prenait ~10 ms) |

Les modèles sont fabriqués par l'outil `tools/ModelesChiffres` à partir des exemples. Si un patch change la police : ajouter des captures, relancer l'outil.
Le journal indique « position lue X % du temps (dont Y % par les modèles de chiffres) ».

### Données du jeu (D-033)

```
 Lancement du bot
   ├─ donnees-jeu/ présent ? → chargé et vérifié → noms des 878 ressources donnés au lecteur du HUD
   └─ en arrière-plan : dernier commit de BitCraft_GameData (GitHub)
         ├─ même commit → rien à faire
         └─ nouveau → téléchargement dans un dossier temporaire → vérification
                         ├─ OK → remplace l'ancienne version
                         └─ KO → ancienne version gardée, alerte
```

| Élément | Rôle |
|---|---|
| `GameDataStore` (brique GameData) | Lit les 8 tables JSON : ressources (nom, tier, étiquette, santé), récolte (durée et endurance par coup, outil et niveau requis), biomes, objets, cargaisons, nourriture. Contrôles de vraisemblance |
| `GameDataUpdater` (brique GameData) | Télécharge seulement si le dépôt a changé, toutes les tables du même commit, vérifie avant de remplacer |
| `GameDataLoader` (App) | Orchestration au lancement et messages du journal (catégorie « Données ») |

- Dossier `donnees-jeu/` **à côté du .exe**, ignoré par Git (pas de licence : jamais republié). ~7 Mo.
- **Contrôle croisé écran / données** : à chaque nouvelle cible, la santé max lue est comparée aux données. Désaccord = alerte.
- Sans Internet : version déjà présente, sinon liste de noms intégrée (19 noms).
- Alerte si les données ont plus de 7 jours.

### Autres changements v0.3.0
- Changement de zone noté seulement s'il est lu **2 fois de suite** (évite « S A Pe Meadows »).

## 13. v0.3.1 : jauge de métier (demande de Julien)

Pendant une action (coupe, minage…), le jeu affiche à gauche de la minicarte une **jauge verticale** : icône du métier, niveau, remplissage vers le niveau suivant.

```
  ┌─┐  y 22  = 100 %
  │ │
  │ │  traits blancs à 75 / 50 / 25 %
  │█│  ← haut du vert = remplissage
  │█│
  └─┘  y 245 = 0 %
  🪓    icône → métier (comparée aux icônes connues)
  26    niveau → mêmes modèles de chiffres que les coordonnées
```

| Étape | Comment |
|---|---|
| Jauge présente ? | Les deux bords gris clair du cadre sur 80 % de la hauteur, et l'intérieur vert, sombre ou trait blanc. Sinon : « aucune action » |
| Pourcentage | Ligne la plus haute où l'intérieur est vert |
| Niveau | Lecture par modèles de chiffres (section 12) |
| Métier | Empreinte de l'icône blanche comparée aux icônes apprises (`tools/ModelesChiffres`, dossier `images/reference-ui/jauge-metier/`). Icône inconnue = « métier ? », sans invention |

Journal : « Action en cours : Forestry niv. 26, 24 % », « Niveau gagné : Forestry passe niveau 27 », « Fin d'action » (après 2 s sans jauge). Le résumé toutes les 30 s inclut la jauge.
Lecture : ~0,1 ms. Seule l'icône **Forestry** est connue pour l'instant.

## 14. v0.3.2 : capture du jeu, région confirmée, images de diagnostic

| Ajout | Comportement |
|---|---|
| **Touche Num 0** | Capture exacte du jeu seul (1920×1080, PNG sans perte) dans `images/analyse/` (dossier trouvé en remontant depuis le .exe ; sinon `captures/` à côté du .exe). Marche même sans session démarrée. Réponse au problème des 2 écrans. Une touche simulée par un programme est ignorée |
| **Région confirmée** | Comme la zone : un changement n'est noté que s'il est lu 2 fois de suite (`ConfirmedText`, brique Core) |
| **Images de diagnostic** | Enregistrées à côté du journal (au plus 5 par session, 1 par minute) quand : l'endurance baisse sans jauge de métier visible (cas inexpliqué du 16/11 à 19:47), ou la position est illisible 10 fois de suite. Servent à comprendre, restent sur le PC |

## 15. v0.3.3 : jauge fiable de jour comme de nuit

Problème vu grâce aux images de diagnostic de la v0.3.2 : l'intérieur vide de la jauge est **semi-transparent** (sombre le jour, bleu la nuit, clair devant le ciel). L'ancienne règle « intérieur sombre » ratait la jauge.
- Nouvelle règle de présence : les **deux bords clairs** du cadre + au moins **2 des 3 traits** des quarts (y 78, 134, 190) qui traversent la barre.
- Icônes connues : **Forestry** (hache), **Foraging** (baies), **Mining** (pioche). 12 exemples dans `images/reference-ui/jauge-metier/` (jour, nuit, ciel clair, info-bulle, absente).
- Résultat sur les 57 captures de Julien : jauge lue à chaque action, aucune fausse jauge. En direct : Mining 40/40.
- **Faim** : alerte seulement après 4 lectures basses de suite (une info-bulle cachait la ligne de faim).

## 16. v0.3.4 : montée de niveau

Journal v0.3.2 du 16/11 à 20:08 : pendant le bandeau de montée de niveau (Mining 16), la faim est lue à 0 %, la vie à ~0 %, la cible « Lim MINING » et sa santé 30/30000.
- Quand la jauge montre un **niveau gagné**, la vie, la faim et la cible sont **ignorées 8 s** (Q-018).
- La cible n'est notée que si elle est lue **2 fois de suite** (un nom raté isolé est ignoré).
- Vérifié en rejouant la séquence du 16/11 : niveau noté, aucune fausse alerte.

Rappel : si une fenêtre (inventaire…) recouvre la jauge, elle n'est pas lue. C'est normal (cas de 20:02:56). Le bot fermera ses fenêtres avant d'agir (Q-015).

## 17 et suivantes : brique Server (voie hybride)

Déplacées dans [hybride/architecture-serveur.md](hybride/architecture-serveur.md) : brique Server, H1 (lecture du serveur), v0.3.5 à v0.3.10.
