# D-004 — Sécurité : 4 mécanismes d'arrêt

- **Statut** : Validée
- **Date** : 2026-11-15
- **Validée par** : Julien
- **Liens** : D-005

## Contexte
Le bot pilote clavier et souris. Julien doit pouvoir tout arrêter à tout moment.

## Décision
4 mécanismes, **chacun activable/désactivable dans l'interface** :
1. Touche globale d'arrêt d'urgence.
2. Arrêt si la souris bouge manuellement.
3. Arrêt sur anomalie (écran inconnu, perte de vie).
4. Durée maximale de session.

L'arrêt global coupe immédiatement toute action.

## Alternatives écartées
- Arrêt uniquement par fermeture de la fenêtre : trop lent.

## Conséquences
- La sécurité vit dans `BitcraftBot.Core` et a priorité sur toute tâche.
- L'arrêt d'urgence fait partie de la v0.1.
