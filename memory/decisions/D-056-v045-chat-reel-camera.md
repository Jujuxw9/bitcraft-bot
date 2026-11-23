# D-056 — v0.4.5 : réponses « @GPT » envoyées pour de vrai, caméra adoucie coupée, molette après W, exploration en v0.5

- **Statut** : Validée
- **Date** : 2026-11-23
- **Validée par** : Julien (« 1 envoi vraiment dans le chat ; 2 oui ; 3 OUI si tu penses que ça aide ; 4 on garde pour 0.5 ; 5 OK »)
- **Liens** : D-054, D-055, Q-027 (point 9), Q-026, Q-025 (point 5), D-038, D-053

## Contexte
Après la v0.4.4, cinq questions restaient : mode essai ou envoi réel pour « @GPT », caméra adoucie, vue plus haute après W, onglet « Bêta : exploration », vrai essai de W.

## Décision
1. **Chat** : pas de mode essai. La v0.4.5 envoie vraiment la réponse dans le chat Région, au clavier (règles de D-054 et D-055).
2. **Caméra adoucie** (« Camera pan smoothing », onglet Commandes du menu) : Julien la passe à non.
3. **Vue plus haute** : le menu n'a aucun réglage de hauteur (captures du 23/11). Quand le bot appuiera lui-même sur W (récolte automatique), il donnera ensuite **toujours le même nombre de crans de molette** pour monter la vue. Nombre de crans : **À vérifier** par essai. Pas codé en v0.4.5 : aujourd'hui le bot n'appuie jamais sur W, et tourner la molette après un W de Julien le gênerait.
4. **Exploration** (D-038) : gardée pour la v0.5.
5. **Vrai essai de W** : Julien le fera au prochain lancement, chat fermé.

## Conséquences
- v0.4.5 : chat ouvert reconnu à l'écran ; W, Q, D tapées dans le chat ignorées par la caméra ; aucune action du bot quand le chat est ouvert.
- Les phrases toutes faites qui laissaient entendre que c'est un bot (« IA de bûcheron », « mes circuits », « Bip boup ») sont remplacées (D-055).
