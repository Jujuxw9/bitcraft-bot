# D-069 — v0.4.17 : capacités automatiques de la barre d'action

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (besoin du 25/11, D-064 point 3 ; réalisée par la tâche planifiée T-02, D-067)
- **Liens** : D-042, D-052, D-064, D-067, [capacites.md](../technique/capacites.md), [architecture-suite.md](../technique/architecture-suite.md) section 21

## Décision
1. Pendant que **Julien récolte lui-même** (action « Extract » lue sur le serveur, métier de la ressource connu), le bot appuie sur la touche de la capacité de la **première barre d'action** qui aide ce métier (ex. Hewing Strikes en coupant du bois), dès que sa recharge est finie.
2. Le métier d'une capacité est déduit des **effets de son bonus** (le champ métier du jeu vaut toujours « ANY »). Sprint et toute capacité sans effet de métier ne sont jamais lancés.
3. Conditions, toutes obligatoires : interrupteur général allumé ; capacité allumée ; recharge finie (serveur) ; son bonus n'est plus actif ; **endurance après le coût ≥ 30 %** du maximum (endurance inconnue = rien) ; au plus **une touche toutes les 2 s** ; sécurités du contrôleur d'actions (jeu au premier plan, écran « en jeu », chat fermé, aucune fenêtre du jeu, Julien ne tape pas depuis 3 s, 20 actions par minute au plus).
4. Confirmation par le serveur (recharge relancée ou bonus apparu) sous 6 s ; deux échecs de suite : capacité en pause 5 min, alerte dans le journal.
5. Interface : interrupteur **« Capacités auto »** dans BOT › OPTIONS (gardé dans les réglages) et, dessous, **un interrupteur par capacité** de la barre, avec son état (prête, recharge, bonus actif, endurance…) et le détail au survol. Journal : « Capacité : Hewing Strikes (touche 7), coupe de bois ».
6. Touches autorisées ajoutées au contrôleur : rangée du haut (position physique de « 1 » à « 0 », « - », « = »).

## Choix prudents de la tâche (sans Julien)
- Interrupteur général **coupé au premier démarrage** (nouveauté jamais essayée en jeu) ; ensuite le choix de Julien est gardé.
- Par défaut, seules les capacités à longue recharge (≥ 10 s) sont allumées : Hewing Strikes, Heavy Swings. Break the Roots, Reel In, Let It Run (1 s, mini-jeu de pêche) et les flèches sont coupées.

## À vérifier
- Que le jeu lance bien la capacité avec la touche physique (AZERTY : `&`, `é`… pour 1, 2…). Jamais essayé : aucun appui sans Julien.
- Outil nécessaire en main pour Hewing Strikes / Heavy Swings ?
