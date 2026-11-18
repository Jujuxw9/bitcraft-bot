# BitcraftBot.Server — brique « hybride » (lecture serveur)

Rôle : lire en direct les données du jeu sur le serveur officiel (SpacetimeDB), **en lecture seule**, avec le jeton de Julien (D-039).
Les actions restent au clavier et à la souris (brique Input).

Règles de la brique :
- Aucun appel de « reducer » (aucune action envoyée au serveur).
- Le jeton n'est jamais écrit en clair, jamais dans le journal, jamais dans Git.

Plan et constats : `memory/technique/hybride/`.
