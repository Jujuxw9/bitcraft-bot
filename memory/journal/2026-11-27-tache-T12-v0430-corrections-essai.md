# Tâche T-12 (nuit du 27/11, 08:00) — v0.4.30 : corrections de « Se déplacer ici »

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Résultat** : v0.4.30 publiée (`artifacts/release/BitcraftBot-v0.4.30.exe`), tests 708/708, essai sans le jeu propre. D-087, Q-028.

## Point de départ
T-11 n'a pas pu essayer le déplacement (jeu bloqué sur l'écran d'accueil). Aucune capture de la carte, de la barre ni de la visée. La fiche demande alors de relire le code de T-10 « avec les yeux de l'essai ».

## Défauts trouvés (classés)
| Défaut | Classe | Fait |
|---|---|---|
| Le trajet part d'un clic dans la fenêtre du bot : le jeu n'est pas au premier plan, la touche de la carte est refusée tout de suite, le trajet rate à chaque fois | **Bloquant** | Corrigé : attente du retour sur le jeu (Alt + Tab), 20 s au plus |
| Carte non lue à 0,6 s : la touche était rappuyée « pour refermer » même si rien n'était ouvert (pouvait ouvrir la carte et tout bloquer) | Gênant | Corrigé : 3 lectures, rappui seulement si la carte grise est vue |
| Carte encore à l'écran 0,5 s après la fermeture (animation) : fausse alerte et tout bloqué | Gênant | Corrigé : 3 regards sur 1,5 s |
| Zone de la barre jamais vérifiée sur une vraie image | Gênant | Vérifiée par un test OCR sur la capture de la grande carte |
| Démarrage du jeu sans personne (accueil, « Enter BitCraft ») | Gênant (essais de nuit) | Pas codé : contraire à D-052 → Q-028 |
| Ordre des nombres de la barre, échelle, écart destination / cible | Inconnu | Rien changé à l'aveugle (consigne de T-11) |

## Vu en passant
- Sur la capture de la grande carte, la boussole en haut à droite affiche la position du personnage (« N 8899, E 6723 ») : la barre (9196, 7258) donne donc le **nord d'abord**. Le bot trouve l'ordre seul de toute façon (mesure à trois points).

## Tests
- `dotnet build` : 0 avertissement. `dotnet test` : 708/708 (7 nouveaux, `V0430Tests`).
- Essai sans le jeu (08:05, `--sans-lancer-jeu --essai-menu-deplacement`) : menu permis à 5 E et 5 N puis « refusé : jeu fermé », 40 E grisé « trop loin » ; aucun « Abonnement refusé », aucune ERREUR. Pas de capture de la fenêtre du bot (rien de visuel nouveau).
