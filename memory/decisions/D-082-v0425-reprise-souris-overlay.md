# D-082 — v0.4.25 : reprise des récoltes à la souris (plus de Tab), overlay plus étroit

- **Statut** : Validée (tâche de nuit T-05, d'après la fiche écrite avec Julien le 27/11)
- **Date** : 2026-11-27
- **Validée par** : Julien (fiche `taches-planifiees/T-05-v0425-overlay-reprise.md`, D-081)
- **Liens** : D-080 (remplace sa méthode Tab), [architecture-suite.md](../technique/architecture-suite.md) section 29

## Contexte
Essai de la v0.4.24 : reprise réussie 1 fois sur 4. M (Interact) = clic sur l'élément survolé (contour vert) : sans la souris sur l'arbre, M ne fait rien. L'arbre reste sélectionné quand l'endurance tombe : Tab est inutile. Compteur d'échecs pas remis à zéro (abandon après un seul essai à 00:29). Overlay jugé trop large depuis la ligne système.

## Décision
1. **Tab retiré** de la reprise.
2. Le bot retient le **dernier clic de Julien dans le jeu** (gauche ou droit, écoute en lecture seule), avec la position du personnage et un compteur de mouvements de caméra (bouton du milieu, molette, Q, D, W). Le point est pris pour la ressource si le clic est fait au plus 8 s avant que le serveur montre la récolte (1 s après au plus). Chaque relance faite par Julien met le point à jour.
3. Point valable seulement si le personnage a bougé de moins d'une demi-case et si la caméra n'a pas bougé depuis le clic. Sinon **rien n'est appuyé** : « reprise impossible : cliquez une fois sur l'arbre ».
4. Reprise : souris déplacée d'une façon humaine sur le point (**sans clic**, nouveau geste `MoveAsync` du contrôleur, mêmes sécurités), 150 ms, contour vert mesuré (journal seulement, **à vérifier**), puis M ; confirmation par le serveur comme avant. La souris n'est pas remise à sa place.
5. Compteur d'échecs remis à zéro à chaque arrêt de la reprise et à chaque récolte lancée par Julien.
6. Overlay : police 12 / 10,5, marges réduites, largeur bornée à 300 ; ligne système courte « Bot 1,5 % · 585 Mo │ Jeu 22 % · 4,3 Go » (42 caractères au plus).

## À vérifier
- Le contour vert est-il bien vu par la mesure (seuil : 12 pixels verts de plus) ? Lire « Contour vert autour du point » dans le journal.
- Le jeu prend-il le survol d'une souris déplacée par le bot (SendInput) comme celui de Julien ?

## Conséquences
- Tests 649/649. Essai sans le jeu : journal propre, « Reprise des récoltes activée ».
