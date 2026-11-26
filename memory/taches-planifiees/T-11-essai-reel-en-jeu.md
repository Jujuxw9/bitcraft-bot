# T-11 (07:00) — Essai réel en jeu, 20 minutes au plus

Règles communes : `nuit-2026-11-27-regles.md`. **Lis le bilan (surtout T-10)** : tu essaies son mode d'essai.

## Autorisation de Julien (27/11, 00:55)
Julien a donné son accord pour que **cette tâche seule** lance le jeu pendant qu'il dort : **20 minutes au plus**, **aucune récolte**, seulement la grande carte et de petits déplacements. Il range le chariot et le bateau avant la nuit. Règles de sécurité : `nuit-2026-11-27-regles.md` §3 (jamais de chariot ni de bateau ; ennemi : Sprint 4 et s'éloigner ; à terre : **Get Up, jamais Go Home** ; ne pas mourir ; pas d'escalade).

## Avant de lancer
1. Conditions, sinon **pas d'essai** (le noter et finir proprement) : T-10 a publié une version avec le mode d'essai ; aucun processus `BitCraft` ni `BitcraftBot-v*` ne tourne (Julien levé ?) ; quota OK ; il est entre 07:00 et 07:40.
2. Lire dans le bilan la ligne de commande exacte donnée par T-10.

## Déroulé
1. Lancer **le bot** de la dernière version depuis `artifacts\release` avec le mode d'essai : c'est **le bot qui lance le jeu** (ordre obligatoire : bot connecté d'abord, puis jeu par Steam, D-066). Noter l'heure.
2. Surveiller le journal du bot (lecture du fichier, toutes les 30 s environ) : connexion, lancement du jeu, écran de connexion, « en jeu », étapes du mode d'essai.
   - Si l'écran de connexion demande une action (bouton « Enter BitCraft ») et que le bot ne sait pas la faire : **ne pas cliquer à sa place** ; noter, fermer, finir.
3. Laisser le mode d'essai faire sa suite. **Ne rien piloter toi-même dans le jeu** (pas de clics, pas de touches), sauf pour arrêter.
4. **Arrêt** : à la fin du mode d'essai, ou au bout de **20 min** depuis le lancement, ou à la moindre anomalie (vie qui baisse, écran inconnu, fenêtre inattendue) : fermer le bot proprement (`CloseMainWindow` sur son numéro de processus), puis **fermer le jeu** que le bot a lancé (fenêtre du jeu : `CloseMainWindow` ; si une confirmation apparaît ou si le jeu ne se ferme pas en 60 s : arrêt du processus `BitCraft`, c'est la tâche qui l'a lancé). Vérifier qu'il ne reste ni jeu ni bot.

## Ce qu'il faut rapporter (bilan + `memory/journal/2026-11-27-tache-T11-essai-reel.md`)
- Chronologie : lancement, écran de connexion, en jeu, chaque étape (ouverture de la carte, échelle mesurée, clic, destination lue sur le serveur, arrivée, retour), fermeture.
- Ce qui a marché, ce qui a raté, avec les lignes de journal et les captures (`images/analyse/20261127_essai-deplacement_*`).
- **Captures de la cible en haut de l'écran** (pour T-13 : le bot n'arrive plus à la lire).
- Vie et position au début et à la fin (le personnage doit être là où il était, ou à quelques cases).
- Liste claire « à corriger » pour T-12.

## Fini quand
- Jeu et bot fermés, rapport écrit, commit/push. **Pas de version** (aucun code changé ; si un bug bloque l'essai, le noter pour T-12, ne pas corriger ici).

## Interdit
- Dépasser 20 min de jeu. Récolter. Toucher au chariot ou au bateau. Cliquer « Go Home ». Escalader. Laisser le jeu ouvert. Lancer le jeu sans passer par le bot.
