# Passation : récolte automatique des arbres (étapes R1 à R7)

Prompt à coller dans un nouveau chat Claude Code, ouvert dans `C:\Users\throj\Desktop\bitcraft-bot`.

---

Reprends le projet bitcraft-bot pour la grosse étape « récolte automatique » (R1 à R7).

**Lire d'abord, dans l'ordre :**
1. `CLAUDE.md` (règles, interdits, méthode de travail avec Julien, versions).
2. `memory/README.md`, `memory/echanges/resume-global.md`, `memory/decisions/README.md`.
3. `memory/echanges/2026-11-19-plan-recolte-auto.md` : **le plan de bataille**, validé par Julien.
4. `memory/decisions/D-049-recolte-auto-choix.md` : ses réponses (arbres d'abord, vieux arbres, caméra W, Q / D avec précaution, ses clics = indices et pas vérité).
5. Pour le code : `memory/technique/architecture.md` (sections 1 à 16) et `memory/technique/hybride/architecture-serveur.md` (sections 17 à 33, surtout 31 à 33), puis `memory/technique/hybride/donnees-serveur.md`.
6. Décisions utiles : D-024 (commandes : clic droit sur un arbre = y aller et couper ; clic gauche au sol = annuler), D-028 (endurance : attendre 100 %), D-034 et D-036 (choix de l'arbre), D-037 (gestes avec une intention), D-045 (contrôleur d'actions), D-047 (pas de touche d'arrêt), D-048 (écran = cible seulement).

**Le but** : le bot coupe seul les vieux arbres autour du personnage, en boucle, en sécurité. On avance **étape par étape**, avec un essai de Julien entre chaque étape : R1 (apprendre la caméra en regardant Julien, aucune action) → R2 (dessiner les arbres du serveur sur l'image du jeu) → R3 (viser à la souris sans cliquer) → R4 (un clic droit, une coupe, vérifiée par le serveur) → R5 (boucle sur place : endurance, sac plein, arrêts) → R6 (roches) → R7 (ennemis). La Banque (H4, v0.5) viendra après. **Commencer par R1**, puis R2 si le temps le permet. Ne pas sauter d'étape.

**Point de départ technique (déjà en place, v0.3.20) :**
- Serveur (lecture seule) : position exacte du personnage, ressources autour avec leur position, nom, âge et santé (`SnapshotBuilder.NearbyResources`), meilleur arbre (`ResourceNames.IsHarvestTarget`), action en cours et cible frappée (`PlayerActivity.CurrentAction`, `LastExtract`), journal des tâches (`TaskJournal`).
- Écran : image du jeu capturée en direct (fil « Capture du jeu »), cible sélectionnée lue (`HudReader`) et rapprochée du serveur (`ScreenTargetMatcher`).
- Actions : `ActionController` (brique Input) : `PressAsync`, `ClickAsync` (trajet humain `HumanMouse`, zone autorisée, jeu actif et visible, souris bougée = arrêt, au plus 20 actions par minute, touches autorisées). Seule action branchée aujourd'hui : Maj + E (repas auto). Clavier écouté par `GlobalKeyboardHook` ; **R1 demande une écoute de la souris du même genre** (lecture seule, accord de Julien).
- Le personnage est au centre de l'écran. **W** remet la caméra en orientation et zoom de référence (Julien). Q / D tournent la vue : très sensibles.

**Règles à respecter (non négociables) :**
- Interdits de `CLAUDE.md` : pas d'exploit, pas de modification du client, pas de lecture mémoire du processus, pas d'injection, **aucune action envoyée au serveur** (lecture seule, D-039). Les actions passent uniquement par le clavier et la souris, via le contrôleur d'actions.
- Le bot se connecte au serveur **avant** le jeu, jamais après (sinon le jeu est éjecté). Ne jamais connecter le bot si BitCraft est lancé.
- Julien joue souvent : vérifier que BitCraft n'est pas au premier plan avant d'ouvrir une fenêtre du bot. Sinon, vérifier sur un **banc hors écran** : un petit programme dans le dossier temporaire de Claude, qui référence `BitcraftBot.App`, ouvre `MainWindow` hors de l'écran (`ShowActivated = false`, position −20000), appelle les méthodes par réflexion et enregistre des captures. Session 15 : c'est ainsi que la panne de l'image du jeu a été trouvée.
- Tester chaque livrable : tests automatiques (263 aujourd'hui, `dotnet test BitcraftBot.sln`) + essai réel ou banc.
- Chaque version : `Directory.Build.props` + message de version dans `MainWindow.xaml.cs`, puis `tools/publier-version.ps1` (bot fermé avant). **Toujours le .exe local, jamais de Release ni de tag GitHub.** Puis commit, push, sync.
- `git status` avant `git add` ; ne pas commiter `memory/technique/recettes-liste-2026-11-19.md` (une autre session le modifie).
- Français partout sauf le code (D-019). Julien ne lit pas le code : expliquer le comportement, pas les lignes. Phrases courtes. Poser une question au moindre doute. Rappeler les questions sans réponse à la fin de chaque message.

**Pièges déjà rencontrés :**
- Décoder des images WebP en parallèle bloque tout le programme (session 15) : toujours une à la fois.
- Ne pas faire tourner un travail long ou bloquant sur les fils partagés (`Task.Run`) : la capture a son propre fil.
- Édition de fichiers par script Python : `\r` dans une chaîne (ex. `artifacts\release`) devient un retour chariot ; utiliser l'outil d'édition ou des barres `/`.

**À la fin de la session :** fichier dans `memory/journal/`, mise à jour de `memory/echanges/resume-global.md` (10 lignes max), des index, de `memory/technique/architecture.md` / `hybride/architecture-serveur.md` (nouvelle section), un fichier d'essai pour Julien dans `memory/echanges/`, puis une passation si l'étape continue.

Note : Julien n'a pas essayé la v0.3.20 (« pas besoin »). La réparation a été vérifiée sur le banc hors écran ; garder un œil sur l'image du jeu et les icônes au premier essai réel.
