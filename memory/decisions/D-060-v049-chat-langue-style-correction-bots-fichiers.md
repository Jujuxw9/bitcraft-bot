# D-060 — v0.4.9 : langue du message, style relâché, faute corrigée, autres bots ignorés, phrases à côté du .exe

- **Statut** : Validée
- **Date** : 2026-11-24
- **Validée par** : Julien (réponses aux 7 améliorations proposées : « 1 OUI, 2 OUI, 3 OUI, 4 pour plus tard, 5 pour plus tard, 6 OUI, 7 OUI » ; « pas deux fois la même phrase » : « OUI »)
- **Liens** : D-059 ; détail : `technique/chat-gpt.md` section 9

## Décision
1. **Langue** : réponse en français si le joueur a « fr/ » **ou** si son message est écrit en français (plus de mots français que de mots anglais, accents comptés).
2. **Faute corrigée** : quand la réponse a une faute volontaire, une fois sur deux le bot envoie 1,5 à 4 s après un 2e message « *mot » (le mot juste), mêmes sécurités. Pas prêt à ce moment-là : correction abandonnée.
3. **Style relâché** 25 % du temps : pas de majuscule au début (sauf pseudo), pas de point final, une abréviation parfois (jsp, tkt, stp, bcp, pk, np, idk, thx, u…).
4. **Autres bots ignorés** : pseudo qui contient « bot », marque de bot dans le texte ([bot], « as an AI », « réponse automatique »…), ou message qui répète une réponse récente du bot. Écrit dans le journal.
5. **Phrases modifiables sans nouvelle version** : dossier `phrases-chat` à côté du .exe. Le bot y réécrit `modele-fr.txt` et `modele-en.txt` à chaque démarrage. Un `fr.txt` ou `en.txt` posé là remplace celui du .exe (copier le modèle, le renommer, le modifier, relancer le bot). Phrase qui parle de bot : refusée au chargement. Fichier inutilisable : phrases du .exe gardées.
6. « Pas deux fois la même phrase » (12 dernières) : **gardé** (D-059).

## Pour plus tard (Julien)
- Mémoire du joueur (« re », « encore toi ? »).
- Local : joueurs proches seulement.
