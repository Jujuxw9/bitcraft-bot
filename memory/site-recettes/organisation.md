# Organisation : site des recettes et bot (préconisation de Claude, 2026-11-26)

Julien a demandé : même dépôt ou pas, quel dossier, mémoire partagée ou non, parallèle ou série.

## Préconisation
1. **Même dépôt** (`bitcraft-bot`, D-064), **dossiers séparés** :
   - code du site : `website-recipes/` ; code du bot : `src/`, `tests/`. Aucune session ne touche les dossiers de l'autre.
   - mémoire du site : `memory/site-recettes/`.
   - Avantage : une seule mémoire, mêmes règles, et le site lit les mêmes données du jeu que le bot.
2. **Mémoire partagée, avec des règles** pour les fichiers communs (décisions, `resume-global.md`, index) :
   - toujours `git pull` juste avant de prendre un numéro de décision, puis commit et push tout de suite ;
   - chaque session ne commite que ses propres fichiers (vérifier `git status` avant) ;
   - dans `resume-global.md`, le site tient sur **une ligne** ; le détail reste dans `memory/site-recettes/`.
3. **Hybride** plutôt que tout en parallèle :
   - les deux sessions peuvent avancer en même temps tant que chacune reste dans ses dossiers ;
   - mais chaque session ouverte consomme le quota de 5 h : quand le quota est serré, **une seule à la fois** (la priorité est choisie par Julien) ;
   - les moments d'attente s'emboîtent bien : pendant que Julien essaie une version du bot, la session du site avance, et inversement.
4. **Rien de privé dans le dépôt public** : l'adresse du serveur Unraid et le domaine vont dans un fichier local ignoré par Git.

## Constaté le 2026-11-26
- La session du bot travaillait en même temps dans le même dossier : elle avait déjà pris D-073 à D-076 sans les avoir encore toutes commitées. La session du site a pris D-077 et n'a commité que ses fichiers. La ligne de D-077 dans `decisions/README.md` est ajoutée quand ce fichier n'a plus de modifications en cours de l'autre session.
- Option plus sûre si les collisions se répètent : un **worktree** (copie de travail à part, même dépôt) pour la session du site. Pas mis en place sans l'accord de Julien.
