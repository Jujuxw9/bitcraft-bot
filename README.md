# bitcraft-bot

Agent autonome pour BitCraft Online : perception écran, prise de décision et automatisation du farm de ressources, sous supervision humaine. Projet d'apprentissage.

> ⚠️ Les Community Guidelines de BitCraft interdisent toute automatisation. Projet expérimental, risque de sanction accepté par le porteur. Détail : [memory/00-contexte/regles-et-risques.md](memory/00-contexte/regles-et-risques.md).

## État

**v0.2.2** : la fenêtre du bot affiche le jeu en direct, **lit le HUD** (position, zone, vie, endurance, faim, cible), surveille sa consommation CPU/RAM, et propose un **banc d'essai des mouvements** souris / caméra (désactivé par défaut). Arrêt d'urgence `Num 5`, durée maximale, journaux. Dernière version validée : v0.1.0 (`executable/`).

Prochaine étape : v0.3, repérer les arbres sur l'image. Architecture : [memory/technique/architecture.md](memory/technique/architecture.md). Voir [memory/echanges/resume-global.md](memory/echanges/resume-global.md).

## Principe

Un `.exe` Windows lancé à côté du jeu. Sa fenêtre de supervision est sur le 2ᵉ écran. Le bot lit l'écran du jeu et pilote clavier/souris. L'humain supervise et peut tout arrêter (touche d'urgence, mouvement de souris, anomalie, durée max).

Stack : C# / .NET 10, WPF.

## Démarrage rapide

Prérequis : Windows, [SDK .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet build
```

```bash
dotnet test
```

```bash
dotnet run --project src/BitcraftBot.App
```

Publication d'un `.exe` unique autonome :

```bash
dotnet publish src/BitcraftBot.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Les versions publiées (tag `vX.Y.Z`) sont construites par `.github/workflows/release.yml` et disponibles dans les GitHub Releases.

## Organisation

| Dossier | Contenu |
|---|---|
| [memory/](memory/README.md) | Mémoire du projet : contexte, décisions, questions ouvertes, journal |
| `src/` | Code C# (App, Core, Perception, Input, GameData) |
| `tests/` | Tests xUnit |
| [images/](images/README.md) | Captures d'écran (référence UI, entraînement, analyse, tests) |
| [data/](data/README.md) | Données de jeu (schémas BitCraftPublic, valeurs) |
| [models/](models/README.md) | Modèles de vision `.onnx` |
| [tools/](tools/README.md) | Scripts utilitaires |
| [executable/](executable/README.md) | Dernier .exe validé (les anciens restent sur le PC) |

## Notes

- Modèles `.onnx` de plus de 50 Mo : à placer dans `models/large/` (ignoré par Git). Git ne sait pas ignorer selon la taille.
- `config.local.json` est ignoré par Git (réglages locaux).
- Agents IA : lire [CLAUDE.md](CLAUDE.md) avant toute action.

## Licence

MIT — voir [LICENSE](LICENSE).
