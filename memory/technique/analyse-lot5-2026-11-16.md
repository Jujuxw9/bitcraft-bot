# Analyse du lot 5, des journaux v0.2.2 et du patch 1.0 (2026-11-16, session 08)

## 1. Journaux du test v0.2.2 (session de 18:07 à 18:14)

| Constat | Détail | Suite |
|---|---|---|
| **Position lue seulement 7 à 21 % du temps** | L'OCR lit « N 894 Q » pour « N 8940 » : le 0 et le 2 deviennent « Q ». La lecture était rejetée (pas fausse), mais trop souvent | **Corrigé en v0.3.0** : modèles de chiffres (section 5) |
| Zone lue « S A Pe Meadows » une fois | Erreur d'OCR isolée, notée comme changement de zone | **Corrigé** : changement noté seulement s'il est lu 2 fois de suite |
| Test caméra complet | 26 mouvements en 30 s, aucune touche restée enfoncée | OK |
| Test souris avec clics | 24 gestes, 9 clics gauche (petits déplacements), durée écoulée | OK |
| Arrêt « souris bougée à la main » | 3 fois. Une fois **après 0,14 s et 0 geste** (18:11:19) | **À vérifier avec Julien** : avait-il touché la souris ? Sinon, fausse détection au départ (la souris partait du 2e écran, x = -1791) |
| Arrêt par Num 5 | Absent du journal | Pas testé (ou pas noté) |
| Ressenti de Julien sur l'allure | Pas encore donné | **Question à Julien** |
| Cible lue | « Mature Oak Tree (Tier 1 - Tree) 8305/12000 », santé qui baisse | OK |
| Ressources | Bot : 0,1 à 0,8 % CPU, 270 à 430 Mo de RAM | OK |

## 2. Patch 1.0 « A Fresh World » (Steam, 2026-11-13)

Lu via l'API publique des actualités Steam (la page web ne donne pas le texte sans navigateur).
Le patch est **antérieur** à toutes nos captures (15 et 16 novembre) : ce que nous avons vu l'inclut déjà.

| Changement | Effet pour le bot |
|---|---|
| Nouveau monde « Fresh World » par défaut, « Legacy » pour l'ancien | Rien (Julien joue le monde 1.0, D-015) |
| **Maker's Tree** répartis dans le monde (ressource d'empire) | **Ne jamais les couper.** Les données du jeu les classent « Wonder Resource », pas « Tree » : le bot ne visera que les ressources d'étiquette `Tree` (D-034) |
| Popups d'expérience plus lumineux, avec « EXP » | Visibles au-dessus du personnage (« 14 EXP »). Hors des zones lues. À surveiller pour Q-018 |
| 9 langues, meilleure traduction | Le jeu doit rester **en anglais** (D-015) : la lecture dépend des textes anglais |
| Banques régionales : profondeur des objets alignée sur les coffres de colonie | Banque de Snowrest : fonctionne (captures du 16, après le patch) |
| Correctif : résolution enregistrée invalide après changement d'écran | Bon pour nous (1920×1080 fixe, D-006) |
| Correctif : « rubberbanding » le long des falaises | Déplacements plus propres |
| Plus d'expérience en ramassant graines / cachettes de Forestry | Option pour plus tard |
| Souches d'arbre à hexite : un peu d'énergie d'hexite | Rien pour le MVP |

Le patch du 10 novembre (« Starfell Reawakened ») : santé des ressources multipliée (×10 à ×200), banques reliées dans une région, agriculture refaite. Déjà pris en compte (12 000 points pour un Mature Oak Tree, confirmé par les données).

### Mise à jour du jeu **aujourd'hui**, sans note de version
- Build public **26707176**, mis en ligne le 2026-11-16 à 17:51 (heure de Paris) (source : api.steamcmd.net). Aucune actualité Steam publiée.
- Ce qui a changé n'est pas connu. Le panneau d'action en bas (section 4) est peut-être lié. **À vérifier**. La jauge de métier, elle, existait déjà : elle est sur la capture de référence de 01:59 (Forestry 23).
- La lecture des coordonnées a été testée après cette mise à jour : OK.

## 3. Captures du chiffre 5 (C1 du lot 5) : OK

