# Q-015 — Gérer les fenêtres internes du jeu (déplaçables, superposées)
- **Statut** : En partie résolue → D-052 (détecter avant toute action ; détection à coder)
- **Priorité** : Moyenne (Haute à partir de v0.4, quand le bot agira)
- **Créée le** : 2026-11-16
## Question
Les fenêtres du jeu (inventaire, Banque, carte…) sont déplaçables, gardent leur position et peuvent se superposer (voir [interface-du-jeu.md](../technique/interface-du-jeu.md)). Comment le bot les trouve, les utilise et les ferme sans erreur ?
## Pistes / options
- Repérer chaque fenêtre par son **bouton croix** (carré bleu et blanc) et son **titre** (OCR : « INVENTORY », « BANK »…).
- Règle simple : avant toute action dans le monde, **fermer toutes les fenêtres** ouvertes (clic sur chaque croix), puis ouvrir seulement celle dont on a besoin.
- Option : demander à Julien de ranger les fenêtres à des positions fixes, et le bot vérifie qu'elles y sont.
## Ce qui bloque
Rien pour v0.3 (le bot n'agit pas). Captures utiles plus tard : plusieurs fenêtres ouvertes en même temps, dont une superposée.
