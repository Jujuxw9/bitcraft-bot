# Session 29 — 2026-11-26 — Reprise, méthode du site des recettes

- **Agent** : Claude Code (Opus 5.5)
- **Durée / poste** : 00:30 → 00:40, PC de Julien

## Objectif
Reprendre après la passation 28 et répondre à Julien sur la méthode du site des recettes.

## Réalisé
- Passation 28 suivie : dépôt à jour ; à 00:32, aucune tâche planifiée n'avait encore tourné (T-01 à 03:30). Pas de journal v0.4.15 (jamais lancée).
- Incohérence vue : T-04 sans modèle fixé (les autres en Opus 5.5). Julien veut Opus 5.5 : à régler par lui dans l'app.
- Méthode du site des recettes proposée : `echanges/2026-11-26-methode-site-recettes.md`.

### Suite (26/11, 18:50)
- Bilan des tâches T-01 à T-03 : les trois versions publiées (v0.4.16 à v0.4.18), tests 594 → 625, mémoire à jour (D-068 à D-070, journaux, état, architecture-suite), .exe archivés dans `artifacts/old`. Aucun écart grave au prompt. T-04 : rapport écrit, rien modifié (ligne de `etat.md` remplie ici).
- Rapport T-04 : corrections sûres appliquées (index racine D-070 et architecture-suite, résumé global raccourci, note dans `tests/README.md`, couverture de `architecture-serveur.md`). À trancher par Julien : T-04 dans D-067, Q-002/Q-003/Q-008/Q-014, exemption de longueur des archives, tri des vieilles questions.
- Julien : le site des recettes est pour lui, ses amis et sa guilde (pas public). Prompt de la session « site des recettes » écrit : `echanges/2026-11-26-prompt-site-recettes.md` (dépôt DOGGO-ESPORT lu par Claude le 26/11, ce qu'il en retient est dans le prompt).

### Suite (26/11, 19:00 → 19:35) : essai de la v0.4.18, v0.4.19
- Essai de Julien (18:53) : démarrage en séquence OK (bot → écran → jeu par Steam). Mais carte, voyageurs, personnage, chat et activité vides.
- Cause (journal) : le serveur refuse les ressources autour du personnage (« Estimated cardinality … exceeds limit »). Les mêmes requêtes passaient à 13:27. Le repli du bot retirait d'abord tout le reste (sac, chat, voyageurs) : onglets vides.
- Sonde de lecture (hors dépôt, jeu fermé) : carré seul refusé ; morceau de carte + carré accepté ; OR refusé ; abonnement complet accepté.
- v0.4.19 (D-071) : requêtes par morceau, repli dans le bon ordre. Tests 626/626. Essai réel (jeu fermé, 19:27) : tout accepté, 1 848 ressources, 24 tâches, 6 morceaux de fond. .exe publié.
- Réponses de Julien : archive du chat, tout garder (complément de D-068) ; plus de 300 lignes permis mais place réduite ; caméra et exploration plus tard ; lien Dogo eSport annoncé mais pas reçu.
### Suite (26/11, vers 19:45) : réponses de Julien
- T-04 écrite noir sur blanc dans D-067 (quatre tâches). Mémoire automatique : exception des tâches planifiées notée ; Python disponible (`py -3`), note corrigée.
- D-072 : consignes dans un onglet Exploration (Q-003), sessions aléatoires de 30 min à 4 h (Q-008), le bot fuit toujours les ennemis (Q-014). Q-002 (trajets) pas comprise : à réexpliquer.
- Chat : tout archiver, messages privés compris ; le chatbot ne répond jamais aux messages privés (complément de D-068).
- Dépôt DOGGO-ESPORT lu (privé) : hébergement Docker sur Unraid, OVH + Nginx Proxy Manager, mot de passe des amis ; retenu dans le prompt du site, sans aucune adresse (le dépôt bitcraft-bot est public).

### Suite (26/11, vers 20:15) : essai de la v0.4.19, v0.4.20 préparée
- Essai de Julien : tout se remplit ; capacité 7 (Hewing Strikes) OK ; images du personnage trop petites ; carte trop pixelisée, couleurs des tiers peu claires ; chat pas en direct ; activité « Récolte » qui clignote.
- v0.4.20 (D-073) : fond de carte en hexagones, filtres simplifiés, images × 1,75, chat récent en bas et en direct, activité tenue 2,5 s. Tests 627/627, hexagones vérifiés hors ligne. Essai réel impossible (Julien joue). Publication en attente de la fermeture du bot.
- D-074 : déplacements par « Set destination », marche visée pour les petits trajets (Q-002). Messages privés : on reste en lecture au démarrage (D-068).

### Suite (26/11, vers 20:00 → 20:30) : v0.4.20 et v0.4.21, contrôles, Release
- v0.4.20 fabriquée (bot fermé). Journal v0.4.19 : aucune erreur, Hewing Strikes lancée 10 fois.
- v0.4.21 (D-075) : zone lue et carte à 25 cases, boutons de tier T1 à T5. Tests 628/628. Essai serveur pas fait (jeu ouvert).
- Lint : compilation sans avertissement ; `dotnet format` ne signale que la mise en page (94 remarques d'espaces, anciennes, non corrigées pour ne pas toucher tout le code). Contrôle de la mémoire : 0 erreur, numéros D-001 à D-075 complets et indexés.
- Release GitHub v0.4.21 à la demande de Julien (exception à D-051, notée dans CLAUDE.md).
- Demande notée pour plus tard : « Se déplacer ici » depuis la carte (`echanges/2026-11-26-demande-deplacement-carte.md`).

### Suite (26/11, vers 20:30 → 21:15) : retours sur la v0.4.21, v0.4.22 préparée
- Règle des Releases changée : une Release par palier (0.4 faite, prochaine en 0.5.x) : D-076, CLAUDE.md, mémoire automatique.
- Retours v0.4.21 : hexagones et chat en direct OK, fond à 25 cases OK ; barre de fabrication qui retombe ; alertes RAM (pic d'environ 900 Mo au démarrage, puis 520-600 Mo).
- v0.4.22 (D-078) : carte à 30 cases, un cercle, zoom, mémoire du terrain sur le disque et des ressources vues ; vraie progression des fabrications ; ceinture en vignettes ; seuils RAM relevés, pics brefs ignorés. Tests 632/632. Publication en attente (bot ouvert).
- Riverwood (`/c1`) : salon du module global, à suivre en direct par une liaison permanente, à essayer avec Julien (prochaine version).

## Décisions prises (liens D-NNN)
- [D-071](../decisions/D-071-v0419-requetes-par-morceau.md) : requêtes de zone par morceau de carte, repli corrigé.
- [D-072](../decisions/D-072-reponses-consignes-sessions-ennemis.md), [D-073](../decisions/D-073-v0420-retours-v0419.md), [D-074](../decisions/D-074-deplacements-set-destination.md), [D-075](../decisions/D-075-v0421-zone-25-release.md), [D-076](../decisions/D-076-releases-aux-grandes-etapes.md), [D-078](../decisions/D-078-v0422-carte-memoire-craft-ram.md).

## Questions ouvertes créées (liens Q-NNN)
Aucune. Question posée : le site est pour qui ?

## Prochaines étapes
- Après 14:00 : bilan des tâches T-01 à T-04 (`taches-planifiees/etat.md`), rapport de la mémoire.
- Julien teste la dernière version sortie.
- 27/11 ou après : caméra, exploration automatique, cadrage du site des recettes.
