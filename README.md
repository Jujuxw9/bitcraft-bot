# bitcraft-bot

Agent autonome pour BitCraft Online : lecture de l'état du jeu, prise de décision et automatisation du farm de ressources, sous supervision humaine. Projet d'apprentissage.

> ⚠️ Les Community Guidelines de BitCraft interdisent toute automatisation. Projet expérimental, risque de sanction accepté par le porteur. Détail : [memory/00-contexte/regles-et-risques.md](memory/00-contexte/regles-et-risques.md).

## État

**v0.3.15** :
- Voie hybride (D-039) : le bot lit le serveur officiel **en lecture seule**, avec le jeton du joueur (code reçu par e-mail, jeton chiffré). Il affiche la position, la vie, l'endurance, la faim et les ressources proches. Vérifié en réel.
- Lecture de l'écran (HUD) : position, zone, vie, endurance, faim, cible, jauge de métier.
- Données du jeu (BitCraft_GameData, jamais republiées), surveillance CPU/RAM, banc d'essai des mouvements (désactivé par défaut).
- H2 : **carte des arbres** autour du joueur, avec leur âge, leur santé, leur distance et leur direction.
- Onglets « Personnage » (sac et banques), « Recettes » (comment obtenir chaque objet), « Paramètres » (connexion, overlay, système).
- Overlay sur l'écran du jeu : état du bot et rappel des touches.
- Sécurité : arrêt d'urgence `Num 5` (seulement quand BitCraft est actif, D-041), durée maximale, journaux. Capture du jeu avec `Num 0`.
- Repas automatique (Maj + E) quand la faim passe sous 30 % ou que le bonus de nourriture est fini (D-042, à cocher dans Paramètres).
- Images des objets dans l'onglet Recettes.

Règle : connecter le bot **avant** de lancer le jeu. Si le jeu est déjà lancé, le bot refuse de se connecter, sinon le jeu serait éjecté.

Prochaine étape : vérifier H2 en jeu, puis H3 (aller au meilleur arbre). Voir [memory/echanges/resume-global.md](memory/echanges/resume-global.md) et l'[architecture](memory/technique/architecture.md).

## Principe

Un `.exe` Windows lancé à côté du jeu, avec sa fenêtre sur le 2ᵉ écran :
- le bot **lit** l'état du jeu sur le serveur, et l'écran en contrôle ;
- il **agit** uniquement au clavier et à la souris, sur le vrai jeu.

L'humain supervise et peut tout arrêter (touche d'urgence, mouvement de souris, anomalie, durée max).

Stack : C# / .NET 10, WPF.

## Lancer le bot

Dernière version : `artifacts\release\BitcraftBot-vX.Y.Z.exe` (le nom porte la version). Les anciennes versions sont dans `artifacts\old\vX.Y.Z\` (D-040, sur le PC seulement).

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
| `tests/` | Tests xUnit |
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
