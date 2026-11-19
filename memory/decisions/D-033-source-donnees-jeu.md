# D-033 — Source des données du jeu : BitCraft_GameData

- **Statut** : Validée
- **Date** : 2026-11-16 (proposée), validée le 2026-11-16 (session 08)
- **Validée par** : Julien : « si c'est à jour et qu'on peut s'y fier, oui. À Claude de bien l'intégrer. »
- **Liens** : Q-004, D-008, D-034

## Contexte
BitCraftPublic n'a que les schémas, pas les valeurs (Q-004). Le dépôt **github.com/BitCraftToolBox/BitCraft_GameData** (branche `cereal/cs`) copie les tables de données statiques du jeu en JSON.

## Vérifications faites (2026-11-16)
- **À jour** : un commit par jour, dernier le 2026-11-16 à 13:59 UTC (commit `00297fe`).
- **Fiable** : valeurs conformes à l'écran : Mature Oak Tree 12 000 points, Young Oak Tree 3 000, Bush 1 100 ; coupe d'un chêne = hache tier 1, Forestry niveau 1, 1,6 s par coup (1,59 s affichées en jeu).
- **Pas de licence** : on ne recopie pas ces fichiers dans notre dépôt public.
- Lecture de données publiques sur GitHub : **aucune connexion au serveur du jeu** (règles respectées).

## Décision
1. **Le bot télécharge lui-même** les 8 tables utiles (ressources, recettes de récolte, biomes, objets, cargaisons, nourriture, types d'outils, métiers) dans `donnees-jeu/`, **à côté du .exe**. Ce dossier est ignoré par Git : jamais republié.
2. **Seulement si le dépôt a changé** (comparaison du commit). Sinon : rien à télécharger (40 ms).
3. **Vérification avant usage** : fichiers lisibles, assez de ressources et d'objets, valeurs vues à l'écran retrouvées, biome « Safe Meadows » présent. Si une nouvelle version échoue : **l'ancienne est gardée**.
4. **Contrôle permanent** : quand le bot lit une cible à l'écran, il compare sa santé max aux données. Désaccord = alerte dans le journal.
5. Sans Internet : le bot garde la version déjà présente, ou sa petite liste de noms intégrée.
6. Alerte si les données ont plus de 7 jours (risque de décalage après un patch).

## Alternatives écartées
- Script séparé lancé à la main : on oublierait de le relancer après un patch.
- Copier les fichiers dans le dépôt : pas de licence.
- API Bitjita : données vivantes, plus complexes ; utile plus tard si besoin.

## Conséquences
- Les noms de **878 ressources** corrigent les lectures du nom de la cible (avant : 19 noms saisis à la main).
- Le bot peut exclure ce qui n'est pas un arbre ordinaire (ex. **Maker's Tree**, étiquette « Wonder Resource »).
- Base pour la suite : endurance par coup, outils requis, nourriture (D-042).
