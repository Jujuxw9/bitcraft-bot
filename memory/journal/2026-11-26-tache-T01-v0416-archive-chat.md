# Tâche planifiée T-01 — 2026-11-26 — v0.4.16 : archive du chat en local

- **Agent** : Claude Code (Opus 5.5), tâche planifiée, sans Julien
- **Durée / poste** : 03:30 → ~04:00, PC de Julien (jeu fermé, bot de Julien fermé)

## Objectif
Enregistrer tout le chat vu par le bot (Local, Région, LFG, Trade, messages privés) dans une petite base locale, et remettre l'onglet CHAT avec un tableau (prompt : `taches-planifiees/T-01-v0416-archive-chat.md`).

## Réalisé
- Base SQLite `archive-chat\chat.db` à côté du .exe (paquet `Microsoft.Data.Sqlite` 11.0.0, intégré au .exe unique). Doublons ignorés (sorte + identifiant du serveur). Écriture sur un fil à part. `.gitignore` : `archive-chat/`, `*.db`.
- Messages privés : 2e abonnement `direct_message_state` dans la liaison de découverte au module global (déjà existante, jeu fermé). Format pris du code serveur (`global_module/src/messages/global.rs`). Pas de nouvelle liaison.
- Message de Julien écrit par le bot : reconnu (même texte qu'une réponse @gpt, ou moins de 6 s après un envoi), archivé 10 s après pour avoir le temps de savoir.
- Onglet CHAT visible : recherche par joueur, filtre par canal, tableau le plus récent en haut, total ; chatbot replié sous un bouton.
- Journal : « Archive du chat : N messages, fichier … ».
- Tests : 11 nouveaux (`V0416Tests.cs`) ; 594 tests réussis sur 594 ; build 0 avertissement.
- Essai réel à 03:38 (`--sans-lancer-jeu`, bot d'essai de `bin\Debug`) : liaison région ouverte, **25 messages archivés en 4 s** (les 5 dernières minutes du chat), puis les nouveaux au fil de l'eau : **51 messages** après 4 min (relu au démarrage suivant, sans connexion). Onglet CHAT : la capture PrintWindow est sortie blanche (écran sans doute verrouillé la nuit, WPF ne dessine pas) ; le tableau est quand même rempli (info-bulle d'une ligne archivée vue par l'accessibilité Windows). Bot d'essai fermé proprement. Le .exe publié ouvre aussi sa base (essai sans connexion : « Archive du chat : 0 messages », un court journal de cet essai est dans `artifacts/release/journaux/v0.4.16/`). Module global refusé (jeu pas lancé depuis plus d'1 h) : **messages privés pas vérifiés**.
- Architecture découpée : `technique/architecture-suite.md` (sections 18 à 20) ; `architecture.md` repasse sous 300 lignes.

## Choix prudents faits seul
- Pas de liaison permanente au module global pour les messages privés (risque inconnu d'éjecter le jeu, constats H1). Conséquence : les messages privés ne sont lus qu'au démarrage, si le module global accepte.
- Les messages privés et le chat restent dans un fichier ignoré par Git (messages d'autres joueurs).

## Décisions prises (liens D-NNN)
- [D-068](../decisions/D-068-v0416-archive-chat.md).

## Questions ouvertes créées (liens Q-NNN)
Aucune. Questions pour Julien (aussi dans `taches-planifiees/etat.md`) :
1. Messages privés : veux-tu qu'on essaie une liaison au module global **pendant** la partie (pour les avoir en direct) ? Risque d'éjection du jeu à tester avec toi devant le PC.
2. L'archive garde tout, sans limite de durée. Faut-il effacer les vieux messages (par exemple après 3 mois) ?

## Prochaines étapes
- Julien : lancer la v0.4.16, jouer, ouvrir l'onglet CHAT ; vérifier la ligne « Messages privés (archive du chat) » du journal quand le jeu a été lancé il y a moins d'1 h.
- T-02 (08:45) : v0.4.17, capacités.
