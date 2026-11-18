# Q-021 — Passer par le réseau / SpacetimeDB au lieu de la vision ?

- **Statut** : Résolue → D-039 (voie hybride prioritaire)
- **Priorité** : Haute (touche D-001 et l'interdit n° 5)
- **Créée le** : 2026-11-17
- **Liens** : D-001, D-007, D-008, D-033, `00-contexte/regles-et-risques.md`

## Demande de Julien
Un ami joueur propose d'abandonner (ou de compléter) la vision : lire les trames réseau, ou faire un programme externe qui parle au serveur, puisque le serveur est open source (et, selon lui, le client aussi).

## Faits vérifiés le 2026-11-17
- **Le client n'est pas public.** Le README de BitCraftPublic dit : le dépôt « n'inclut pas encore le client ni les outils pour se connecter au jeu officiel ». Aucun dépôt client dans l'organisation clockworklabs (liste lue avec `gh`).
- Le jeu = un module SpacetimeDB. Un client se connecte en WebSocket chiffré (TLS), s'abonne à des tables (requêtes SQL) et reçoit les changements ; il agit en appelant des « reducers ». Des SDK publics existent (C#, Python, TypeScript, Rust).
- FAQ officielle (développeurs) : lire des données avec **son propre jeton** est en général autorisé ; lecture limitée à la région du joueur ; **outils de triche ou de bot interdits**, sanction possible jusqu'au ban définitif. Programme « Verified developer » rouvert le 2026-03-31.

## Options
1. **Écouter les trames** à côté du jeu : impossible sans casser le chiffrement (proxy d'interception). C'est de l'interception : écarté.
2. **Client tiers complet** (connexion + appels de reducers) : techniquement le plus simple et le plus fiable. Mais c'est exactement l'interdit n° 5, le plus facile à détecter côté serveur, et un bot déclaré au sens des règles.
3. **Hybride** : connexion en **lecture seule** avec le jeton de Julien pour « voir » (position, arbres, inventaire), actions toujours au clavier/souris. Lecture autorisée en principe par la FAQ, mais l'usage final (bot) est interdit ; et cela lève l'interdit n° 5.
4. **Garder la vision** (D-001) + données statiques (D-033) + code serveur pour comprendre les règles. Statu quo.

## Avis de Claude
Option 4 recommandée. L'option 3 est la seule autre défendable ; elle exige que Julien lève l'interdit n° 5 par une décision explicite. Options 1 et 2 : non.

## Ce qui bloque
Choix de Julien.
