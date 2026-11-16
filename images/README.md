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
