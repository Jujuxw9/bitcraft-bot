# Recettes : liste de Julien du 2026-11-19

Source : BitCraft_GameData (D-033), commit `856b4f3`, données du 2026-11-19. Tables utilisées : `item_desc`, `cargo_desc`, `resource_desc`, `extraction_recipe_desc`, `crafting_recipe_desc`, `item_list_desc`, `enemy_desc`, `building_type_desc`.
Hypothèse : tout est au **niveau 1** (« Rough », « Ferralith », « Basic »). À confirmer par Julien.

## Liste demandée

| Demande | Nom exact dans le jeu | Quantité |
|---|---|---|
| crushed shell | Crushed Rough Shells | 10 |
| braxite | Rough Braxite | 10 |
| rough animal hair | Rough Animal Hair | 6 |
| rough pebbles | Rough Pebbles | 30 |
| cloth | Rough Cloth | 50 |
| leather | Rough Leather | 50 |
| ferralith ingot | Ferralith Ingot | 20 |
| rough brick | Rough Brick | 40 |

## Chaînes (bâtiment, métier niveau 1, outil)

- **Rough Stone Chunk** (cargaison) : miner un rocher de calcaire (Limestone Rock, Boulder, Outcrop… ; Mining 1, pioche).
- **Smash Rough Stone Chunk** (Mining Station, pioche, 50 actions) → tirage : 8 / 12 / 16 cailloux (95 %), parfois du braxite (1 % : 20 cailloux + 2 braxite ; 3,5 % : 10 cailloux + 4 braxite). Moyenne par bloc : ≈ 11,9 cailloux, ≈ 0,16 braxite.
- **Braxite direct** : Exposed Rough Quarry Rock (Mining 6), très rare.
- **Rough Animal Hair** : chasser un Sagi Bird (arc) → corps (cargaison) → Hunting Station « Harvest » (75 actions) : 3 peaux brutes + 4 viandes, + 1 poil dans 25 % des cas.
- **Crushed Rough Shells** : pêcher un Briny Guppi (canne) → Fishing Station « Process » : 2 appâts, + 1 coquillage dans 10 % des cas. Autre source : T1 Lobster Output (2 coquillages sûrs), origine du homard **à vérifier**.
- **Rough Leather** : Raw Pelt → Clean (Leatherworking Station, couteau, 50) → Cleaned Pelt + Basic Tannin → Tanning Tub (passif 300 s) → Tanned Pelt → Stitch (couteau, 100) → 1 cuir.
  - Basic Tannin : 1 écorce + 2 seaux d'eau → 3 tanins (Leatherworking Station, couteau, 25). Écorce : en coupant les arbres, ou 4 par tronc fendu. Seau d'eau : seau vide rempli au puits (Well).
- **Rough Cloth** : 1 Cloth Strip + 5 Wispweave Filament (Tailoring Station, ciseaux, 100).
  - Cloth Strip : 1 Spool of Thread au métier à tisser (Loom, passif 180 s).
  - Spool of Thread : 40 Plant Fiber (ciseaux, 50). Fibre : 100 par Rough Plant Roots (Foraging Station, machette). Racines : buissons (Bush) et Basic Deep Roots.
  - Wispweave Filament : culture (Farming). Graines sauvages (Wild Fiberplant Seeds, cueillette) ou graines de Wispweave + engrais → champ (passif 300 s) → plante → « Husk » (houe) : 3 à 7 filaments (moyenne 5).
- **Ferralith Ingot** : Ferralith Ore Chunk (veine de Ferralith, Mining 1) → Mining Station : 4 morceaux → Smithing Station (marteau, 50) : 2 morceaux = 1 concentré → Smelter (passif 300 s) : concentré + 1 bûche = métal en fusion → Smithing Station « Forge » (marteau, 100) : 1 lingot (+ 5 % de restes).
- **Rough Brick** : 5 cailloux + 2 Basic Clay Lump → Potter's Mix (Masonry Station, ciseau, 50) → Unfired Brick (ciseau, 100) → Kiln + 1 bûche (passif 300 s) → 1 brique.
  - Clay Lump : creuser un Mud Mound (98 %).
- **Rough Wood Log** : fendre un Rough Wood Trunk (Forestry Station, hache, 40) → 6 bûches. Tronc : en coupant les arbres (chêne, hêtre).

## Totaux de matières brutes (estimés)

| Matière brute | Pour | Quantité estimée |
|---|---|---|
| Rough Stone Chunk | 10 braxite (+ cailloux et briques) | ≈ 63 blocs (le braxite est le facteur limitant ; ≈ 750 cailloux en bonus) |
| Sagi Bird | 6 poils, 50 peaux | ≈ 24 oiseaux (moyenne ; 72 peaux) |
| Briny Guppi | 10 coquillages | ≈ 100 poissons |
| Écorce + seaux d'eau | 50 tanins | ≈ 17 écorces + 34 seaux |
| Rough Plant Roots | 50 bobines | 20 racines (2 000 fibres) |
| Wispweave Filament | 50 tissus | 250 filaments ≈ 50 plantes |
| Ferralith Ore Chunk | 20 lingots | 10 blocs |
| Basic Clay Lump | 40 briques | 80 mottes |
| Rough Wood Log | fonderie (20) + four (40) | 60 bûches = 10 troncs |

Les tirages sont aléatoires : ce sont des moyennes. Le sens exact des « probability » des récoltes (par coup ou par récolte finie) est **à vérifier** en jeu.
