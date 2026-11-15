# Résumé global (à jour au 2026-11-16)

1. Projet : bot autonome pour BitCraft Online (vision écran + clavier/souris), serveur officiel, compte principal, risque accepté (D-001).
2. MVP : récolter une ressource, déposer à la Banque quand l'inventaire est plein, en boucle (D-002).
3. Stack : C# / .NET 10, WPF, .exe unique en GitHub Releases (D-009, D-010).
4. Session 01 (2026-11-15, Claude chat) : cadrage, 18 décisions, 8 questions.
5. Session 02 (2026-11-16, Claude Code) : repo initialisé, `memory/` rempli, squelette .NET qui compile, workflow de release.
6. Bloquant principal : Q-001 (perception 3D). Puis Q-002 (navigation), Q-004 (valeurs de données), Q-006 (première ressource).
7. Prochaine étape 1 : Julien envoie des captures du jeu (HUD, inventaire, banque, carte, ressource ciblée, info-bulles) → `images/analyse/`.
8. Prochaine étape 2 : instruire Q-001 et Q-002 à partir de ces captures.
9. Ensuite seulement : v0.1 = capture de la fenêtre du jeu + affichage dans la fenêtre de supervision + arrêt d'urgence.
10. À trancher par Julien : Q-009 (langue des messages de commit).
