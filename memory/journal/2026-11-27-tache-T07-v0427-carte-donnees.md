# Tâche T-07 (nuit du 27/11, 03:00) — v0.4.27 : carte, données

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Décision** : D-084 ; architecture : `technique/architecture-suite.md` section 31

## Fait
- Zoom : crans jusqu'à 140 et 200 cases. Morceaux découverts cherchés à 230 cases (au lieu de 130).
- **Cause des zones noires** (journal v0.4.24 de Julien) : 74 morceaux découverts dans la région, 51 gardés ; les 23 manquants étaient au-delà de 130 cases, donc jamais demandés. Le reste du « noir » : morceaux pas découverts (jamais lus, comme dans le jeu), dessinés dans un bleu nuit presque noir. Lien zones découvertes ↔ morceaux vérifié dans le code du serveur (juste). Corrections : rayon 230, zones inconnues en gris-bleu discret.
- Mémoire des ressources sur le disque (`carte/ressources/ressources.bin`), date « vue le », nettoyage 72 h au démarrage puis chaque heure, 200 000 au plus, écriture groupée (2 min) et à la fermeture, survol « vue il y a N h ».
- Contrôle : positions hors monde, identifiant nul, doublons, morceaux illisibles comptés.
- Terrain gardé de moins de 24 h pas redemandé (sauf 3 × 3 autour du personnage). Mesures au journal (Ko par envoi, abonnements par minute, total).

## Testé
- Build 0 avertissement, tests 662/662 (9 nouveaux, `V0427Tests`).
- Essai sans le jeu 1 (03:07, 75 s) : aucun refus, aucune erreur ; abonnement n° 4 « 10 morceaux déjà découverts » : 77 gardés sur 77 découverts ; 3 419 ressources écrites (198 Ko).
- Essai 2 (03:08, 45 s) : « 3419 lues sur le disque », « 3419 gardées, 0 retirées (plus de 72 h) », aucun refus.

## Mesures
- Avant (v0.4.24) : envoi complet 1 358 Ko ; parties fixes ≈ 364 Ko (colonies 1 280 lignes, banques, inventaires) ; terrain 9 morceaux ≈ 167 Ko ; reste = 2 747 ressources × 3 tables.
- Après (v0.4.27, même endroit) : 1 472 Ko (3 419 ressources lues : plus de ressources, pas plus de terrain) ; le terrain reste à 9 morceaux (3 × 3 autour : rien à retirer ici). Gain réel seulement quand la zone touche 4 × 4 morceaux.

## Pas fait
- Parties fixes lues une seule fois : demande plusieurs abonnements ou un désabonnement (changement de protocole, interdit sans essai réel).
- Affichage rapide à 200 cases : T-08 (une forme par case : lent à 200).
