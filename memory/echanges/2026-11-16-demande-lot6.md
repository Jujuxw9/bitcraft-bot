# Demande de captures et d'essais — lot 6 (2026-11-16, session 08)

Reste du lot 5, mis à jour. **Nouveau : les captures se font avec le bot** (v0.3.2), car « Impr. écran » prend les 2 écrans.

## Comment capturer
1. Lancer `artifacts/release/BitcraftBot.exe` (dernière version, D-040) (pas besoin de cliquer sur Démarrer).
2. Dans le jeu, appuyer sur **Num 0** (pavé numérique) : le bot enregistre l'écran du jeu seul, en 1920×1080 exact, dans `images/analyse/`. Une ligne « Capture du jeu enregistrée » apparaît dans le journal du bot.
3. Rien d'autre à faire : les noms sont automatiques. Dis-moi juste « lot 6 déposé » et ce que montre chaque capture, dans l'ordre (ex. « 1 : hêtre survolé, 2 : … »).

## Reçu le 2026-11-16 à 20:03 (57 captures Num 0)
- **D** (nuit : survol et coupe) : fait.
- **G** (fenêtres superposées) : fait.
- **H** (autres métiers) : fait (Foraging et Mining appris).
- Essai des mouvements : retour reçu (D-037).

## Reçu le 2026-11-16 à 20:10 (5 captures Num 0) : A, B, C non trouvés
Julien a dit « A B C OK », mais les 5 captures (20:10:30 à 20:10:44) montrent seulement le personnage près du rocher (Limestone), la caméra qui tourne, et un **lac** à gauche. Pas de hêtre (A), pas de vieil arbre à côté d'un jeune (B), pas de panneau après un clic droit lointain (C). **À refaire.** Les captures servent quand même pour **l'eau** (Q-020).

## Déjà fait (merci)
- Chiffre 5 (C1), vidéos de survol (V1, V2), coupe jusqu'à l'endurance vide (V3), barres d'action, inventaire plein.
- Lecture **de nuit** des coordonnées : vérifiée par Claude en direct (30/30, 2026-11-16 à 19:49).

## Captures (Num 0)

| # | Quoi | Pourquoi |
|---|---|---|
| A | **Mature Beech Tree** : 1 capture en le survolant (contour vert), 1 pendant la coupe | 2e espèce de vieil arbre (D-034) |
| B | **Un vieil arbre et un jeune côte à côte**, chacun survolé (2 captures) | Calibrer « le plus grand contour » (D-036) |
| C | **Clic droit sur un arbre loin** : capture dès que le panneau du haut s'affiche, pendant que le personnage marche | Vérifier D-036 |
| D | ~~De nuit : arbre survolé, coupe~~ | **Fait** |
| E | **Bonus de nourriture** : juste après avoir mangé (icône), puis sans bonus. Essayer aussi **Shift + E** (vu dans l'info-bulle : « mange le premier aliment de l'inventaire ») et dire ce qui se passe | Repas (D-032) |
| F | **Inventaire qui déborde** : couper avec l'inventaire plein, capture du message « Some items couldn't fit… » | Condition de retour à la Banque |
| G | ~~Fenêtres superposées~~ | **Fait** |
| H | ~~Autre métier en action~~ | **Fait** (Foraging, Mining) |
| I | Seulement si ça arrive **sans risque** : ennemi proche, attaque | Q-014 |

## Vidéos (OBS, comme d'habitude)

| # | Quoi | Durée | Pourquoi |
|---|---|---|---|
| V4 | **Set destination** : ouvrir la grande carte, cliquer une destination, fermer, laisser marcher jusqu'au bout | 1 min | Navigation (Q-002) |
| V6 | **Montée de niveau** d'un métier (si elle arrive pendant que tu filmes) | 10 s | Notification qui masque le panneau (Q-018) |

## Essai simple des mouvements (facultatif, 5 min)
Rappel : le bot a un onglet **Tests** où **la souris bouge toute seule** pendant 30 s (gestes « humains »). Tu l'as lancé à 18:11 (le journal le montre). Ce que je veux savoir, en une phrase : **quand la souris bougeait seule, est-ce que ça ressemblait à une main humaine, ou à un robot ?** (trop rapide, trop lent, trop régulier…). Si tu ne t'en souviens plus : onglet Tests → cocher « Activer le banc d'essai » → « Lancer le test souris » → regarder sans toucher la souris.

## Mise à jour du 2026-11-16 (fin de session 08) : une seule vidéo à la place
Julien préfère filmer plutôt que faire une capture par élément. **Oui, une vidéo suffit** : Claude en extrait lui-même les images.
Rappel : la vidéo est compressée (texte un peu flou). Elle sert à voir les **objets et l'eau**, pas à apprendre les chiffres. Pour les chiffres, les captures Num 0 restent meilleures.

### Vidéo « lot 6 » (OBS, 1920×1080, 30 images/s, 3 à 5 min, sans parler)
Dans cet ordre, en restant **2 à 3 s** immobile sur chaque élément :
1. **A** : survoler un **Mature Beech Tree** (hêtre), puis le couper 10 s.
2. **B** : se placer pour voir un **vieil arbre et un jeune** côte à côte ; survoler l'un, puis l'autre.
3. **C** : **clic droit sur un arbre loin** ; laisser le personnage marcher 3 s ; puis **clic gauche à côté du personnage** pour annuler (D-036).
4. **Eau** : marcher jusqu'à une **rive** (lac ou rivière) ; tourner lentement la caméra (Q puis D) pour montrer l'eau sous plusieurs angles ; W pour recentrer.
5. **Herbe** : 10 s de survol lent de **l'herbe vide** autour du personnage (aucun contour vert ne doit apparaître).
6. Si possible, **de nuit** : refaire le point 4 (l'eau de nuit).

Reste à part (quand l'occasion se présente) : E (bonus de nourriture, Shift + E), F (inventaire qui déborde), I (ennemi, sans risque), V4 (Set destination), V6 (montée de niveau).
