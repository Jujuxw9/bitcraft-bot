# Q-010 — Poids des .exe dans l'historique Git
- **Statut** : Résolue → D-040 (2026-11-18)
- **Priorité** : Basse
- **Créée le** : 2026-11-16
## Question
D-020 versionne le `.exe` validé (~60 Mo). Git conserve chaque version dans l'historique, même après suppression. Après 10 versions, le clone pèse ~600 Mo. Faut-il changer de méthode avant que ça gêne ?
## Pistes / options
- Garder D-020 tel quel (simple ; acceptable pour quelques versions).
- Git LFS pour `executable/*.exe` : le repo reste léger. Quota gratuit GitHub limité (**À vérifier** : quota 2026).
- Releases GitHub seules + lien « dernière version » dans le README.
## Ce qui bloque
Rien tant qu'aucun `.exe` n'est validé. À trancher avant la v0.3 environ.
