# Images — règles et index

## Dossiers

| Dossier | Contenu |
|---|---|
| `reference-ui/` | Captures d'éléments d'interface (inventaire, barres, menus) servant de modèles |
| `entrainement/` | Images pour entraîner la détection (sous-dossiers par classe : `arbre/`, `rocher/`…) |
| `analyse/` | Captures envoyées pour discussion / description |
| `tests/` | Captures liées à un test `T-…` |
| `sauvegarde/` | Copies de sécurité |
| `archives/` | Images obsolètes (ex. UI d'avant un patch) |

## Stockage (D-026)

Les captures restent **sur le PC** (ignorées par Git). Seuls ce fichier et `reference-ui/` sont poussés. Penser à copier `images/` de temps en temps sur un disque externe.

## Vidéos

Même nommage, extension `.mp4` et sujet commençant par `video-` (ex. `20261116_analyse_video-coupe-plusieurs-arbres_001.mp4`). Format conseillé : MP4, 1920×1080, 30 i/s, 1 à 3 min, un sujet par vidéo.

## Nommage

`AAAAMMJJ_categorie_sujet_NNN.png`

- Exemple : `20261115_reference-ui_inventaire-plein_001.png`
- Tout en minuscules, tirets dans le sujet.
- Chaque dossier contient un `.gitkeep` tant qu'il est vide.
- Conditions de capture par défaut : 1920×1080, fenêtré sans bordure, échelle UI par défaut, jeu en anglais, monde 1.0 (D-006, D-015).

## Index des images

Une ligne par image ajoutée.

| Fichier | Date | Contenu | Version du jeu |
|---|---|---|---|
| `analyse/20261115_analyse_nuit-hud-complet_001.png` | 2026-11-15 | Nuit, écran complet : HUD, chat, minicarte, barres vie/endurance | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-approche_001.png` | 2026-11-16 | Approche de la Banque à Snowrest | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-approche_002.png` | 2026-11-16 | Approche de la Banque à Snowrest | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-interface_001.png` | 2026-11-16 | Interface Bank ouverte à côté de l'inventaire | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-interface-info-bulle_001.png` | 2026-11-16 | Interface Bank ouverte + info-bulle d'objet | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-interface-info-bulle_002.png` | 2026-11-16 | Interface Bank ouverte + info-bulle d'objet | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-survol-entree_001.png` | 2026-11-16 | Entrée de la Banque survolée, contour vert | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-survol-exterieur_001.png` | 2026-11-16 | Banque survolée de loin, contour vert, étiquette « Bank » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-vue-exterieure_001.png` | 2026-11-16 | Banque vue de l'extérieur, arbres autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_banque-vue-exterieure_002.png` | 2026-11-16 | Banque vue de l'extérieur, arbres autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chargement_001.png` | 2026-11-16 | Écran de chargement « Initializing player » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_inventaire_001.png` | 2026-11-16 | Inventaire (I) : 1 emplacement outil + 20 emplacements, 3 occupés | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-camera-proche_001.png` | 2026-11-16 | Jour, caméra proche du personnage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_001.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_002.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_003.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_004.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_005.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_006.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_007.png` | 2026-11-16 | Jour, prairie : angles de caméra variés, arbres au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-survol-buisson_001.png` | 2026-11-16 | Jour : buisson survolé au pied des arbres, contour vert | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-survol-plante_001.png` | 2026-11-16 | Jour, prairie : plante survolée, contour vert | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-abilities_001.png` | 2026-11-16 | Menu Abilities (N) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-achievements_001.png` | 2026-11-16 | Menu Achievements (Y) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-achievements_002.png` | 2026-11-16 | Menu Achievements (Y) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-achievements_003.png` | 2026-11-16 | Menu Achievements (Y) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-achievements_004.png` | 2026-11-16 | Menu Achievements (Y) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-character-equipement_001.png` | 2026-11-16 | Menu Character, onglet Equipment (H) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-character-professions_001.png` | 2026-11-16 | Menu Character, onglet Professions | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-character-skills_001.png` | 2026-11-16 | Menu Character, onglet Skills | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-compendium_001.png` | 2026-11-16 | Compendium (C) : objets, recettes, créatures | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-compendium_002.png` | 2026-11-16 | Compendium (C) : objets, recettes, créatures | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-compendium_003.png` | 2026-11-16 | Compendium (C) : objets, recettes, créatures | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-compendium_004.png` | 2026-11-16 | Compendium (C) : objets, recettes, créatures | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-compendium_005.png` | 2026-11-16 | Compendium (C) : objets, recettes, créatures | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-principal_001.png` | 2026-11-16 | Menu principal : monde, annonces « BitCraft 1.0 World is now available » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-quest-log_001.png` | 2026-11-16 | Journal de quêtes (G) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-quest-log_002.png` | 2026-11-16 | Journal de quêtes (G) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-traveler-tasks_001.png` | 2026-11-16 | Menu Traveler Tasks (T) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_001.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_002.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_003.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_004.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_005.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_006.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_menu-vault_007.png` | 2026-11-16 | Menu Vault (V) : cosmétiques, collections | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_minicarte-zoom_001.png` | 2026-11-16 | Minicarte agrandie : heure, coordonnées, zone, région, suivi | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_minicarte-zoom_002.png` | 2026-11-16 | Minicarte agrandie : heure, coordonnées, zone, région, suivi | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_minicarte-zoom_003.png` | 2026-11-16 | Minicarte agrandie : heure, coordonnées, zone, région, suivi | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_nuit-camera-basse_001.png` | 2026-11-16 | Nuit, caméra basse derrière le personnage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_nuit-camera-basse-batiments_001.png` | 2026-11-16 | Nuit, caméra basse, bâtiments au loin | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_nuit-camera-dessus_001.png` | 2026-11-16 | Nuit, caméra vue de dessus, zoom proche | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_nuit-camera-loin_001.png` | 2026-11-16 | Nuit, caméra éloignée | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_nuit-camera-tres-proche_001.png` | 2026-11-16 | Nuit, caméra collée au personnage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-affichage_001.png` | 2026-11-16 | Réglages affichage : Borderless, 1920×1080 165 Hz, qualité High | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-carte_001.png` | 2026-11-16 | Réglages carte : « Click to move on minimap » activé | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-etiquettes_001.png` | 2026-11-16 | Réglages des étiquettes (bâtiments, noms de joueurs) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-general_001.png` | 2026-11-16 | Réglages généraux : langue English | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-souris_001.png` | 2026-11-16 | Réglages souris / caméra (sensibilités) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_001.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_002.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_003.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_004.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_005.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_006.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_007.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_reglages-touches_008.png` | 2026-11-16 | Réglages des touches (déplacement, caméra, barre d'action, raccourcis) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-arrivee_001.png` | 2026-11-16 | Arrivée dans la ville Snowrest (bandeau « Entering Snowrest ») | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-ateliers_001.png` | 2026-11-16 | Snowrest : ateliers (workbench, stations), PNJ et joueurs | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-ateliers_002.png` | 2026-11-16 | Snowrest : ateliers (workbench, stations), PNJ et joueurs | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-ateliers_003.png` | 2026-11-16 | Snowrest : ateliers (workbench, stations), PNJ et joueurs | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-ateliers_004.png` | 2026-11-16 | Snowrest : ateliers (workbench, stations), PNJ et joueurs | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-ateliers_005.png` | 2026-11-16 | Snowrest : ateliers (workbench, stations), PNJ et joueurs | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-arbre_001.png` | 2026-11-16 | Snowrest : arbre survolé, contour vert + remplissage sombre | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-batiment_001.png` | 2026-11-16 | Snowrest : bâtiments survolés, contour vert + nom | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-batiment_002.png` | 2026-11-16 | Snowrest : bâtiments survolés, contour vert + nom | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-batiment_003.png` | 2026-11-16 | Snowrest : bâtiments survolés, contour vert + nom | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-batiment_004.png` | 2026-11-16 | Snowrest : bâtiments survolés, contour vert + nom | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-batiment_005.png` | 2026-11-16 | Snowrest : bâtiments survolés, contour vert + nom | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_snowrest-survol-batiment_006.png` | 2026-11-16 | Snowrest : bâtiments survolés, contour vert + nom | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-riviere_001.png` | 2026-11-16 | Jour, bord de rivière, personnage arrêté | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-survol-objet_001.png` | 2026-11-16 | Jour, bord de rivière, petit objet survolé (contour vert) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-silex_001.png` | 2026-11-16 | Récolte de « Flint Pile » (Mining) : panneau cible en haut, barre d'action en bas, gain « Knapped Flint » à gauche, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-silex_002.png` | 2026-11-16 | Récolte de « Flint Pile » (Mining) : panneau cible en haut, barre d'action en bas, gain « Knapped Flint » à gauche, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-lavande_001.png` | 2026-11-16 | Récolte de « Lavender » (Foraging, machette) : panneaux cible et action, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-lavande_002.png` | 2026-11-16 | Récolte de « Lavender » (Foraging, machette) : panneaux cible et action, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-fleur_001.png` | 2026-11-16 | Récolte de « Daisy » (Foraging) : panneaux cible et action, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-fleur_002.png` | 2026-11-16 | Récolte de « Daisy » (Foraging) : panneaux cible et action, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-fleur_003.png` | 2026-11-16 | Récolte de « Daisy » (Foraging) : panneaux cible et action, +EXP | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre_001.png` | 2026-11-16 | **Coupe de « Mature Oak Tree »** (Tier 1, Axe Tier 1, Forestry) : contour vert, santé 6746/12000, action « Chop » 1,59 s | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre_002.png` | 2026-11-16 | **Coupe de « Mature Oak Tree »** (Tier 1, Axe Tier 1, Forestry) : contour vert, santé 6746/12000, action « Chop » 1,59 s | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre_003.png` | 2026-11-16 | **Coupe de « Mature Oak Tree »** (Tier 1, Axe Tier 1, Forestry) : contour vert, santé 6746/12000, action « Chop » 1,59 s | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-roche_001.png` | 2026-11-16 | Récolte de « Limestone Rock Formation » (pioche) à côté d'une souche d'arbre | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-roche-inventaire-plein_001.png` | 2026-11-16 | Message **« Some items couldn't fit in your inventory and were dropped on the ground »** (inventaire plein) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-roche_002.png` | 2026-11-16 | Récolte de « Limestone Rock Formation » (pioche) à côté d'une souche d'arbre | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-roche-inventaire-plein_002.png` | 2026-11-16 | Message **« Some items couldn't fit in your inventory and were dropped on the ground »** (inventaire plein) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre-sac-au-sol_001.png` | 2026-11-16 | Arbre abattu (Young Beech Tree) : **« Item Pouch »** au sol avec un **Rough Wood Trunk**, boutons Claim All / Release, minuteur, « Owned by ChatGPT » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre-sac-au-sol_002.png` | 2026-11-16 | Arbre abattu (Young Beech Tree) : **« Item Pouch »** au sol avec un **Rough Wood Trunk**, boutons Claim All / Release, minuteur, « Owned by ChatGPT » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_001.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre_004.png` | 2026-11-16 | « Mature Oak Tree » (12 000 points) : survol et coupe, contour vert | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre_005.png` | 2026-11-16 | « Mature Oak Tree » (12 000 points) : survol et coupe, contour vert | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-arbre_006.png` | 2026-11-16 | « Mature Oak Tree » (12 000 points) : survol et coupe, contour vert | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-buisson_001.png` | 2026-11-16 | Récolte de « Strawberry Bush » (Foraging) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-cereales_001.png` | 2026-11-16 | Récolte de « Wild Grains » (Foraging) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-cereales_002.png` | 2026-11-16 | Récolte de « Wild Grains » (Foraging) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-objet_001.png` | 2026-11-16 | Inventaire ouvert + info-bulle d'objet (Basic Berry, Plain Roasted Fish) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-objet_002.png` | 2026-11-16 | Inventaire ouvert + info-bulle d'objet (Basic Berry, Plain Roasted Fish) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_charge-cargo_001.png` | 2026-11-16 | Message **« Carrying Cargo : movement speed encumbered »** (tronc porté) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-radeau_001.png` | 2026-11-16 | Info-bulle « Raft » (transport de cargaison sur l'eau) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-charrette_001.png` | 2026-11-16 | Info-bulle « Cart » (transport de cargaison) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-ferralith_001.png` | 2026-11-16 | Minage de « Large Ferralith Vein » (Tier 1, 30 000 points, pioche) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-ferralith_002.png` | 2026-11-16 | Minage de « Large Ferralith Vein » (Tier 1, 30 000 points, pioche) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-ferralith_003.png` | 2026-11-16 | Minage de « Large Ferralith Vein » (Tier 1, 30 000 points, pioche) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-ferralith_004.png` | 2026-11-16 | Minage de « Large Ferralith Vein » (Tier 1, 30 000 points, pioche) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-ferralith_005.png` | 2026-11-16 | Minage de « Large Ferralith Vein » (Tier 1, 30 000 points, pioche) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-boue_001.png` | 2026-11-16 | Récolte de « Mud Mound » (argile) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-boue_002.png` | 2026-11-16 | Récolte de « Mud Mound » (argile) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-batons_001.png` | 2026-11-16 | Ramassage de « Sticks » (Forestry, sans outil) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-batons_002.png` | 2026-11-16 | Ramassage de « Sticks » (Forestry, sans outil) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_peche_001.png` | 2026-11-16 | Pêche « Briny Guppi » : poisson avec contour vert ou rouge dans l'eau | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_peche_002.png` | 2026-11-16 | Pêche « Briny Guppi » : poisson avec contour vert ou rouge dans l'eau | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_peche_003.png` | 2026-11-16 | Pêche « Briny Guppi » : poisson avec contour vert ou rouge dans l'eau | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_peche_004.png` | 2026-11-16 | Pêche « Briny Guppi » : poisson avec contour vert ou rouge dans l'eau | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_peche_005.png` | 2026-11-16 | Pêche « Briny Guppi » : poisson avec contour vert ou rouge dans l'eau | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_002.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_003.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_004.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_005.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_006.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_007.png` | 2026-11-16 | Coupe de « Young Beech Tree » / « Young Oak Tree » (Tier 1, 3 000 points), gain « Rough Tree Bark », souches autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_endurance-vide_001.png` | 2026-11-16 | **Endurance 0/100** : compétences 2 et 3 grisées en rouge | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_endurance-basse_001.png` | 2026-11-16 | Endurance 3/100, veine de ferralith survolée | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-competence_001.png` | 2026-11-16 | Info-bulles des compétences de la barre d'action (Autoattack, Strike, On Guard, Sprint) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-competence_002.png` | 2026-11-16 | Info-bulles des compétences de la barre d'action (Autoattack, Strike, On Guard, Sprint) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-competence_003.png` | 2026-11-16 | Info-bulles des compétences de la barre d'action (Autoattack, Strike, On Guard, Sprint) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-competence_004.png` | 2026-11-16 | Info-bulles des compétences de la barre d'action (Autoattack, Strike, On Guard, Sprint) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-vie_001.png` | 2026-11-16 | Info-bulle Health (100/100, régénération) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-endurance_001.png` | 2026-11-16 | Info-bulle **Stamina** : sert à récolter, crafter, courir ; régénération passive | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-teleportation_001.png` | 2026-11-16 | Info-bulle Teleportation Energy (Waystones) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-faim_001.png` | 2026-11-16 | Info-bulle **Hunger 70/100** : à 0, le personnage perd de la vie | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-plante_001.png` | 2026-11-16 | Récolte de « Wild Starbulb Plant » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-plante_002.png` | 2026-11-16 | Récolte de « Wild Starbulb Plant » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-plante_003.png` | 2026-11-16 | Récolte de « Wild Starbulb Plant » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-plante_004.png` | 2026-11-16 | Récolte de « Wild Starbulb Plant » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-plante_005.png` | 2026-11-16 | Récolte de « Wild Starbulb Plant » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-arbre-distance_001.png` | 2026-11-16 | **Arbres survolés de loin** : contour vert sur des arbres éloignés | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-arbre-distance_002.png` | 2026-11-16 | **Arbres survolés de loin** : contour vert sur des arbres éloignés | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-arbre-distance_003.png` | 2026-11-16 | **Arbres survolés de loin** : contour vert sur des arbres éloignés | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-arbre-distance_004.png` | 2026-11-16 | **Arbres survolés de loin** : contour vert sur des arbres éloignés | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-arbre-distance_005.png` | 2026-11-16 | **Arbres survolés de loin** : contour vert sur des arbres éloignés | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre-temps-restant_001.png` | 2026-11-16 | Coupe « Young Beech Tree » 905/3000 : info-bulle **« Estimated time left: 2m 24s »** au survol du panneau d'action | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre-niveau_001.png` | 2026-11-16 | Montée de niveau « Forestry Level 22 », gains « Rough Tree Bark 29 » et « Owl Feather » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_coupe-arbre-troncs-au-sol_001.png` | 2026-11-16 | Coupe de « Young Oak Tree » ; plusieurs **troncs laissés au sol** autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_coupe-arbre-troncs-au-sol_002.png` | 2026-11-16 | Coupe de « Young Oak Tree » ; plusieurs **troncs laissés au sol** autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-tronc-au-sol_001.png` | 2026-11-16 | Tronc au sol survolé : contour vert + étiquette « Rough Wood Trunk » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_coupe-arbre-troncs-au-sol_003.png` | 2026-11-16 | Coupe de « Young Oak Tree » ; plusieurs **troncs laissés au sol** autour | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-tronc-au-sol_002.png` | 2026-11-16 | Tronc au sol survolé : contour vert + étiquette « Rough Wood Trunk » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-tronc-au-sol_003.png` | 2026-11-16 | Tronc au sol survolé : contour vert + étiquette « Rough Wood Trunk » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_souche-et-troncs_001.png` | 2026-11-16 | Souche et troncs au sol après abattage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_survol-arbre-proche_001.png` | 2026-11-16 | Arbre survolé de près (contour vert), panneau « Daisy » encore affiché | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_recolte-jeune-arbre_008.png` | 2026-11-16 | Coupe de « Young Oak Tree » 3000/3000, panneau d'action | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-bonus-nourriture_001.png` | 2026-11-16 | Zoom : bonus **« Level 1 Food Regen »** (endurance +5/min, vie +3/min), durée restante 13 min 54 s, icône « 14m » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_info-bulle-charrette_002.png` | 2026-11-16 | Zoom : info-bulle **Cart** (1 conducteur, vitesse 15, 4 emplacements d'objets ×100, 2 emplacements de cargaison ×250) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-monde_001.png` | 2026-11-16 | Chariot posé dans la prairie | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-zoom_001.png` | 2026-11-16 | Zoom sur le chariot et le personnage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-zoom_002.png` | 2026-11-16 | Zoom sur le chariot et le personnage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-zoom_003.png` | 2026-11-16 | Zoom sur le chariot et le personnage | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-survol_001.png` | 2026-11-16 | Chariot survolé : contour vert et étiquette « Cart » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-monde_002.png` | 2026-11-16 | Chariot posé dans la prairie | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-survol_002.png` | 2026-11-16 | Chariot survolé : contour vert et étiquette « Cart » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-attele_001.png` | 2026-11-16 | Personnage qui **tire le chariot** | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-attele_002.png` | 2026-11-16 | Personnage qui **tire le chariot** | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-attele_003.png` | 2026-11-16 | Personnage qui **tire le chariot** | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-attele_004.png` | 2026-11-16 | Personnage qui **tire le chariot** | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-interface_001.png` | 2026-11-16 | Inventaire + fenêtre **« ChatGPT Cart (I) »** ouverts | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-interface-zoom_001.png` | 2026-11-16 | Zoom : fenêtre du chariot (4 emplacements, 2 emplacements de cargaison 0/250, Sort / Transfer) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_chariot-interface_002.png` | 2026-11-16 | Inventaire + fenêtre **« ChatGPT Cart (I) »** ouverts | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_hud-menu-bas-droite_001.png` | 2026-11-16 | Zoom : icônes du menu en bas à droite | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_hud-menu-bas-gauche_001.png` | 2026-11-16 | Zoom : icônes en bas à gauche (chat, social, construction, commerce) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_minicarte-ennemi_001.png` | 2026-11-16 | Zoom minicarte : **triangle rouge (ennemi)** près de la Banque, coordonnées `N 8907, E 6724` | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_minicarte-ennemi_002.png` | 2026-11-16 | Zoom minicarte : **triangle rouge (ennemi)** près de la Banque, coordonnées `N 8907, E 6724` | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie-vue-dessus_001.png` | 2026-11-16 | Prairie vue de dessus, personnage seul | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_008.png` | 2026-11-16 | Prairie, arbres et rochers, panneau cible « Young Oak Tree » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_jour-prairie_009.png` | 2026-11-16 | Prairie, arbres et rochers, panneau cible « Young Oak Tree » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-monde_001.png` | 2026-11-16 | **Carte du monde** ouverte : zone explorée en hexagones, icônes, panneau « Map Options » | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-monde-zoom_001.png` | 2026-11-16 | Carte du monde zoomée sur la zone de Snowrest | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-monde-zoom_002.png` | 2026-11-16 | Carte du monde zoomée sur la zone de Snowrest | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-monde-zoom_003.png` | 2026-11-16 | Carte du monde zoomée sur la zone de Snowrest | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-barre-coordonnees_001.png` | 2026-11-16 | Zoom : barre de la carte **« Keldan (R18) | (8918, 6700) »**, Set destination, Zoom, Pan, F2 Toggle Empire Grid | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-options_001.png` | 2026-11-16 | Zoom : Map Options (Buildings, Claims, Deployables, Empires, Placeables, Travelers, Watchtowers, Wind) | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-icones_001.png` | 2026-11-16 | Zoom : icônes de bâtiments sur la carte | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_carte-monde-dezoom_001.png` | 2026-11-16 | Carte du monde très dézoomée : région entière | 1.0 (build affiché : Early-Access-2-082daa0e) |
| `analyse/20261116_analyse_video-coupe-plusieurs-arbres_001.mp4` | 2026-11-16 | **Vidéo 1 min 30** : coupe de plusieurs arbres (Young Beech, Mature Oak, Young Oak) ; nombreux triangles rouges sur la minicarte ; message « inventaire plein » répété ; gains Rough Tree Bark, Tree Sap, Oak Seed | 1.0 |
| `analyse/20261116_analyse_video-minage-ferralith_001.mp4` | 2026-11-16 | **Vidéo 41 s** : minage de « Large Ferralith Vein » (30 000 points) puis « Ferralith Vein » (3 000) | 1.0 |
| `analyse/20261116_analyse_carte-repere-destination_001.png` | 2026-11-16 | Zoom carte du monde : repère de destination posé près d'une zone non découverte | 1.0 |
| `analyse/20261116_analyse_video-trajet-banque-camera-w_001.mp4` | 2026-11-16 | **Vidéo 30 s** : trajet de la zone de minage (Ferralith Vein) jusqu'à la Banque de Snowrest, caméra recentrée avec W, interface Bank ouverte à l'arrivée | 1.0 |
| `reference-ui/20261116_reference-ui_hud-coupe-arbre_001.png` | 2026-11-16 | **Image de référence des tests** : écran complet 1920×1080 (capture exacte par le bot), coupe d'un Mature Oak Tree 893/12000, endurance 65/100, position N 8927, E 6707. Versionnée. | 1.0 |
