# T-07 (03:00) — v0.4.27 : carte, partie 1 : les données

Règles communes : `nuit-2026-11-27-regles.md`. Lis le bilan (T-05, T-06) avant de commencer.

## Pourquoi
Julien veut « la carte la plus complète possible dans le bot », et une récupération, un traitement et un usage des informations optimisés. Il veut dézoomer jusqu'à **200 cases** ; aujourd'hui, au plus loin, il reste des **zones noires** même là où il est passé. Les ressources croisées doivent **rester sur la carte quand il s'éloigne**. Pour l'instant **le bois d'abord**, mais tout doit être prêt pour ajouter ensuite pierre, minerai, plantes, ennemis.
**T-08 (04:00) fait l'affichage** (taille, mise en page, vitesse de dessin) : toi, les données. Ne refais pas la mise en page.

## État actuel (lire le code avant de changer)
- Zone lue en direct : 30 cases autour du personnage (`ServerReader.ResourceRadius = 90` petites cases), relue quand il s'éloigne de 15 cases (`ResubscribeDistance`). Requêtes par morceau de carte (`Queries.AreaFilters`, D-071 : le serveur refuse sinon : « Estimated cardinality »).
- Terrain : `terrain_chunk_state` (un morceau = 32 × 32 grandes cases), autour (`Queries.TerrainRadius = 34`) + morceaux découverts lus une fois (D-079, `ServerReader.MissingDiscovered`, 130 cases, 24 par abonnement). Mémoire sur disque : `TerrainStore` (`artifacts/release/carte/*.bin`, format BCT1).
- Ressources vues : `ResourceMemory` (en mémoire seulement, 40 000 au plus, oubliées si absentes de la zone lue).
- Zoom : `ResourceMap.ViewSteps` (10 à 100), proportions √3/2 (`RowFactor`).

## À faire
1. **Zoom jusqu'à 200** : crans `10, 15, 20, 30, 45, 65, 100, 140, 200`. Rayon de recherche des morceaux découverts porté à **230 cases** (`DiscoveredRadius`), toujours un peu à la fois (24 par abonnement, ou plus si le journal montre que c'est léger : mesurer la taille reçue).
2. **Zones noires** : comprendre pourquoi des zones découvertes restent noires à 100 cases (journal v0.4.24 de Julien : lignes « Mémoire de la carte », « Fond de carte dessiné »). Pistes à vérifier : morceaux découverts hors du rayon de 130 ; dessin limité à `ViewRadius / RowFactor + 4` ; morceaux non découverts jamais lus (normal : le jeu ne les montre pas non plus ; les dessiner en « inconnu » discret, pas en noir pur) ; bitmap des zones découvertes mal lue au bord des régions. Corriger ce qui est faux, et écrire la cause trouvée dans le journal.
3. **Mémoire des ressources sur le disque, 72 h** (règle de Julien, pour **toutes** les ressources, pas seulement le bois) :
   - Chaque ressource vue est gardée avec la date où elle a été **vue pour la dernière fois** (étiquette « vue le … »), sur le disque à côté du .exe (ex. dossier `carte/ressources`, format simple et compact ; ignoré par Git comme `carte/`).
   - Dans la zone lue en direct, le serveur fait foi (coupée = retirée). Hors zone : gardée, dessinée pâle, avec « vue il y a N h » au survol ou dans le tableau.
   - **Nettoyage** : au démarrage du bot, puis **toutes les heures** tant qu'il tourne : retirer ce qui n'a pas été revu depuis **72 h**. Une ligne de journal par nettoyage (nombre retiré, nombre gardé).
   - Taille maîtrisée : borne raisonnable (ex. 200 000 ressources), écriture sur disque groupée (pas à chaque message), lecture au démarrage en arrière-plan.
4. **Échanges avec le serveur allégés** (mesurer avant/après dans le journal : Ko reçus par abonnement, nombre d'abonnements par minute de marche) :
   - Aujourd'hui chaque réabonnement renvoie tout (≈ 1,4 Mo) toutes les ~15 cases de marche. Étudier ce qui est possible **sans risque** : ne pas redemander les morceaux de terrain déjà gardés et inchangés (le terrain change rarement ; un morceau gardé de moins de 24 h peut être pris dans la mémoire au lieu du serveur, sauf les 3 × 3 autour du personnage) ; regarder si d'autres parties fixes (colonies `claim_state` 640 lignes, banques) peuvent être lues une fois au lieu de chaque fois.
   - **Ne rien changer au protocole** sans essai réel concluant (abonnements multiples, désabonnement : « À vérifier » dans la mémoire). Si un essai sans le jeu montre un refus : revenir en arrière.
5. **Contrôle des données reçues** : rejeter et compter (journal) les morceaux illisibles, les ressources sans position ou hors monde, les doublons.
6. Préparer les familles : la mémoire et les filtres doivent marcher pour toutes les catégories existantes (`MapCategories`) ; les ennemis viendront plus tard (noter dans la décision où ils se brancheront).
7. Tests `V0427Tests.cs` : nettoyage 72 h (date simulée), relecture du fichier, zone lue qui retire une ressource coupée, crans de zoom, rayon des morceaux découverts.

## Fini quand
- v0.4.27 publiée, tests verts, essai sans le jeu : journal avec « Mémoire des ressources : N gardées, M retirées (plus de 72 h) », taille reçue par abonnement notée, aucun refus.
- Bilan : cause des zones noires, mesures avant/après, ce que Julien doit regarder (dézoomer à 200 près de Keldan).

## Interdit
- Lire tout le monde ou toute la région d'un coup (le serveur refuse au-delà de 20 millions de lignes estimées). Supprimer le dossier `carte/` existant (51 morceaux gardés). Refaire la mise en page de l'onglet (T-08).

## Si une tâche d'avant a raté
- Indépendant de T-05/T-06 : partir de la dernière version publiée.
