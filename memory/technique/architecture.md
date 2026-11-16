# Architecture logicielle

> Document de référence pour Julien (D-023) : il décrit **comment le bot est organisé et pourquoi**, sans entrer dans le code.
> État : **v0.1.0** (2026-11-16). Le bot regarde le jeu mais n'agit pas encore dessus.

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

## 2. Les 5 briques (projets .NET)

| Brique | Rôle | Contenu en v0.1 |
|---|---|---|
| **App** | La fenêtre que voit Julien (WPF) | Image du jeu en direct, boutons Démarrer / Arrêter, réglages de sécurité, journal en direct, position de la fenêtre mémorisée |
| **Core** | Le « cerveau » : règles, sécurité, journal | Surveillance de sécurité (touche d'urgence, durée max), journal de session, horloge |
| **Perception** | Les « yeux » | Recherche de la fenêtre du jeu, capture de l'écran |
| **Input** | Les « mains » (et l'écoute du clavier) | Écoute de la touche Num 5 dans tout Windows |
| **GameData** | La « mémoire du jeu » (objets, recettes) | Vide pour l'instant (Q-004) |

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
