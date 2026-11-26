# D-068 — v0.4.16 : archive du chat en local

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (besoin du 25/11, D-064 point 4 ; réalisée par la tâche planifiée T-01, D-067)
- **Liens** : D-039, D-054, D-061, D-064, D-067, [architecture-suite.md](../technique/architecture-suite.md) section 20

## Décision
1. Le bot enregistre **tout le chat texte qu'il voit** pendant qu'il est connecté : Local, Région, Looking For Group, Trade. Pour chaque message : qui (nom + entité), quand, quel canal, quel texte, la région, et si c'est une réponse du bot (@gpt).
2. Les **messages privés** de Julien (reçus et envoyés) sont lus seulement pendant la découverte au module global (liaison courte déjà faite au démarrage, jeu fermé). Pas de nouvelle liaison. Si le module global refuse (jeu pas lancé depuis plus d'1 h), pas de messages privés : une ligne dans le journal.
3. Base **SQLite locale** `archive-chat\chat.db` à côté du .exe, **ignorée par Git** (contient des messages de joueurs). Un message n'est jamais enregistré deux fois. Écriture sur un fil à part.
4. **Onglet CHAT** de nouveau visible : tableau (heure, canal, joueur, texte), recherche par joueur, filtre par canal, le plus récent en haut, nombre total. Le chatbot (statistiques, test, historique) est replié en dessous.
5. But : garder l'historique et, plus tard, nourrir le chatbot. Rien n'est envoyé nulle part ; lecture seule du serveur (D-039 inchangé).

## À vérifier
- Format réel des messages privés (jamais reçus : module global refusé pendant l'essai du 26/11 à 03:38).
- Choix prudent de la tâche : pas de liaison permanente au module global (risque d'éjecter le jeu) ; les messages privés reçus pendant la partie ne sont donc archivés qu'au démarrage suivant, s'ils sont encore sur le serveur.

## Complément (2026-11-26 au soir)
Julien : **ne jamais effacer** les vieux messages de l'archive, le but est de tout garder.
Messages privés (Julien, 26/11 au soir) : **tout enregistrer** dans l'archive, messages privés compris. Le chatbot **ne répond jamais** aux messages privés (pour l'instant). Aujourd'hui, les messages privés ne sont lus qu'au démarrage, si le module global accepte ; les lire en direct demande une liaison permanente au module global (risque d'éjection du jeu à tester avec Julien, « À vérifier »).
