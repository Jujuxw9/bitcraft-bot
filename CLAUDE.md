# Instructions pour tout agent IA

## Avant toute action

1. Lire [memory/README.md](memory/README.md).
2. Lire [memory/echanges/resume-global.md](memory/echanges/resume-global.md).
3. Lire les décisions en vigueur : [memory/decisions/README.md](memory/decisions/README.md).

## Règles

- Respecter les décisions validées. Toute proposition contraire passe par une nouvelle question ouverte (`memory/questions-ouvertes/Q-NNN-*.md`).
- Ne pas inventer : ce qui n'est pas vérifié est marqué « À vérifier ».
- **Langue (D-019)** : seules les lignes de code sont en anglais. Tout le reste en français : commentaires, commits, docs, `memory/`, messages des scripts, textes de l'interface.

- **Code (D-023)** : Claude code tout, en autonomie. Julien ne lit pas le code. Expliquer l'architecture et le comportement, jamais les lignes de code. Tenir `memory/technique/architecture.md` à jour.

## Méthode de travail avec Julien (2026-11-16)

- Julien n'est pas technique : il a le besoin, Claude a les compétences. L'échange doit être fluide.
- **Tester en interne chaque livrable** avant de le donner : tests automatiques + essai réel sur le PC.
- **Poser une question au moindre doute** sur une fonction ou un livrable, plutôt que supposer.
- **Rappeler à la fin de chaque message les questions encore sans réponse.**
- Liens envoyés par Julien : les évaluer et les ranger dans `memory/technique/sources.md`. Tout lien utilisé va aussi dans `memory/technique/liens-utilises.md`.

## Versions et fichiers (D-040)

- Nouvelle version : changer `<Version>` dans `Directory.Build.props` et le message de version (`MainWindow.xaml.cs`), puis lancer `tools/publier-version.ps1`. La dernière version est toujours dans `artifacts/release/`, les anciennes dans `artifacts/old/vX.Y.Z/`.
- **Chaque nouvelle version : toujours fabriquer le .exe en local (`artifacts/release`), mais pas de Release ni de tag GitHub** (demande de Julien, 2026-11-19). Commit, push et sync seulement.
- Julien lance toujours `artifacts\release\BitcraftBot-vX.Y.Z.exe` (nom avec la version, depuis la v0.3.15). Ses journaux sont dans `artifacts/release/journaux/vX.Y.Z/AAAA-MM-JJ/`.
- `references/BitCraftPublic/` : copie du code serveur officiel. Vérifier de temps en temps s'il a changé (voir `references/README.md`).

## Voie hybride (D-039)

- Le bot lit le serveur officiel en lecture seule, avec le jeton de Julien. Le jeton ne doit jamais apparaître dans un journal, dans Git ni dans une réponse.
- Le bot se connecte **avant** le jeu, jamais après : sinon le jeu est éjecté (« Logged in elsewhere »). Pas de reconnexion automatique.
- L'e-mail de Julien reste privé : Julien le tape lui-même dans le bot.

## En fin de session

- Ajouter un fichier dans `memory/journal/` (gabarit dans `memory/journal/README.md`).
- Mettre à jour `memory/echanges/resume-global.md` (10 lignes max).
- Mettre à jour les index (`README.md` de chaque dossier modifié).

## Style attendu par Julien

- Français, phrases courtes, concret, sans remplissage.
- Poser des questions plutôt que supposer.
- Signaler explicitement les incohérences.

## Interdits non négociables

- Pas d'exploit.
- Pas de modification du client.
- Pas de lecture mémoire du processus.
- Pas d'injection.
- Pas d'action envoyée au serveur officiel par un client tiers (lecture seule permise, D-039).

Détail : [memory/00-contexte/regles-et-risques.md](memory/00-contexte/regles-et-risques.md).
