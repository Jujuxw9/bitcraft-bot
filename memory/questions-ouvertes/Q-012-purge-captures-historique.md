# Q-012 — Effacer les captures déjà poussées de l'historique Git ?
- **Statut** : Résolue (2026-11-16, choix de Julien : on ne réécrit pas l'historique, voir `images/HISTORIQUE.md`)
- **Priorité** : Basse
- **Créée le** : 2026-11-16
## Question
D-026 arrête de pousser les captures. Mais les 96 captures déjà poussées (116 Mo) restent dans l'historique public. Faut-il les effacer ?
## Pistes / options
- Ne rien faire : 116 Mo restent dans le repo, sans autre conséquence.
- Réécrire l'historique (`git filter-repo`) puis forcer le push sur `main` : repo léger, captures retirées de GitHub. **Irréversible**, et tout autre clone du repo doit être recloné.
## Ce qui bloque
Choix de Julien. À faire de préférence tôt, tant que le repo n'a qu'un seul utilisateur.
