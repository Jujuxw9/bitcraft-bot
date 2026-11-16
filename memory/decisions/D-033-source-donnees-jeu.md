# D-033 — Source des données du jeu : BitCraft_GameData

- **Statut** : Proposée
- **Date** : 2026-11-16
- **Validée par** : — (Julien laisse la question en suspens le 2026-11-16, à reprendre plus tard)
- **Liens** : Q-004, D-008

## Contexte
BitCraftPublic n'a que les schémas, pas les valeurs (Q-004). Julien a trouvé le dépôt **github.com/BitCraftToolBox/BitCraft_GameData**.

## Constat (vérifié le 2026-11-16)
- Copie des tables de données statiques (« desc tables ») du jeu : objets, ressources, recettes de récolte, etc. 112 fichiers JSON.
- **Mise à jour automatique chaque jour** (dernière : 2026-11-15, donc après la sortie 1.0).
- Branche conseillée : `cereal/cs` (JSON lisible). Exemple : `static/resource_desc.json` (2 Mo) donne nom, points de vie, tier, catégorie de chaque ressource.
- Vérification : « Mature Oak Tree » = 12 000 points (comme à l'écran), « Young Oak Tree » = 3 000. La ressource lue « Tier 1 - Fiber Plant » 380/1100 est un **« Bush »** (1 100 points).
- Données lues sans connexion au serveur : conforme aux règles (outil en lecture seule de données publiques).
- **Pas de licence** sur ce dépôt : on ne recopie pas ces fichiers dans notre dépôt public.

## Décision proposée
- Un script `tools/` télécharge les tables utiles dans `data/gamedata/` (**ignoré par Git**, jamais republié).
- `BitcraftBot.GameData` lit ces fichiers : noms connus (correction OCR), points de vie, outils requis, temps de récolte.
- En attendant, les noms manquants sont ajoutés à la main dans `KnownNames`.

## Alternatives écartées
- Saisie manuelle depuis le jeu : long et incomplet.
- API Bitjita : données vivantes, plus complexes ; utile plus tard si besoin.

## Conséquences
- Les données suivent les mises à jour du jeu (relancer le script après un patch).
