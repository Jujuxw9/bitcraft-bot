# Contrôle complet du dépôt (T-15, 27/11/2026, 11:00)

Tâche planifiée T-15, sans Julien. Fiche : `taches-planifiees/T-15-controle-depot.md`. Résultat : **v0.4.33**, aucun changement de comportement du bot.

## 1. Build et tests
- `dotnet build` Debug et Release : **0 avertissement, 0 erreur** (avant et après les corrections).
- `dotnet test` : 742/742 au départ, **756/756** à la fin (14 nouveaux cas dans `V0433Tests`).

## 2. Mise en forme
- Au départ : **139 remarques** d'espaces (`dotnet format --verify-no-changes`), dans 13 fichiers anciens (listes écrites sur une ligne, accolades, retraits).
- Corrigé par `dotnet format whitespace` ; vérifié que seule la mise en forme change (relu). Après : **0 remarque**. Build et tests restent verts.

## 3. Code
- **Code mort** : aucun reste de l'onglet Recettes (`RecipeView` absent du code) ; aucun ancien cran de zoom ; aucun `TODO` / `FIXME` ; aucun fichier vide.
- **Touche Tab** : plus envoyée depuis la v0.4.25 mais encore dans la liste des touches permises (`GameKeys.Allowed`). **Laissée** (la retirer changerait la liste des sécurités ; sans effet aujourd'hui). Commentaire corrigé. Priorité basse : à retirer lors d'une prochaine version de la reprise.
- **Texte faux corrigé** : la bulle d'aide de l'interrupteur « Reprise des récoltes » disait encore « resélectionne la ressource (Tab) » ; elle dit maintenant « pose la souris sur le point de votre dernier clic et relance (M) ».
- **`using` inutiles** : aucun signalé par le compilateur ni par `dotnet format`.
- **Classes de plus de ~600 lignes** (signalées, pas refaites) :

| Fichier | Lignes | Avis |
|---|---|---|
| `App/MainWindow.xaml.cs` | 914 | Démarrage et liaisons ; déjà découpé en fichiers `MainWindow.*.cs`. Priorité basse. |
| `Input/Actions/ActionController.cs` | 742 | Contient aussi `GameKeys` et `ChatProbe` : les sortir dans leurs fichiers serait simple. Priorité moyenne. |
| `App/MainWindow.Travel.cs` | 738 | Déplacement (T-10 à T-14), a grossi en une nuit. À découper après l'essai réel (geste carte / suivi / menu). Priorité moyenne. |
| `Server/ServerReader.cs` | 677 | Lecture du serveur. Priorité basse. |

## 4. Sécurité du dépôt public
- `git grep` sur `@gmail`, `@hotmail`, `@outlook`, `password`, `api_key`, jetons longs (`eyJ…`), adresses IP : **rien de privé**. Seuls jetons trouvés : deux faux jetons de test (`H1ServerTests`, ex. `eyJhbGciOi.secret.sig`).
- Fichiers suivis : aucun `config.local.json`, aucune base `.db`, aucun journal, aucun dossier `carte/` ni `archive-chat/` (les fichiers `images/reference-ui/minicarte/…` sont des images de référence voulues).
- `.gitignore` couvre : `artifacts/`, `**/config.local.json`, `**/journaux/`, `carte/`, `archive-chat/`, `*.db`, `**/donnees-jeu/`, `images/**` (sauf `reference-ui`), `.env`. **Correct.**
- Le journal du bot écrit le **nom du PC** (ligne « Poste : … ») : sans danger tant que les journaux restent ignorés par Git (c'est le cas). À garder en tête si un journal est un jour copié dans `memory/`.

## 5. Tests ajoutés (`V0433Tests`, simples)
- Mémoire des ressources : gardée à **72 h pile**, retirée 1 s après ; texte « vue il y a 59 min » puis « vue il y a 2 h » ; ressource inconnue = aucun texte ; positions refusées (NaN, infini, négative, hors monde) comptées ; identifiant nul refusé.
- Barre de la grande carte : texte vide, sans nombres, nombre nul ou hors bornes refusés ; dernière paire gardée, séparateurs « ; » et « . » acceptés.
- Touches : aucune touche interdite (chariot, bateau, V, B) dans la liste permise ; Maj + V et Maj + B reconnues interdites ; Sprint + M permis.
- Reprise des récoltes : déjà bien couverte (`V0424Tests`, `V0425Tests`) ; rien ajouté.

## 6. Documentation
- `README.md` racine : annonçait la **v0.3.18** et 5 onglets → réécrit pour la v0.4.33 (6 onglets, carte, déplacement, sécurités).
- `tests/README.md` : **n'existait pas** → créé (organisation, règles, nombre de tests).
- `technique/architecture.md` : en-tête « État v0.4.12 » qui citait encore `RecipeView` → renvoi vers `architecture-suite.md` (sections 18 à 37), `RecipeView` marqué retiré.
- `technique/architecture-suite.md` : sections 29 à 36 de la nuit relues, cohérentes avec le code ; section 37 ajoutée.

## 7. Version et essai
- v0.4.33 publiée (`artifacts/release/BitcraftBot-v0.4.33.exe`, 69,8 Mo ; v0.4.32 rangée dans `artifacts/old`). Pas de Release GitHub (D-076).
- Essai sans le jeu (11:07:50 → 11:08:45, `--sans-lancer-jeu`, fermé proprement, code 0) : connexion en lecture seule à R18, **aucun « Abonnement refusé »**, **aucune ERREUR**, 3 412 ressources relues sur le disque, 77 morceaux de terrain, 9 morceaux reçus du serveur, barre d'action lue. Le module global refuse la liaison au démarrage (déjà connu depuis T-01, sans effet).

## 8. Ce qui reste (par priorité)
1. **Haute** : essai réel du déplacement avec Julien (T-11 n'a pas pu) : rien d'autre ne prouve « Se déplacer ici ».
2. **Moyenne** : sortir `GameKeys` et `ChatProbe` de `ActionController.cs` ; découper `MainWindow.Travel.cs` après l'essai réel.
3. **Basse** : retirer Tab de la liste des touches permises (inutile depuis v0.4.25) ; `MainWindow.xaml.cs` et `ServerReader.cs` à surveiller.
