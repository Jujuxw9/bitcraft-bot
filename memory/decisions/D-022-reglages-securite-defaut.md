# D-022 — Réglages par défaut de la sécurité

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-004

## Contexte
D-004 fixe 4 mécanismes d'arrêt sans valeurs par défaut. Julien propose le pavé numérique pour l'arrêt d'urgence (choix de la touche laissé à Claude) et 15 min de session.

## Décision
- Touche d'arrêt d'urgence : **pavé numérique 5** (`Num 5`). Au centre du pavé, facile à trouver sans regarder.
- Détection par le code de la touche physique (scancode), pour marcher que le Verr. Num soit actif ou non.
- Durée maximale d'une session : **15 minutes**.
- Les deux restent modifiables dans l'interface (D-004).

## Alternatives écartées
- `F12`, `Pause` : proposées avant, Julien préfère le pavé numérique.

## Conséquences
- **À vérifier** : `Num 5` n'est pas utilisé par le jeu (capture C10, réglages de touches).
