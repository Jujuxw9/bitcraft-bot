# T-05 (01:00) — v0.4.25 : overlay plus étroit, reprise des récoltes fiable

Règles communes : `nuit-2026-11-27-regles.md` (à lire d'abord). Tu es la **première** tâche de la nuit.

## Pourquoi
Julien coupe de gros arbres (22 000 points de vie) : l'endurance tombe à 0 plusieurs fois. La v0.4.24 relance la récolte toute seule (D-080, `src/BitcraftBot.Server/World/HarvestResume.cs`, `src/BitcraftBot.App/MainWindow.Resume.cs`). Essai de Julien (journal `artifacts/release/journaux/v0.4.24/2026-11-27/session_2026-11-27_00-14-41.log`, catégorie « [Reprise] ») : **1 réussite sur 4**.
Cause, donnée par Julien : **M (Interact) = un clic sur l'élément que la souris survole** (le jeu l'entoure de vert). Si la souris est sur l'herbe, M et Tab ne font rien. La réussite de 00:22:35 : la souris était restée sur l'arbre. Autre constat du journal : l'arbre **reste sélectionné** (target_state = l'arbre) quand l'endurance tombe : Tab ne sert à rien.

## À faire
1. **Overlay plus étroit** (`OverlayWindow.cs`, `MainWindow.Overlay.cs`, `Core/Monitoring/SystemSummary.cs`) : Julien le trouve trop large depuis la ligne système (v0.4.23). Viser **au plus ~300 px de large** à 1920×1080 : ligne système plus courte (ex. « Bot 1,5 % · 585 Mo │ Jeu 22 % · 4,3 Go »), police un peu plus petite, marges réduites. Garder l'arrondi CPU 0,5 % et RAM 5 Mo pour le bot (Julien) ; pour le jeu, Go avec une décimale suffit. Adapter le test `V0423Tests.OverlayRoundsCpuAndRam`.
2. **Reprise : souris sur la ressource avant M.**
   - Retenir **où était la souris** quand la récolte a démarré : le bot écoute déjà la souris en lecture seule (`MainWindow.Camera.cs`, clics droits dans le jeu, molette, bouton du milieu). Garder la position du **dernier clic (gauche ou droit) dans le jeu** avant le début de la récolte, avec la position du personnage à ce moment-là.
   - Cette position n'est valable que si : le personnage n'a pas bougé depuis le clic (moins d'une demi-case ; sinon la caméra a suivi et l'arbre n'est plus sous ce point), et la caméra n'a pas été tournée depuis (bouton du milieu, molette, W, touches Z Q S D : le module caméra le sait déjà, « caméra tournée »).
   - Bonne idée à garder : **mettre à jour ce point à chaque coup confirmé** quand Julien a lui-même relancé la récolte (son clic de relance est fait personnage arrêté : c'est le meilleur point).
   - Au moment de reprendre : déplacer la souris sur ce point (contrôleur d'actions `BitcraftBot.Input` : regarder comment le repas ou les capacités envoient les gestes, et s'il existe déjà un déplacement de souris permis ; sinon l'ajouter avec les mêmes sécurités), attendre ~150 ms, **vérifier le contour vert** si c'est faisable simplement (analyse du 22/11 : `memory/technique/hybride/analyse-visee-2026-11-22.md` §3 ; comparer une petite capture autour du point avant/après le survol, chercher des points verts vifs) ; si la vérification n'est pas fiable, la noter « À vérifier » et compter sur la confirmation par le serveur. Puis M, puis confirmation serveur (déjà faite).
   - Si aucun point valable : ne pas appuyer au hasard. État « reprise impossible : cliquez une fois sur l'arbre » et une ligne de journal ; dès que Julien clique l'arbre, le point est appris.
   - **Retirer Tab** de la reprise (inutile, et sans souris dessus il ne fait rien). Si la cible du serveur n'est plus l'arbre, M sur le bon point suffit (le clic cible aussi).
   - Après le geste, **remettre la souris où elle était** n'est pas utile (Julien est absent) : ne pas le faire.
3. **Défaut du compteur d'échecs** : `_failures` doit repartir à 0 quand la reprise s'arrête (Stop) ou qu'une nouvelle récolte de Julien commence. Ajouter un test qui reproduit le cas de 00:29 (deux arrêts successifs sur le même arbre, le second ne doit pas abandonner après un seul essai).
4. Tests `V0425Tests.cs` : point valable / pas valable (personnage bougé, caméra tournée), pas de Tab, compteur remis à zéro, largeur de la ligne système (longueur du texte bornée).

## Fini quand
- Tests verts, build sans avertissement, v0.4.25 publiée, essai sans le jeu OK (journal propre, ligne « Reprise des récoltes activée »).
- Bilan rempli avec **ce que Julien doit essayer** : couper un gros arbre sans toucher la souris après son clic, laisser l'endurance tomber deux fois ; regarder dans le journal « Récolte … reprise (confirmé par le serveur) ».

## Interdit
- Aucun clic de souris envoyé par le bot pour la reprise (seulement déplacer la souris + touche M). Aucun déplacement du personnage. Ne pas toucher à la carte (T-07/T-08) ni aux recettes (T-06).
