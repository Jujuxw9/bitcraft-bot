# D-059 — v0.4.8 : chat vérifié avant chaque lettre, réponses par thème, fautes volontaires 5 %

- **Statut** : Validée
- **Date** : 2026-11-24
- **Validée par** : Julien (« Corrige, il doit même vérifier entre chaque frappe » ; « crée une sorte de base de données avec plein de phrases regroupées par thème, le bot doit analyser le message pour trouver le thème, pour ensuite prendre une réponse au hasard par rapport au thème […] fluide, compact, modifiable et évolutive » ; « une fonction qui peut faire des fautes d'orthographe ou de conjugaison […] genre 5 % du temps »)
- **Liens** : D-054, D-055, D-058 ; étude et détail : `technique/chat-gpt.md` section 8

## Contexte
Journal v0.4.7 (24/11, 00:03:48) : le chat s'est fermé 1 s après son ouverture (sans doute un clic de Julien en revenant dans le jeu, **À vérifier**). Le bot n'a rien vu et a tapé le message pendant 11 s **dans le jeu** (lettres = actions). Entrée a ensuite rouvert le chat. Cause : le chat n'était vérifié qu'avant et après la frappe.

## Décision
1. **Avant chaque lettre**, image neuve du jeu : le chat doit être ouvert. Sinon : plus aucune lettre, pas d'Entrée, alerte « chat fermé pendant la frappe ». Vérifié aussi juste avant Entrée.
2. **Clic de souris de Julien** pendant la frappe : arrêt, comme une touche du clavier.
3. **2 s d'attente** après un retour dans le jeu (jeu revenu au premier plan) ou après un clic de Julien, avant d'ouvrir le chat.
4. **Base de phrases par thème** : fichiers texte `src/BitcraftBot.Server/World/Phrases/fr.txt` et `en.txt`, intégrés au .exe. 28 thèmes (salutation, merci, arbres, minage, commerce, groupe, question… et « autre » pour le reste). Le bot cherche les mots du thème dans le message ; le thème avec le plus de mots trouvés gagne ; aucun mot : « autre ».
5. **Faute volontaire** dans 5 % des réponses : une seule, sur un seul mot, jamais sur le pseudo (grammaire courante « a / à », « et / est », « your / you're »… ou faute de frappe : lettres inversées, oubliée, doublée, touche d'à côté, accent oublié).

## Ajouté par Claude, confirmé par Julien le 2026-11-24 (D-060)
- Une phrase dite parmi les 12 dernières n'est pas redite (tant qu'il en reste d'autres dans le thème).

## Inchangé
1 réponse par minute au plus ; jamais d'allusion à un bot (D-055, testé sur toute la base) ; smiley 30 % ; langue : français si « fr/ », anglais sinon.

## Conséquences
- Changer ou ajouter des phrases : modifier les fichiers texte, puis fabriquer une nouvelle version (pas de fichier à côté du .exe pour l'instant).
- Un test vérifie que les deux fichiers n'ont aucune ligne mal écrite et que chaque thème a au moins 8 phrases par langue.
