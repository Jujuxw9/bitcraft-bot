# Session 11 — 2026-11-19 — Essai long, v0.3.13

- **Agent** : Claude Code
- **Durée / poste** : soirée, PC de Julien

## Objectif
Analyser l'essai long de Julien (v0.3.12) et corriger ce qu'il révèle.

## Réalisé
- Julien : cases par famille OK, cible à l'écran OK en v0.3.12.
- Essai de 42 min (T-20261119-01) : liaison tenue, mémoire du bot stable (≈ 480 Mo), 11 relectures de zone.
- Défauts : Num 0 et Num 5 déclenchées en tapant au pavé numérique dans le Bloc-notes (4 captures du Bloc-notes, un arrêt d'urgence) ; lecture de l'écran pendant que le Bloc-notes cachait le jeu (fausses alertes, image de diagnostic du Bloc-notes).
- v0.3.13 : visibilité du jeu vérifiée à 5 points ; lecture de l'écran en pause et Num 0 ignorée si le jeu est caché ; message du module global adouci (refus normal après 1 h sans jeu). 219 tests OK, exe lancé.
- Test réel « Bloc-notes par-dessus » bloqué par la sécurité de Claude Code (risque de fermer le Bloc-notes de Julien) : à vérifier par Julien.

## Décisions prises (liens D-NNN)
Aucune.

## Questions ouvertes créées (liens Q-NNN)
- Q-022 : Num 5 hors du jeu.

## Prochaines étapes
- Réponse de Julien à Q-022 ; essai de la v0.3.13.
- H3 : aller au meilleur arbre.

## Suite : v0.3.14 (demandes de Julien)
- Demandes : overlay sur l'écran du jeu, contenu de la banque, onglet Recettes, onglet Paramètres, Num 5 seulement en jeu. Triées : sécurité, rangement, overlay, banque, recettes.
- D-041 : Num 5 seulement si BitCraft est actif (Q-022 résolue).
- Banque : `inventory_state` et `bank_state` publiques (code serveur + vrai schéma) → lecture seule du sac, de la ceinture et des coffres de banque.
- Recettes : `crafting_recipe_desc` et `building_type_desc` ajoutées aux données téléchargées ; 8707 recettes.
- 229 tests OK ; exe lancé ; onglets vérifiés par capture ; overlay vérifié avec le jeu lancé (fenêtre affichée). Format réel des poches : à vérifier au prochain essai (extrait écrit dans le journal).
