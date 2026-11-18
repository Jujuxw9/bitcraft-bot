# D-039 — Voie hybride prioritaire : lire le serveur, agir au clavier et à la souris

- **Statut** : Validée
- **Date** : 2026-11-17
- **Validée par** : Julien
- **Liens** : Q-021, D-001, D-007, `technique/hybride/`

## Contexte
La vision seule a ses limites (jeu graphiquement complexe). Le jeu tourne sur SpacetimeDB. La FAQ officielle autorise la lecture des données avec son propre jeton, limitée à la région du joueur. Le client du jeu n'est pas public.

## Décision
1. **Priorité : la voie hybride.** Une connexion en **lecture seule** au serveur officiel, avec le jeton de Julien, pour « voir » (position, arbres, inventaire, vie, endurance, faim…). Les actions restent au **clavier et à la souris** sur le vrai jeu.
2. La vision (D-001, D-007) continue en **voie B** : en pause pour les captures et vidéos, gardée pour la sécurité et le contrôle croisé.
3. Même dépôt. Nouvelle brique `src/BitcraftBot.Server`. Docs dans `memory/technique/hybride/`.
4. **Interdit n° 5 modifié** : la connexion en lecture seule est permise. Aucune action n'est envoyée au serveur par notre programme (aucun appel de reducer).
5. Interdits 1 à 4 inchangés (Julien l'a confirmé : pas d'injection, pas de modification du client).

## Alternatives écartées
- Écoute des trames : tout est chiffré, il faudrait intercepter la connexion.
- Client bot complet (actions envoyées au serveur) : pas maintenant. Julien est ouvert à en reparler ; cela demandera une nouvelle décision.

## Conséquences
- Risque de sanction inchangé sur le fond (la FAQ interdit les bots), plus visible côté serveur : chaque connexion est enregistrée.
- Point technique critique : « la connexion la plus récente gagne » (voir `technique/hybride/constats.md`). Ordre de lancement à respecter.
- Le jeton doit être protégé comme un mot de passe : il donne accès au compte.
