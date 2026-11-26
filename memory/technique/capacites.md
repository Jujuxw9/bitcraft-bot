# Capacités de la barre d'action (v0.4.17, D-069)

Recherche faite le 2026-11-26 (tâche T-02) dans les données du jeu : `donnees-jeu/ability_custom_desc.json`, `buff_desc.json`, `skill_desc.json` (BitCraft_GameData). Le wiki n'a pas été consulté : les données suffisaient.

## Constat important
Le champ `profession_hit_skill_id` vaut **1 (« ANY ») pour toutes les capacités** : il ne dit pas le métier. Le bot déduit le métier **des effets du bonus** donné par la capacité : premier effet positif dont le nom commence par un métier de catégorie « Profession » (`ForestrySpeed` → Forestry, `MiningPower` → Mining, `FishingPower` / `FishingSpeed` → Fishing, `HuntingWeaponPower` → Hunting). Sans effet de métier (Sprint) : capacité jamais lancée par le bot.

## Tableau

| Capacité | Métier (déduit) | Endurance | Recharge | Effet (bonus) | Par défaut dans le bot |
|---|---|---|---|---|---|
| Hewing Strikes | Forestry | 0 | 60 s | vitesse en forestry +10 % pendant 20 s | **allumée** |
| Heavy Swings | Mining | 25 | 120 s | mining power +8 pendant 20 s | **allumée** |
| Break the Roots | Forestry | 10 | 1 s | vitesse en forestry +1 % pendant 1 s | coupée (à répéter sans arrêt, coûte de l'endurance) |
| Reel In | Fishing | 10 | 1 s | fishing speed +0,05 pendant 5 s | coupée (mini-jeu de pêche) |
| Let It Run | Fishing | 10 | 1 s | fishing power +2, mais récolte et fabrication −15 % pendant 5 s | coupée (mini-jeu de pêche) |
| Equip Bone Tipped Arrows | Hunting | 0 | 0 s | hunting weapon power +35 pendant 1 h | coupée (chasse = combat, pas une récolte) |
| Equip Gem Tipped Arrows | Hunting | 0 | 0 s | hunting weapon power +73 pendant 1 h | coupée |
| Sprint | aucun | 50 | 120 s | vitesse de marche +40 % pendant 15 s | jamais (pas liée à une récolte) |

Règle « par défaut » : allumée si liée à un métier **et** recharge d'au moins 10 s. Julien peut tout changer case par case (choix gardé dans `config.local.json`).

## Barre de Julien (journal du 25/11)
1 attaque auto ; 2 Shortsword Strike ; 3 On Guard ; 4 Sprint ; 5 Reel In ; 6 Let It Run ; 7 Hewing Strikes ; 8 Heavy Swings ; 9 Equip Bone Tipped Arrows ; 0 Break the Roots.

## À vérifier (par Julien, en jeu)
- La touche envoyée est la **position physique** de « 1 » à « 0 » (en AZERTY : `& é " ' ( - è _ ç à`). À vérifier que le jeu lance bien la capacité ainsi.
- Hewing Strikes et Heavy Swings demandent-ils un outil en main ? Si la touche ne donne rien deux fois, le bot met la capacité en pause 5 min et l'écrit dans le journal.
- La pêche est-elle une « récolte » (`Extract`) pour le serveur ? Pas vu en réel.
