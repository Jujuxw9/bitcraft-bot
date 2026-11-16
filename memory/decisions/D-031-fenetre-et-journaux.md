# D-031 — Fenêtre du bot et journaux de session

- **Statut** : Validée
- **Date** : 2026-11-16
- **Validée par** : Julien
- **Liens** : D-005, D-020

## Fenêtre
- Taille de départ : **960 × 540** (moitié de 1920 × 1080). Pas de plein écran.
- **Redimensionnable et déplaçable**. Position et taille **mémorisées** d'une session à l'autre (fichier `config.local.json`, non versionné).

## Journaux
Un fichier par **lancement du programme** (chaque Démarrer / Arrêter y est noté), classé par version puis par jour :

```
journaux/
└── v0.1.0/
    └── 2026-11-16/
        └── session_2026-11-16_14-30-05.log
```

- Dossier `journaux/` à côté du `.exe` (ignoré par Git).
- Première ligne : version exacte du bot (avec le commit), date, poste.
- Une ligne par événement : `2026-11-16 14:30:05.123 [INFO] [Sécurité] message`.
- Niveaux : `INFO`, `ALERTE`, `ERREUR`.
- Catégories : `Session`, `Sécurité`, `Capture`, `Perception`, `Action`, `Config`.

## Conséquences
- Pour analyser un problème, Julien envoie le fichier de la session (ou Claude le lit directement sur le PC).
