# Session 14 — 2026-11-19 — v0.3.19 : Num 5 supprimée, journal des tâches, moins de CPU

**Agent** : Claude Code

## Réalisé
Demandes de la passation (`echanges/2026-11-19-passation-session-14.md`), dans l'ordre :
1. **Num 5 supprimée** (D-047, remplace D-041 ; D-022 et D-045 mis à jour).
2. **Journal** : passages de niveau et début / fin de tâche, lus sur le serveur (`TaskJournal`). Une ligne par événement.
3. **Écran** : vie, endurance, faim et jauge de métier plus lues (moins d'OCR). Restent position, zone, région, cible.
4. **Optimisations** : instantané recalculé seulement si le serveur a changé, onglet affiché seulement, capture à 2 images/s quand l'image est cachée, « jeu lancé ? » toutes les 3 s, une seule recherche de la fenêtre du jeu, messages bruts du serveur échantillonnés.
- Version 0.3.19 dans le code. 262 tests OK. Rendu hors écran des 5 onglets et de l'overlay OK.
- Détail : `technique/hybride/architecture-serveur.md`, section 32. Essai : `echanges/2026-11-19-essai-v0319.md`.

## Écarts notés
- **Num 5** était la sécurité principale (D-004, D-022). Supprimée comme demandé ; remplaçants (bouton, souris, pause clavier) **confirmés par Julien**.
- **.exe** : d'abord pas fabriqué (malentendu sur « pas de Release »). Précision de Julien : toujours fabriquer le .exe en local dans `artifacts/release`, mais **pas de Release GitHub**. Fabriqué ensuite.
- **Pas d'essai réel** : Julien jouait (BitCraft au premier plan), et le journal des tâches demande la connexion au serveur (jeu fermé avant). Mesure CPU avant / après pas faite.
- « Coups » = nombre d'actions distinctes du serveur (`start_time`). À vérifier en réel contre le jeu.
- Fabrication : le nom de la recette n'est pas écrit (les données chargées ne relient pas `recipe_id` à un nom) ; les objets gagnés et utilisés le montrent.
- La jauge de métier n'est plus lue : D-044 citait « jauge » côté écran ; mis à jour.

## Décisions prises
- D-047 (plus de touche d'arrêt d'urgence).

## Prochaines étapes
- Essai de Julien : `echanges/2026-11-19-essai-v0319.md`.
- H3 (aller au meilleur arbre).
