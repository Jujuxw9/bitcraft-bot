# Session 04 — 2026-11-16 — Analyse du lot 1 de captures

- **Agent** : Claude Code (Opus 5.5)
- **Durée / poste** : PC perso de Julien

## Objectif
Trier, renommer et analyser les 80 captures déposées par Julien dans `images/analyse/`.

## Réalisé
- 80 captures renommées (`AAAAMMJJ_analyse_sujet_NNN.png`) et indexées dans `images/README.md`. Poids total : 116 Mo.
- Analyse : [analyse-captures-2026-11-16.md](../technique/analyse-captures-2026-11-16.md).
- Constats majeurs : contour vert au survol de tout objet interactif ; clic droit = cibler + aller + interagir ; ZQSD = caméra, W = recentrer (pas d'incohérence) ; clic sur la minicarte activé ; Banque à Snowrest ; horloge = heure du PC.
- Clavier AZERTY noté dans `materiel.md`.

## Décisions prises (liens D-NNN)
- [D-024](../decisions/D-024-commandes-du-bot.md) **proposée** (tout à la souris, réglages du jeu inchangés).
- D-016 et D-018 complétées (Banque confirmée, correction de l'horloge).

## Questions ouvertes créées (liens Q-NNN)
Aucune. Q-001, Q-002, Q-011 complétées.

## Points à signaler
- Le build affiché est `Early-Access-2-082daa0e` alors que le menu annonce la 1.0. **À vérifier** avec Julien.
- Le repo est public. Les captures montrent des pseudos de joueurs et le chat de région.
- 116 Mo d'images dans l'historique Git, de façon définitive.

## Prochaines étapes
- Julien : valider D-024, faire le lot 2 de captures (M1 à M9).
- Claude : mesurer le contour vert sur les captures, préparer la v0.1.
