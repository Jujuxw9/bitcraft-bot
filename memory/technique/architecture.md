# Architecture logicielle cible

> Statut : squelette créé le 2026-11-16 (session 02). Aucune logique codée. Le reste est une cible, pas un état.

## Boucle de l'agent

```
Capture écran ──► Perception ──► Décision (Core) ──► Action (Input)
      ▲                                │                    │
      └──────────── jeu (fenêtre 1920×1080, D-006) ◄─────────┘
                               │
                 Fenêtre de supervision (App, 2ᵉ écran)
```

La sécurité (D-004) est au-dessus de la boucle : elle peut couper l'action à tout moment.

## Projets (.NET 10, `net10.0-windows`)

| Projet | Rôle | Dépend de |
|---|---|---|
| `BitcraftBot.App` | WPF : fenêtre de supervision (D-005), réglages, tâches | Core, Perception, Input, GameData |
| `BitcraftBot.Core` | Machine à états (N1, D-003), tâches, objectifs, journalisation, sécurité (D-004) | GameData |
| `BitcraftBot.Perception` | Capture (Windows.Graphics.Capture), OCR, motifs (D-007), détection (Q-001) | — |
| `BitcraftBot.Input` | Clavier/souris via `SendInput`, délais humanisés | — |
| `BitcraftBot.GameData` | Chargement des données de jeu `data/` (D-008) | — |
| `BitcraftBot.Tests` | Tests xUnit | Core, Perception, Input, GameData |

## Points à vérifier

- **À vérifier** : `Windows.Graphics.Capture` demande une cible avec version du SDK Windows (ex. `net10.0-windows10.0.19041.0`). La cible actuelle `net10.0-windows` devra sans doute évoluer dans `BitcraftBot.Perception`.
- **À vérifier** : moteur OCR (Windows.Media.Ocr, Tesseract…). Non choisi.
- **À vérifier** : support DirectML / Windows ML en 2026 (voir [materiel.md](../00-contexte/materiel.md)).
