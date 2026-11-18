# Session 09 — 2026-11-17 — Voie réseau, puis voie hybride

- **Agent** : Claude Code
- **Durée / poste** : PC de Julien

## Objectif
Évaluer l'idée d'un ami (passer par le réseau / SpacetimeDB), puis lancer la voie hybride choisie par Julien.

## Réalisé
- Vérifié : le client du jeu n'est **pas** public.
- Lu la FAQ officielle : lecture avec son jeton permise, limitée à la région ; bots interdits.
- Trouvé la méthode de connexion (code par e-mail → jeton), l'hôte et les modules (versions 2025, à vérifier en 1.0).
- Code serveur 1.0 : « la connexion la plus récente gagne » → risque de déconnexion du jeu ; parade : lancer le bot avant le jeu (à tester).
- Repo : nouvelle brique `src/BitcraftBot.Server` (vide, compile) ; docs `memory/technique/hybride/`.
- Interdit n° 5 modifié (CLAUDE.md, règles et risques) : lecture seule permise.

## Décisions prises
- [D-039](../decisions/D-039-voie-hybride.md) : voie hybride prioritaire, vision en voie B.

## Questions ouvertes créées
- [Q-021](../questions-ouvertes/Q-021-voie-reseau-spacetimedb.md) (résolue → D-039)

## Prochaines étapes
- Réponses de Julien (compte Steam ou e-mail, région, essai de déconnexion).
- Livrable **H1 — Lecture serveur**.
