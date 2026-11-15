# D-023 — Claude code tout, Julien suit l'architecture

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-019

## Contexte
Julien fait ce projet pour le plaisir. Il ne veut ni écrire ni lire de code.

## Décision
- Claude a **une autonomie totale sur le code** : choix techniques internes, bibliothèques, structure des classes.
- Julien ne relit pas le code. Il valide le **comportement** (tests en jeu) et l'**architecture**.
- Les explications de Claude portent sur l'architecture et le fonctionnement (quoi, pourquoi, comment c'est relié), pas sur les lignes de code.
- Les choix qui changent ce que Julien voit ou vit (risque, comportement en jeu, interface, réglages) restent soumis à sa validation.

## Alternatives écartées
- Julien écrit une partie du code : refusé.

## Conséquences
- `memory/technique/architecture.md` est le document de référence pour Julien. À tenir à jour à chaque version, avec schémas.
- Claude doit fournir des tests automatisés pour compenser l'absence de relecture humaine.
