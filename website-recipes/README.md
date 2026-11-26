# Site des recettes BitCraft (Riverwood Recipes)

Site web des recettes de BitCraft Online pour Julien, ses amis et la guilde Riverwood (D-064, D-077).
Fonctionnement et règles : `memory/site-recettes/architecture.md`.

## Lancer la maquette sur le PC
1. Fabriquer les données (télécharge les tables publiques du jeu et les images) :
   `py -3 -B website-recipes/tools/build_data.py`
2. Lancer le serveur : `py -3 -B website-recipes/serve.py`, puis ouvrir http://127.0.0.1:8790

## Tests
- `node --test website-recipes/tests/test_calc.mjs`
- `py -3 -B -m unittest discover -s website-recipes/tests -p "test_*.py"`

## Dossiers
- `public/` : la page (HTML, JavaScript, CSS). Textes du site en anglais.
- `tools/` : fabrication des données.
- `tests/` : tests automatiques.
- `build/` : données et images fabriquées, **non suivies par Git**.

Rien de privé dans ce dossier (dépôt public) : adresses du serveur et domaine dans un fichier local `*.local.json`, ignoré par Git.
