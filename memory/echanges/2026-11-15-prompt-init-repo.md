# Prompt Claude Code — Initialisation du repo `bitcraft-bot`

> Copie tout ce fichier dans Claude Code, lancé dans un clone local de `https://github.com/Jujuxw9/bitcraft-bot`.
> Rédigé le 2026-11-15 à l'issue de la session de cadrage n°1 (Claude, chat).

---

## 1. Ta mission

Tu initialises le repo `Jujuxw9/bitcraft-bot` (public, licence MIT, README et `.gitignore` Python créés par GitHub).
Tu crées la structure complète, le dossier `memory/` rempli avec le contenu de ce prompt, et le squelette du code.
Tu ne codes aucune logique de bot à ce stade.

Règles d'exécution :
1. Tout le contenu rédigé est en **français**.
2. Ne modifie pas `LICENSE`.
3. Remplace le `.gitignore` Python par un `.gitignore` .NET / Visual Studio (`dotnet new gitignore` si le SDK est présent, sinon le template GitHub `VisualStudio.gitignore`). Ajoute : `images/entrainement/**/*.png` **non** ignoré (les images sont versionnées), `*.onnx` ignoré au-delà de 50 Mo (note dans le README), `config.local.json` ignoré.
4. Si le SDK .NET 10 est installé (`dotnet --list-sdks`), crée la solution (§5). Sinon, crée seulement les dossiers avec un `README.md` chacun et ajoute une question ouverte « Création de la solution .NET » dans `memory/questions-ouvertes/`.
5. Un seul commit par grande étape, messages en français, format `type: description` (`docs:`, `chore:`, `feat:`).
6. À la fin, mets à jour `memory/journal/` avec ta propre session (format §4.4) et pousse sur `main`.
7. Signale explicitement toute incohérence trouvée dans ce prompt au lieu de la corriger en silence.

---

## 2. Contexte projet (à reporter dans `memory/00-contexte/projet.md`)

