# Session 18 bis — 2026-11-23 — Étude du module « @GPT » (chat + Claude Haiku)

**Agent** : Claude Code (session lancée en parallèle de la session 18, sans toucher à `src/`)

## Objectif
Étudier un module qui répond dans le chat du jeu aux joueurs qui écrivent « @GPT », avec des réponses de Claude Haiku 5.5. Étude seulement, rien qui envoie quoi que ce soit au jeu ou au serveur.

## Réalisé
- Code serveur lu : table `chat_message_state` (module de région, publique, sans filtre), canaux Local / Région / Groupe / Commerce, messages privés (`direct_message_state`, module global, visibles seulement pour l'expéditeur et le destinataire), effacement après 2 jours, 250 caractères, 3 messages par 15 s en Région.
- Deux voies d'envoi expliquées : appel au serveur **interdit** (D-039) ; clavier dans le jeu via le contrôleur d'actions (D-045), seule voie possible.
- **Incohérence trouvée** : D-024 interdit au bot la touche Entrée (le chat). Le module demande une nouvelle décision.
- Risques, coût de l'API (environ 0,0002 $ par réponse), architecture envisagée, mode essai sans envoi.
- Note : `technique/chat-gpt.md`. Question : Q-027.

## Décisions prises
- Aucune.

## Questions ouvertes créées
- Q-027.

## Prochaines étapes
- Réponses de Julien à Q-027, puis (si oui) mode essai : lecture du chat et réponse affichée dans la fenêtre du bot, sans envoi.

## Suite (même jour) : réponses de Julien → D-054
- D-054 écrite : Entrée permise pour répondre dans le chat (D-024 remplacée en partie), « @GPT » en majuscules, tout le monde, Région, 1 réponse par minute (la plus récente), même sans question, phrases toutes faites (pas de clé API).
- Code : `World/GptChat.cs` (lecture du chat, choix du message, phrases toutes faites), aucun envoi, pas encore branché. 17 tests, 369 au total, tous réussis.
- Restent dans Q-027 : clavier pendant la récolte, marque « [GPT] », mode essai.
