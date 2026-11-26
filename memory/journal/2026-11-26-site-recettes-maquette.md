# Session « site des recettes » — 2026-11-26 — Cadrage, proposition, maquette

- **Agent** : Claude Code (Opus 5.5), session à part (D-064)
- **Durée / poste** : soirée du 26/11, PC de Julien

## Objectif
Suivre le prompt `echanges/2026-11-26-prompt-site-recettes.md` : cadrage, proposition, maquette du site des recettes.

## Réalisé
- Lecture du dépôt privé DOGGO-ESPORT (modèle d'hébergement) ; rien de privé recopié ici.
- Cadrage avec Julien (`site-recettes/cadrage.md`) : 5 cas concrets, anglais, PC + téléphone, pas de compte, pas de mot de passe pour l'instant.
- Recherches (`site-recettes/recherches.md`) : BitCraft Codex, bitjita, brico.app ; table des supplies du claim ; pas d'image officielle publique.
- Proposition validée (D-077) ; organisation avec la session du bot (`site-recettes/organisation.md`).
- **Maquette** (`website-recipes/`, `site-recettes/maquette.md`, `site-recettes/architecture.md`) : tout le jeu, recherche tolérante aux fautes, arbre zoomable avec coches et « have », totaux mis en commun, ordre de fabrication, stacks, « Where to get it », « Used in », pages Supplies et Tools, version téléphone. 33 tests automatiques ; essai réel dans le navigateur.
- Méthode de Julien notée (besoin → plan → recherches → réalisation → tests → GitHub) ; il garde une seule session et fait des « compacts ».

## Décisions prises (liens D-NNN)
- [D-077](../decisions/D-077-site-recettes-proposition.md) : proposition validée, recette la plus facile, arbre zoomable avec stacks, anglais, sous-domaine `bitcraft-riverwood`, dossier `website-recipes/`.

## Questions ouvertes créées (liens Q-NNN)
- Aucune Q-NNN. Questions posées à Julien en fin de message : essai de la maquette, taille des stacks, worktree.

## Prochaines étapes
- Julien essaie la maquette sur son PC (Brave) et donne ses retours.
- Étape 4 : conteneur Docker sur Unraid, mise à jour quotidienne des données, déploiement (avec l'accord de Julien), réseau (OVH, box, Nginx Proxy Manager) pas à pas avec Julien.
