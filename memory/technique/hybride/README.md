# Voie hybride — index et plan

Principe (D-039) : le bot **lit** l'état du jeu sur le serveur, **agit** au clavier et à la souris.

- [constats.md](constats.md) — ce qui est vérifié sur la connexion, le jeton, les tables
- [architecture-serveur.md](architecture-serveur.md) — brique Server, versions v0.3.5 à v0.3.11 (H2)

## Schéma

```
  Serveur officiel (SpacetimeDB)
        │  abonnement en lecture seule (WebSocket chiffré, jeton de Julien)
        ▼
  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
  │   SERVER     │ ──► │   DÉCISION   │ ──► │    INPUT     │ ──► jeu BitCraft (vrai client)
  │ position,    │     │ que faire ?  │     │ souris,      │
  │ arbres, sac… │     │              │     │ clavier      │
  └──────────────┘     └──────────────┘     └──────────────┘
        ▲                      ▲
        │ contrôle croisé      │
  ┌──────────────┐             │
  │  PERCEPTION  │ ────────────┘  (vision : sécurité, vérification, voie B)
  └──────────────┘
```

## Livrables

| Livrable | Contenu | État |
|---|---|---|
| **H1 — Lecture serveur** | Onglet « Serveur » : connexion par code reçu par e-mail, jeton chiffré, trouver la région, afficher en direct position, vie, endurance, faim, arbres proches (type, âge, distance). Aucune action. | **Fait** (v0.3.9, 2026-11-18) : lecture **vérifiée en réel** ; connexion refusée si le jeu est lancé (sinon le jeu est éjecté) |
| H2 — Contrôle croisé | Comparer position serveur et position lue à l'écran ; carte des arbres autour du joueur | **v0.3.12** (2026-11-18) : essai v0.3.11 OK ; onglet Carte, familles, cible stable ; essai à faire |
| H3 — Aller à un arbre | Choisir le meilleur arbre (données serveur), y aller à la souris, suivre la position serveur | Plus tard |
| H4 — Boucle bois → Banque | MVP (D-002) en hybride | Plus tard |
