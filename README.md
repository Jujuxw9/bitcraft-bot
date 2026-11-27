# bitcraft-bot

Agent autonome pour BitCraft Online : lecture de l'état du jeu, prise de décision et automatisation du farm de ressources, sous supervision humaine. Projet d'apprentissage.

> ⚠️ Les Community Guidelines de BitCraft interdisent toute automatisation. Projet expérimental, risque de sanction accepté par le porteur. Détail : [memory/00-contexte/regles-et-risques.md](memory/00-contexte/regles-et-risques.md).

## État

**v0.4.33** (27/11/2026) :
- Voie hybride (D-039) : le bot lit le serveur officiel **en lecture seule**, avec le jeton du joueur (code reçu par e-mail, jeton chiffré) : position, vie, endurance, faim, ressources proches, sac et banques, bonus, expérience des métiers, action en cours, capacités.
- 6 onglets (D-043, D-083) : Bot (tableau de bord, options, sécurité), Chat (archive locale du chat, recherche), Personnage, Voyageurs, Carte, Paramètres (avec le journal). Onglet Recettes retiré en v0.4.26 ; le livre des recettes sert encore ailleurs.
- **Carte** : fond lu sur le serveur (biomes, relief, eau, zones découvertes), zoom jusqu'à 200 cases, ressources vues gardées 72 h sur le disque, filtres par catégorie et par tier.
- **Déplacement** (v0.4.29 à v0.4.32) : clic droit sur la carte du bot → « Se déplacer ici » (30 cases au plus, sans escalade ni nage), « Aller à cette ressource », « Récolter jusqu'à épuisement ». **Pas encore essayé en jeu.**
- Reprise des récoltes quand l'endurance tombe (souris posée sur le point du dernier clic, puis M) ; capacités automatiques (coupées au départ) ; repas automatique (Maj + E).
- **Contrôleur d'actions commun** (D-045) : toutes les touches et tous les clics passent par les mêmes sécurités. Touches du chariot et du bateau (`-`, `=`, Maj + V, Maj + B) **jamais** envoyées. Vie qui baisse : Sprint et fuite. À terre : « Get Up », jamais « Go Home ».
- Lecture de l'écran en contrôle : cible en haut de l'écran, fenêtres ouvertes, chat ouvert, grande carte.
- Overlay sur l'écran du jeu : état du bot, charge du bot et du jeu.
- Sécurité : bouton « Arrêter », durée maximale, arrêt si la souris bouge, pause quand on tape au clavier, journaux. Déconnexion du serveur garantie à la fermeture.

Règle : connecter le bot **avant** de lancer le jeu. Si le jeu est déjà lancé, le bot refuse de se connecter, sinon le jeu serait éjecté.

Suite : essai réel du déplacement avec Julien. Voir [memory/echanges/resume-global.md](memory/echanges/resume-global.md) et l'[architecture](memory/technique/architecture.md).

## Principe

Un `.exe` Windows lancé à côté du jeu, avec sa fenêtre sur le 2ᵉ écran :
- le bot **lit** l'état du jeu sur le serveur, et l'écran en contrôle ;
- il **agit** uniquement au clavier et à la souris, sur le vrai jeu.

L'humain supervise et peut tout arrêter (touche d'urgence, mouvement de souris, anomalie, durée max).

Stack : C# / .NET 10, WPF.

## Lancer le bot

Dernière version : `artifacts\release\BitcraftBot-vX.Y.Z.exe` (le nom porte la version). Raccourci bureau « BitCraft Bot » (créé par `tools/creer-raccourci-bureau.ps1`) : lance toujours la dernière version. Les anciennes versions sont dans `artifacts\old\vX.Y.Z\` (D-040, sur le PC seulement).

## Pour les développeurs

Prérequis : Windows, [SDK .NET 10](https://dotnet.microsoft.com/download).

```powershell
dotnet build
```

```powershell
dotnet test
```

Fabriquer une version (lit `Directory.Build.props`, range l'ancienne dans `old`) :

```powershell
powershell -ExecutionPolicy Bypass -File tools/publier-version.ps1
```

Les versions marquées d'un tag `vX.Y.Z` sont aussi construites par `.github/workflows/release.yml` (GitHub Releases).

## Organisation

| Dossier | Contenu |
|---|---|
| [memory/](memory/README.md) | Mémoire du projet : contexte, décisions, questions ouvertes, journal |
| `src/` | Code C# : App, Core, Perception, Input, GameData, Server |
| [tests/](tests/README.md) | Tests xUnit |
| [images/](images/README.md) | Captures d'écran (référence UI, entraînement, analyse) |
| [models/](models/README.md) | Modèles de vision `.onnx` (voie B, en pause) |
| [tools/](tools/README.md) | Scripts utilitaires |
| [references/](references/README.md) | Copie du code serveur officiel BitCraftPublic (Apache 2.0) |
| `artifacts/` | Versions fabriquées : `release/` et `old/` (sur le PC, ignoré par Git) |

## Notes

- `config.local.json` (réglages du PC), les journaux et les captures restent sur le PC (ignorés par Git).
- Le jeton du compte est chiffré par Windows, hors du dépôt, et n'apparaît jamais dans les journaux.
- Agents IA : lire [CLAUDE.md](CLAUDE.md) avant toute action.

## Licence

MIT — voir [LICENSE](LICENSE). Le dossier `references/BitCraftPublic/` garde sa propre licence (Apache 2.0).