- `20261116_analyse_coordonnees-chiffre5_001` à `_003` : N 8945 / 8948 / 8950, E 6704 / 6705. Le 5 est net.
- **Remarque** : ces captures ne font pas 1920×1080 (1914×1077, 1919×1079, 1917×1069). Ce sont des découpes faites à la main (outil Capture d'écran). Les chiffres sont utilisables, mais **pas les positions**. Pour les prochaines : touche **Impr. écran** (écran complet).
- Une capture de **nuit** du lot 1 contient aussi un 5 (N 8952).

## 4. Vidéos

### V1 — survol des arbres, caméra basse (`video-survol-arbres-camera-basse_001`, 62 s)
- Caméra presque à l'horizontale : beaucoup d'arbres visibles, jusqu'au loin.
- Au survol : **contour vert** autour de l'arbre entier (tronc et feuillage), même loin. Le curseur prend la forme d'une **hache** au pied de l'arbre.
- **Le panneau du haut ne change pas au survol** : il garde la dernière cible (« Mature Oak Tree 7795/12000 »). Le nom de l'arbre survolé **n'est donc pas lisible sans cliquer**.

### V2 — survol d'éléments divers (`video-survol-elements-divers_001`, 50 s)
- Caméra vue de dessus. Contour vert aussi sur les **rochers**, les **plantes**, les buissons.
- Même constat : panneau du haut inchangé.
- Conséquence pour la v0.3 (repérer les vieux arbres) : le contour dit « il y a un objet récoltable ici », pas lequel. → **Q-019**.

### V3 — coupe jusqu'à l'endurance vide (`video-coupe-jusqua-endurance-vide_001`, 5 min 20)
- Début : inventaire et chariot ouverts (transferts), puis coupe d'un Mature Oak Tree de 3 655 à 2 395 points environ.
- **Deux panneaux pendant la coupe** :
  - en haut : la cible (nom, catégorie, santé, outil et niveau requis) ;
  - **en bas, au-dessus des barres** (nouveau pour nous) : icône, nom, verbe **« Chop »**, santé, **nombre de coups** (10, 11…) et **durée d'un coup : 1,59 s** (les données du jeu disent 1,6 s).
- La coupe **s'arrête à 4/100 d'endurance** (pas à 0) : le panneau du bas **disparaît**, celui du haut reste.
- Endurance : de 84 à 4 en ~215 s, soit ~0,37 par seconde. **À vérifier** : les données donnent 0,75 par coup (ce qui ferait ~0,47/s).
- Au-dessus de l'endurance : minuteurs des bonus (« 27s », « 11m »).
- Jauge verticale à gauche de la minicarte, avec une hache et un nombre (24, puis 25) : progression du métier Forestry. Confirmé par Julien ; lue par le bot depuis la v0.3.1 (section 6).

### Captures de 18:32 et 19:00
- `info-bulle-feu-proche_001` : survol de l'icône verte = « Nearby Flame : A nearby flame is keeping enemies away » (bonus du feu de camp, Q-014).
- `coupe-panneau-action-bas_001` : coupe en cours, **sans** panneau en haut (pourquoi ? **À vérifier**), panneau du bas présent, « 14 EXP » au-dessus du personnage.

## 5. Conséquences techniques

| Sujet | Décision ou suite |
|---|---|
| Coordonnées | Lecture par modèles de chiffres en v0.3.0 (Q-016) |
| Signal « coupe en cours » | Le **panneau du bas** est un meilleur signal que la santé du haut : il apparaît pendant l'action et disparaît à l'arrêt. À lire en v0.3 (suite) |
| Fin d'endurance | Le bot doit considérer « coupe arrêtée » quand le panneau du bas disparaît, pas attendre 0 |
| Choix de l'arbre | Q-019 |
| Maker's Tree | Exclu par l'étiquette des données (`Wonder Resource`) |

## 6. Jauge de métier (demande de Julien, v0.3.1)

Julien : « prendre l'info quand je réalise une action : quelle profession, quel niveau, quel pourcentage ».

| Élément | Position (1920×1080) | Lecture |
|---|---|---|
| Barre | x 1537-1548, de y 22 (100 %) à y 245 (0 %), cadre gris clair, traits blancs à 75 / 50 / 25 % (y 78, 134, 190) | Hauteur du vert (17, 182, 74) |
| Icône du métier | y 255-292 | Comparée aux icônes connues (Forestry = hache, seule connue) |
| Niveau | y 298-310 | Mêmes modèles de chiffres que les coordonnées |

- **Visible seulement pendant une action** (et peu après) : absente quand Julien ne coupe pas (vérifié en direct à 19:16).
- Essais en direct : « Forestry niv. 26, 24 % » vérifié à l'œil ; Julien est passé du niveau 25 (95 %) au 26 entre 19:13 et 19:18.
- Lecture : ~0,1 ms.
- Les autres métiers (Mining, Foraging…) : icônes inconnues pour l'instant → métier « ? », mais niveau et pourcentage lus. Une capture par métier suffit pour l'apprendre.

## 7. Réglages du jeu changés par Julien (2026-11-16, pour information)
- **Heure en 24 h** (ex. « 19:13 » en haut de la minicarte).
- **Carte sans animation d'ouverture** : elle s'ouvre instantanément (touche ou icône). Utile pour la navigation (Q-002).
