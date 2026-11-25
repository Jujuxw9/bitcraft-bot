# T-02 — v0.4.17 : capacités automatiques (tâche planifiée du 26/11, 08:45)

Lire d'abord [README.md](README.md) (règles communes) et les suivre du début à la fin.

## Le besoin (Julien, 25/11, D-064 point 3)
Quand Julien fait **lui-même** une action compatible (ex. couper du bois), le bot appuie sur la touche de la capacité de la barre d'action qui va avec (ex. **Hewing Strikes**), dès que sa recharge est finie.
- Un **interrupteur général** « Capacités auto » dans l'onglet BOT, carte OPTIONS, comme « Repas auto » et « Chat @gpt » (style `SwitchCheck`), gardé dans les réglages.
- Un **interrupteur par capacité** de la barre d'action (liste lue sur le serveur), pour activer ou non chacune.
- **Réserve d'endurance : 30 %** : jamais de capacité qui coûte de l'endurance si l'endurance passe sous 30 %.
- Seulement **pendant que Julien fait l'action lui-même** (la récolte automatique viendra plus tard).

## Ce qui existe déjà
- Barre d'action lue sur le serveur : `Server/World/ActionBar.cs` (`action_bar_state`, `ability_state` avec `cooldown`). Barre de Julien (journal du 25/11) : 1 attaque auto ; 2 Shortsword Strike ; 3 On Guard ; 4 Sprint ; 5 Reel In ; 6 Let It Run ; 7 Hewing Strikes ; 8 Heavy Swings ; 9 Equip Bone Tipped Arrows ; 0 Break the Roots.
- Données du jeu : `donnees-jeu/ability_custom_desc.json` (`ability_name`, `stamina_cost`, `cooldown`, `buffs[{buff_id, duration}]`, `profession_hit_skill_id`) et `buff_desc.json`. Exemples : Hewing Strikes recharge 60 s, endurance 0 ; Heavy Swings 25 d'endurance, recharge 120 s ; Sprint 50, recharge 120 s, effet 15 s ; Let It Run 10, recharge 1 s.
- Action en cours de Julien : `player_action_state` (le journal écrit déjà « Tâche : Début : récolte « Mature Birch Tree », Forestry… »), métier en cours (`SkillTracker`).
- Appui sur une touche : contrôleur d'actions commun (`Input/Actions/ActionController.cs`), avec ses sécurités (jeu au premier plan, écran « en jeu », chat fermé, pause quand Julien tape, aucune fenêtre du jeu ouverte). Le repas automatique (`MainWindow.Meals.cs`) est le meilleur modèle à copier.

## À faire
1. **Recherche d'abord** : quel métier pour quelle capacité (`profession_hit_skill_id` dans les données ; compléter avec le wiki communautaire bitcraft.wiki.gg si besoin, lien noté dans `memory/technique/liens-utilises.md`). Écrire le tableau capacité → métier → endurance → recharge → effet dans `memory/technique/capacites.md`.
2. Logique (testable sans fenêtre, dans `Server/World` ou `Core`) : décider « appuyer maintenant sur la touche N » à partir de : interrupteurs, action en cours de Julien et son métier, recharge restante (serveur), endurance (≥ 30 % après coût), effet déjà actif (bonus encore là : ne pas relancer). Une seule touche à la fois, au plus une toutes les 2 s.
3. Interface : interrupteur général dans OPTIONS ; liste des capacités de la barre avec un interrupteur chacune (carte simple, peu de texte) ; ligne de journal à chaque appui (« Capacité : Hewing Strikes (touche 7), coupe de bois »).
4. Tests automatiques de la logique (recharge, réserve 30 %, métier qui ne correspond pas, interrupteur coupé, effet actif).
5. Essai réel : sans le jeu, seulement l'interface (`--sans-lancer-jeu` : liste des capacités lue sur le serveur, interrupteurs). **Aucun appui de touche ne peut être essayé sans Julien** : le noter « à essayer par Julien » dans `etat.md`.

## Version
v0.4.17 (ou la suivante libre). Décision suivante libre.
