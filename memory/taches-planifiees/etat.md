# État des tâches planifiées

Chaque tâche ajoute ses lignes ici (démarrée, reportée, finie), avec l'heure. Julien lit ce fichier pour savoir où on en est.

| Tâche | Démarrée | Finie | Version | Résumé, à essayer, questions pour Julien |
|---|---|---|---|---|
| T-01 (v0.4.16, archive du chat) | 26/11 03:31 | 26/11 03:43 | v0.4.16 (.exe publié) | **Résumé** : tout le chat vu par le bot (Local, Région, LFG, Trade) est enregistré dans `archive-chat/chat.db` à côté du .exe (ignoré par Git) ; messages privés lus au démarrage si le module global accepte ; onglet CHAT de retour (tableau, recherche par joueur, filtre par canal, chatbot replié). Tests 594/594, essai réel : 51 messages archivés en 4 min. **À essayer** : lancer la v0.4.16, jouer, ouvrir CHAT, chercher un joueur, filtrer un canal ; dans le journal, la ligne « Messages privés (archive du chat) ». **À vérifier** : messages privés jamais reçus (module global refusé à 03:38). **Questions** : 1) messages privés en direct pendant la partie (liaison globale permanente, risque d'éjection à tester avec toi) ? 2) effacer les vieux messages (ex. après 3 mois) ? |
| T-02 (v0.4.17, capacités) | — | — | — | prévue le 26/11 à 08:45 |
| T-03 (v0.4.18, carte) | — | — | — | prévue le 26/11 à 13:15 |
| T-04 (vérification de la mémoire, lecture seule) | — | — | — | prévue le 26/11 à 14:00 ; rapport : artifacts\rapports\rapport-memoire-2026-11-26.md |
