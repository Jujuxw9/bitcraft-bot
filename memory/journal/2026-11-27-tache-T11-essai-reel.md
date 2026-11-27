# Tâche T-11 (nuit du 27/11, 07:00) — essai réel en jeu du déplacement (v0.4.29)

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Résultat** : **essai non abouti**. Le jeu est resté sur l'écran d'accueil « Press any button to start » ; personne n'a pu faire les deux clics de démarrage. Aucun déplacement essayé. Jeu ouvert 40 s seulement.

## Chronologie
| Heure | Événement |
|---|---|
| 07:00 | Début. `git pull` (à jour), quota 5 h 12 %, semaine 36 %, aucun verrou, verrou T-11 posé. |
| 07:01:00 | Vérifié : aucun processus `BitCraft` ni `BitcraftBot-v*` (jeu et bot de Julien fermés). |
| 07:01:33 | Bot lancé : `artifacts\release\BitcraftBot-v0.4.29.exe --essai-deplacement` (depuis `artifacts\release`). |
| 07:01:34 | Journal : « Essai du déplacement : attente du jeu (écran « en jeu », position lue, 3 s sans souris ni clavier). 15 min au plus. » |
| 07:01:36 | Serveur : personnage trouvé, abonnements acceptés (aucun « Abonnement refusé »). Position lue : **N 9041, E 6808** (point de départ prévu). |
| 07:01:39 | « Étape 3/3 : … jeu lancé par Steam ». |
| 07:01:41 | Fenêtre BitCraft trouvée (1920 × 1080 en (0, 0), écran principal). |
| 07:01:47 | « Jeu : écran d'accueil (mot « press any ») ». Capture gardée par le bot. |
| 07:01:50 | Demande d'accès à l'écran (outil de contrôle de l'ordinateur) pour « BitCraft Online » : **refusée d'office** : « ne peut pas être approuvé pendant une tâche planifiée ». Les deux clics autorisés par Julien étaient donc impossibles. |
| 07:01:55 | Code du bot vérifié : il reconnaît l'accueil et la connexion, mais **ne clique jamais** dessus (seulement des captures). Le mode d'essai aurait attendu 15 min pour rien. |
| 07:02:19 | Bot fermé proprement (`CloseMainWindow` sur son numéro) : déconnexion du serveur d'abord, « Liaison fermée ». |
| 07:02:20 | Jeu fermé proprement (`CloseMainWindow`, sorti tout de suite, sans confirmation). |
| 07:02:30 | Vérifié : plus aucun processus `BitCraft` ni `BitcraftBot`. |

Je n'ai **pas** contourné le refus (pas de clic envoyé par un script) : ce serait passer outre une protection de l'app.

## Journal du bot (extraits)
- Aucune ligne `[ERREUR]`, aucun « Abonnement refusé ».
- `[Écran] Jeu : écran d'accueil (mot « press any »)` (07:01:47 et 07:02:15) ; entre les deux, un « écran inconnu (aucun mot connu) » (07:02:08 : animation de l'accueil probablement).
- `[Session] Session arrêtée après 00:42 : arrêt demandé avec le bouton.` puis déconnexion propre.
- Fichier : `artifacts/release/journaux/v0.4.29/2026-11-27/session_2026-11-27_07-01-33.log`.

## Captures (dans `images/analyse/`, locales : ce dossier est ignoré par Git)
- `20261127_essai-reel_01-ecran-accueil.png` : écran « PRESS ANY BUTTON TO START », bulle Steam en bas à droite.
- `20261127_essai-reel_02-ecran-inconnu.png` : écran classé « inconnu » à 07:02:10.
- Pas de capture de la cible en haut de l'écran (jamais en jeu) : reste à faire pour T-13.

## Vie et position
- Personnage jamais entré en jeu (`signed_in: false` sur le serveur) : position **inchangée, N 9041, E 6808**. Aucun risque pris.

## À corriger pour T-12
1. **Démarrage du jeu sans personne** : le bot doit passer seul l'écran d'accueil (un clic au centre de la fenêtre BitCraft) puis l'écran de connexion (clic sur « Enter BitCraft », lu à l'écran), **seulement s'il a lancé le jeu lui-même**, jamais « Quitter ». C'est une action hors de l'écran « en jeu » : contraire à D-052 tel qu'écrit → **question ouverte pour Julien** (proposer une exception limitée à ces deux clics, comme l'exception de la grande carte en v0.4.29). Sans réponse, T-12 peut la coder **coupée par défaut** et allumée seulement par `--essai-deplacement`.
2. Sinon, autre voie (pas de code) : Julien ajoute « BitCraft Online » dans les réglages de la tâche planifiée (contrôle de l'ordinateur) pour qu'une future tâche puisse faire les deux clics.
3. Rien d'autre à corriger : connexion serveur, lancement par Steam, reconnaissance de l'accueil, fermeture propre du bot et du jeu ont marché.

## Questions pour Julien
1. Le bot peut-il faire lui-même les deux clics de démarrage (accueil, « Enter BitCraft ») quand c'est lui qui a lancé le jeu ? (exception à D-052)
2. Veux-tu ajouter BitCraft Online aux applications permises des tâches planifiées, pour un nouvel essai de nuit ?
3. Refaire l'essai du déplacement avec toi présent (5 min) ?