- **Porteur** : Julien (GitHub `Jujuxw9`), ingénieur électronique / électrotechnique / automatisme.
- **But** : apprendre à concevoir un agent autonome (vision + décision + action) en construisant un bot pour **BitCraft Online**.
- **Usage** : un `.exe` Windows lancé à côté du jeu. Sa fenêtre est sur le 2ᵉ écran. Le bot lit l'écran du jeu et pilote clavier/souris. Julien supervise sans toucher clavier ni souris et peut tout arrêter.
- **Esprit** : suite du projet « pokebonk » (bot par captures d'écran).
- **Jeu** : BitCraft Online, MMORPG sandbox de Clockwork Labs. Sortie 1.0 le 13 nov. 2026 (Early Access depuis le 21 juin 2025). Monde partagé, 12 professions, récolte, craft, construction, commerce. Serveur sous SpacetimeDB. Client Unity non publié.

---

## 3. Arborescence cible

```
bitcraft-bot/
├── README.md                      # présentation, état, démarrage rapide, liens vers memory/
├── LICENSE                        # inchangé (MIT)
├── .gitignore                     # .NET
├── CLAUDE.md                      # instructions pour tout agent IA (voir §6)
├── memory/                        # mémoire complète du projet (voir §4)
├── images/                        # captures d'écran (voir §7)
├── src/                           # code C# (voir §5)
├── tests/                         # tests automatisés C#
├── data/
│   ├── README.md                  # origine et format des données de jeu
│   ├── schemas/                   # schémas extraits de BitCraftPublic
│   └── gamedata/                  # valeurs (items, recettes, ressources) — source à déterminer (Q-004)
├── models/                        # modèles de vision (.onnx) + README
├── tools/                         # scripts utilitaires (capture, annotation, extraction)
└── .github/workflows/release.yml  # build du .exe sur tag vX.Y.Z (voir §8)
```

---

## 4. Dossier `memory/`

### 4.1 Arborescence

```
memory/
├── README.md                      # INDEX : rôle de chaque dossier, règles de mise à jour, liste des fichiers
├── 00-contexte/
│   ├── projet.md                  # §2
│   ├── materiel.md                # §4.6
│   └── regles-et-risques.md       # §4.7
├── decisions/
│   ├── README.md                  # tableau récapitulatif de toutes les décisions (ID, titre, statut, date)
│   └── D-001-....md               # une décision par fichier (§4.3)
├── questions-ouvertes/
│   ├── README.md                  # tableau récapitulatif (ID, titre, priorité, statut)
│   └── Q-001-....md               # une question par fichier
├── journal/
│   ├── README.md                  # liste chronologique des sessions
│   └── AAAA-MM-JJ-session-NN-sujet.md
├── echanges/
│   ├── resume-global.md           # synthèse courte et à jour de TOUS les échanges
│   └── AAAA-MM-JJ-sujet.md        # détail d'un échange (une conversation = un fichier)
├── technique/
│   ├── README.md                  # index
│   ├── architecture.md            # architecture logicielle cible
│   ├── analyse-bitcraftpublic.md  # §4.8
│   └── sources.md                 # liens analysés et leur utilité (§4.9)
└── tests/
    ├── README.md                  # tableau de tous les tests (ID, date, type, résultat)
    └── T-AAAAMMJJ-NN-sujet.md     # un test par fichier
```

Règle de découpage : dès qu'un fichier dépasse ~300 lignes ou mélange plusieurs sujets, le découper et mettre l'index à jour. Les fichiers `README.md` de chaque dossier servent d'index.

### 4.2 Règles à écrire dans `memory/README.md`

1. Tout agent (Claude chat, Claude Code, autre) lit `memory/README.md` puis `echanges/resume-global.md` avant toute action.
2. Toute décision validée par Julien devient un fichier `decisions/D-NNN-*.md`. Une décision remplacée passe au statut « Remplacée par D-NNN », elle n'est jamais supprimée.
3. Toute interrogation non tranchée devient `questions-ouvertes/Q-NNN-*.md`. Une fois tranchée : statut « Résolue → D-NNN ».
4. Chaque session de travail ajoute un fichier dans `journal/` et met à jour `echanges/resume-global.md`.
5. Identifiants séquentiels, jamais réutilisés. Dates au format ISO `AAAA-MM-JJ`.
6. Ne pas inventer : ce qui n'est pas vérifié est marqué « À vérifier ».

### 4.3 Gabarit décision

```markdown
# D-NNN — Titre

- **Statut** : Validée | Proposée | Remplacée par D-NNN
- **Date** : AAAA-MM-JJ
- **Validée par** : Julien
- **Liens** : Q-NNN, D-NNN

## Contexte
## Décision
## Alternatives écartées
## Conséquences
```

### 4.4 Gabarit journal

```markdown
# Session NN — AAAA-MM-JJ — Sujet

- **Agent** : Claude (chat) | Claude Code | autre
- **Durée / poste** : …

## Objectif
## Réalisé
## Décisions prises (liens D-NNN)
## Questions ouvertes créées (liens Q-NNN)
## Prochaines étapes
```

### 4.5 Gabarits question ouverte et test

```markdown
# Q-NNN — Titre
- **Statut** : Ouverte | Résolue → D-NNN
- **Priorité** : Haute | Moyenne | Basse
- **Créée le** : AAAA-MM-JJ
## Question
## Pistes / options
## Ce qui bloque
```

```markdown
# T-AAAAMMJJ-NN — Titre
- **Type** : unitaire | perception | intégration | session en jeu
- **Version testée** : vX.Y.Z (commit)
- **Durée** :
## Objectif
## Conditions (résolution, réglages jeu, zone)
## Procédure
## Résultat (OK / KO, métriques)
## Observations et images (liens images/)
## Suites
```

### 4.6 Contenu `00-contexte/materiel.md`

- CPU : AMD Ryzen 7 5800X
- RAM : 32 Go DDR4
- GPU : AMD Radeon RX 7800 XT → **pas de CUDA**. Inférence GPU via ONNX Runtime + DirectML (à vérifier : état de support de DirectML / Windows ML en 2026).
- Stockage : Windows sur SSD
- Écrans : 2 × 1920×1080. Principal 24" (jeu), secondaire 27" (fenêtre du bot).
- OS : Windows (version à préciser).

### 4.7 Contenu `00-contexte/regles-et-risques.md`

- Les Community Guidelines de BitCraft (https://bitcraftonline.com/community-guidelines) interdisent strictement toute automatisation, tout botting et toute macro multi-actions, même supervisés. Elles interdisent aussi la promotion d'outils tiers. Les outils en lecture seule de données publiques sont autorisés.
- **Risque accepté par Julien** : projet expérimental et d'apprentissage, sur le serveur officiel, avec son **compte principal**, en supervision constante, sessions courtes. Conséquence possible : sanction ou bannissement du compte.
- Le repo est public (choix de Julien).
- Aucun anti-cheat n'est mentionné sur Steam ni dans les news (vérifié le 2026-11-15). Absence de mention ≠ absence de détection côté serveur.
- Interdits non négociables du projet : pas d'exploit, pas de modification du client, pas de lecture mémoire du processus, pas d'injection, pas de connexion directe au serveur officiel par un client tiers.

### 4.8 Contenu `technique/analyse-bitcraftpublic.md`

Résultat de l'analyse du clone du 2026-11-15 (dernier commit : 2026-11-13, « release 1.0 ») :
- Contenu : `BitCraftServer/packages/game` et `global_module`. 723 fichiers Rust. Module SpacetimeDB : tables + reducers.
- Licence Apache 2.0, limitée au code. Art, musique et IP exclus. Interdits : exploiter un serveur, publier des exploits.
- **Données statiques : schémas présents, valeurs absentes.** `src/messages/static_data.rs` définit environ 115 tables `*Desc` (dont `ItemDesc`, `ResourceDesc`, `CraftingRecipeDesc`, `ExtractionRecipeDesc`, `BuildingDesc`, `ToolDesc`, `SkillDesc`, `BiomeDesc`). Les valeurs sont chargées au déploiement par des reducers `import_*` (`src/import_reducers.rs`). Elles ne sont pas dans le repo. → Q-004.
- Champs utiles au bot :
  - `ExtractionRecipeDesc` (récolte) : `resource_id`, `time_requirement`, `stamina_requirement`, `tool_requirements`, `level_requirements`, `extracted_item_stacks`, `range`.
  - `ResourceDesc` : `max_health`, `tier`, `on_destroy_yield`, `scheduled_respawn_time`, `footprint`.
  - `CraftingRecipeDesc` : `consumed_item_stacks`, `building_requirement`, `tool_requirements`, `time_requirement`.
  - `ItemDesc` : `volume` (capacité inventaire), `tier`, `tag`.
- Monde en **grille hexagonale** (`src/game/coordinates/` : `hex_coordinates`, `small_hex_tile`, `large_hex_tile`, `chunk_coordinates`, `region_coordinates`). Important pour la cartographie (Q-002).
- Le serveur valide les déplacements (`MoveValidationParamsDesc`) et a un agent de déconnexion automatique (`src/agents/auto_logout_agent.rs`). → impact sur les sessions longues à étudier.
- Phase 2 de l'open source annoncée environ 6 mois après janvier 2026 (Relay, Mob Manager, Map server). Client non publié.

### 4.9 Contenu `technique/sources.md`

| Source | Contenu | Utilité projet |
|---|---|---|
| github.com/clockworklabs/BitCraftPublic | Code serveur Rust, schémas de données | **Haute** : modèle de données, règles de récolte et de craft, coordonnées hexagonales |
| bitcraftonline.com/community-guidelines | Règles anti-bot | **Haute** : risques (§4.7) |
| bitcraftonline.com/news/bitcraft-open-sourcing-update (16 janv. 2026) | Phase 1 open source, permis et interdits | Moyenne : cadre légal du code |
| bitcraftonline.com/news/open-sourcing-bitcraft-online (28 avr. 2025) | Annonce de l'intention | Faible : historique |
| bitcraftonline.com/news/bitcraft-1-0-your-questions-answered (11 nov. 2026) | 1.0 : nouveau monde, reset de progression, refonte des professions, changements Banque et Marché | **Haute** : l'UI et les mécaniques viennent de changer ; captures de référence à refaire en 1.0 |
| bitcraftonline.com/news | Patch notes (cadence régulière) | **Haute** : chaque patch peut casser la vision ; à surveiller |
| store.steampowered.com/app/3454650 | Config requise, 12 professions, UI en français disponible | Moyenne |
| bitcraftonline.com/discover | Présentation marketing | Faible |
| bitcraftonline.com | Liens support, EULA, Discord | Faible |
| spacetimedb.com/docs | Docs SpacetimeDB 2.0, SDK TypeScript/Rust/C#/C++ | Faible pour l'option A (pas de connexion directe au serveur) ; utile pour lire le code serveur |

### 4.10 Décisions à créer (statut Validée, date 2026-11-15)

- **D-001 Orientation A** : bot sur le serveur officiel, par vision écran + clavier/souris, compte principal, risque accepté (lien §4.7). Écartées : B (serveur local), C (copilote en lecture seule).
- **D-002 MVP** : récolter un type de ressource dans une zone, aller déposer à la banque quand l'inventaire est plein, en boucle.
- **D-003 Niveaux d'autonomie** : N1 boucles de tâches pilotées par la vision (machine à états) → N2 planificateur d'objectifs (ex. « 200 planches » → récolte, transport, craft) → N3 consignes de haut niveau. Développement dans cet ordre.
- **D-004 Sécurité** : 4 mécanismes, **chacun activable/désactivable dans l'interface** : (1) touche globale d'arrêt d'urgence, (2) arrêt si la souris bouge manuellement, (3) arrêt sur anomalie (écran inconnu, perte de vie), (4) durée maximale de session. L'arrêt global coupe immédiatement toute action.
- **D-005 Fenêtre de supervision** (2ᵉ écran) : capture du jeu annotée (détections), état courant, prochaine action, journal, liste des tâches et objectifs (cases à cocher, tableaux, listes).
- **D-006 Affichage du jeu** : fenêtré sans bordure, 1920×1080, sur l'écran principal. Échelle d'interface par défaut.
- **D-007 Vision de l'interface** : reconnaissance de motifs + OCR pour l'UI (inventaire, barres, textes). La vision du monde 3D reste ouverte (Q-001).
- **D-008 Données de jeu** : base locale (items, recettes, ressources) construite à partir des schémas de BitCraftPublic.
- **D-009 Langage** : C# / .NET 10, interface WPF. Raisons : capture et entrées Windows natives, bonne interface graphique, OpenCvSharp et ONNX Runtime disponibles, .exe unique de taille raisonnable. Écartés : Python (exe lourd, UI faible), Rust (UI et vision immatures, développement lent).
- **D-010 Distribution** : un seul `.exe` autonome (self-contained, single-file), versions successives v0.1, v0.2… publiées en GitHub Releases.
- **D-011 Mémoire** : structure §4, découpage en plusieurs fichiers dès que nécessaire, compréhensible par tout agent IA.
- **D-012 Langue** : documentation et mémoire en français. Code (identifiants, noms de fichiers, messages de commit techniques) en **anglais**, commentaires du code en **français**.
- **D-013 Dossier images** : structure et nommage §7.
- **D-014 Repo public**, licence MIT.
- **D-015 Monde et langue du jeu** : monde **1.0** (pas EA2). Interface du jeu en **anglais** → OCR et noms de ressources en anglais.
- **D-016 Dépôt** : la **Banque** (le plus gros bâtiment de la carte ; nom exact en jeu à confirmer par capture).
- **D-017 Récolte générique** : le bot doit savoir identifier puis récolter n'importe quel type de ressource (arbres, bushes, ferralith veins, limestone, lavande, fleurs, champignons, branches/sticks au sol, silex…). Architecture générique dès le départ ; le premier type validé en test sert de référence (choix en Q-006).
- **D-018 Localisation par OCR du HUD** : la position est lue sur l'interface, sous la minicarte ronde en haut à gauche : coordonnées (ex. `N 8969, E 6739`), nom de zone (ex. « Safe Meadows », orthographe à confirmer), région (ex. `R18`). L'heure du jeu est affichée au-dessus de la minicarte.

### 4.11 Questions ouvertes à créer

- **Q-001 (Haute) Perception du monde 3D** : comment détecter arbres, rochers, minerais, PNJ, bâtiments ? Pistes : détecteur YOLO entraîné sur captures annotées (inférence ONNX/DirectML sur RX 7800 XT), segmentation couleur, exploitation des info-bulles et surbrillances au survol de la souris, indicateurs d'interface (prompt d'interaction). **Fondation de tout le projet** : à instruire avant tout code de récolte.
- **Q-002 (Haute) Cartographie et navigation** : la position absolue est lisible par OCR (D-018). Reste à définir : relation coordonnées HUD ↔ grille hexagonale du serveur, orientation de la caméra, déplacement vers une cible (clic au sol, touches), mémorisation des ressources et obstacles rencontrés (carte construite par le bot), points de passage enregistrés par Julien en complément.
- **Q-003 (Basse) Interface de consignes N3** : éditeur à nœuds (graphe visuel) ou texte libre interprété par un LLM. Plus tard.
- **Q-004 (Haute) Source des valeurs de données de jeu** : BitCraftPublic ne contient que les schémas. Chercher des exports communautaires fiables, sinon saisie manuelle depuis le jeu.
- **Q-006 (Haute) Premier type de ressource testé** : proposition Claude = arbres (cible grande et contrastée). À valider par Julien, avec la zone et l'outil.
- **Q-008 (Moyenne) Déconnexion automatique et cadence** : étudier `auto_logout_agent.rs` et les délais de récolte pour calibrer les sessions.

(Q-005 « langue du code » et Q-007 « monde joué » ont été tranchées pendant la session 01 : créer les fichiers avec le statut « Résolue → D-012 » et « Résolue → D-015 ».)

---

## 5. Squelette du code (`src/`, `tests/`)

Solution `BitcraftBot.sln`, cible `net10.0-windows` :

| Projet | Type | Rôle |
|---|---|---|
| `src/BitcraftBot.App` | WPF | Fenêtre de supervision, réglages, liste des tâches |
| `src/BitcraftBot.Core` | Bibliothèque | Machine à états, tâches, objectifs, journalisation, sécurité |
| `src/BitcraftBot.Perception` | Bibliothèque | Capture (Windows.Graphics.Capture), OCR, reconnaissance de motifs, détection |
| `src/BitcraftBot.Input` | Bibliothèque | Clavier/souris via `SendInput`, délais humanisés |
| `src/BitcraftBot.GameData` | Bibliothèque | Chargement des données de jeu (`data/`) |
| `tests/BitcraftBot.Tests` | xUnit | Tests unitaires |

Uniquement le squelette : projets, références entre projets, une fenêtre WPF vide intitulée « BitCraft Bot — v0.0.1 ». Aucune dépendance NuGet à ce stade, sauf xUnit. Vérifier que `dotnet build` passe.

Publication cible (à documenter dans le README, pas à exécuter) :
`dotnet publish src/BitcraftBot.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`

---

## 6. `CLAUDE.md` (racine)

Contenu :
- Lire `memory/README.md`, puis `memory/echanges/resume-global.md`, puis les décisions en vigueur, avant toute action.
- Respecter les décisions validées. Toute proposition contraire passe par une nouvelle question ouverte.
- En fin de session : journal + résumé global + index à jour.
- Style attendu par Julien : français, phrases courtes, concret, sans remplissage. Poser des questions plutôt que supposer. Signaler explicitement les incohérences.
- Interdits du §4.7.

---

## 7. Dossier `images/`

```
images/
├── README.md          # règles de nommage + index
├── reference-ui/      # captures d'éléments d'interface (inventaire, barres, menus) servant de modèles
├── entrainement/      # images pour entraîner la détection (sous-dossiers par classe : arbre/, rocher/…)
├── analyse/           # captures envoyées pour discussion / description
├── tests/             # captures liées à un test T-…
├── sauvegarde/        # copies de sécurité
└── archives/          # images obsolètes (ex. UI d'avant un patch)
```

Nommage : `AAAAMMJJ_categorie_sujet_NNN.png` (ex. `20261115_reference-ui_inventaire-plein_001.png`). Tout en minuscules, tirets dans le sujet. Chaque dossier contient un `.gitkeep` tant qu'il est vide. Le `README.md` décrit chaque image ajoutée (une ligne : fichier, date, contenu, version du jeu).

---

## 8. `.github/workflows/release.yml`

Déclenché sur tag `v*.*.*`. Runner `windows-latest`. Étapes : checkout, setup-dotnet 10.x, `dotnet test`, `dotnet publish` (§5), upload du `.exe` dans la GitHub Release du tag. Si l'installation .NET 10 échoue sur le runner, le signaler dans le journal.

---

## 9. Fichiers d'échanges à créer

- `memory/echanges/2026-11-15-cadrage-initial.md` : création du repo, analyse des règles, choix de l'orientation A, 15 questions de cadrage et réponses de Julien (§4.10, §4.11), analyse des liens (§4.9).
- `memory/echanges/resume-global.md` : 10 lignes maximum, état actuel du projet et prochaine étape.
- `memory/journal/2026-11-15-session-01-cadrage.md` : session de cadrage (agent : Claude chat).
- `memory/journal/2026-11-15-session-02-init-repo.md` : ta session (agent : Claude Code).

## 10. Prochaine étape (à écrire dans le résumé global)

1. Julien envoie des captures du jeu (HUD, inventaire, banque, carte, ressource ciblée, info-bulles) → rangées dans `images/analyse/`.
2. Instruction de Q-001 (perception 3D) et Q-002 (localisation) à partir de ces captures.
3. Ensuite seulement : v0.1 = capture de la fenêtre du jeu + affichage dans la fenêtre de supervision + arrêt d'urgence.
