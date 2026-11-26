# T-15 (11:00) — Contrôle complet du dépôt

Règles communes : `nuit-2026-11-27-regles.md`. Lis tout le bilan : tu contrôles le travail de la nuit et le reste du dépôt.

## À faire
1. **Build et tests** : `dotnet build` (Debug et Release) 0 avertissement ; `dotnet test` ; noter le nombre de tests.
2. **Mise en forme** : `dotnet format --verify-no-changes` ; il restait ~94 remarques d'espaces anciennes (session 29) : les corriger (`dotnet format whitespace`), vérifier que build et tests restent verts.
3. **Code** : code mort laissé par la nuit (recettes retirées, Tab de la reprise, anciens crans de zoom…), `using` inutiles, fichiers vides, commentaires faux (versions, comportements), TODO oubliés ; classes de plus de ~600 lignes à signaler (ne pas tout refaire). Pas de changement de comportement.
4. **Sécurité du dépôt public** : aucune IP, aucun domaine privé, aucun mot de passe, aucun jeton ni e-mail dans les fichiers suivis (`git grep` sur des motifs : `@gmail`, `token`, `password`, adresses IP) ; `.gitignore` couvre `artifacts/`, `carte/`, journaux, `config.local.json`, archives du chat.
5. **Tests manquants** : les fonctions de la nuit sans test (déplacement, mémoire 72 h, reprise) : en ajouter quelques-uns, simples.
6. **Documentation technique** : `memory/technique/architecture.md` et `architecture-suite.md` décrivent bien le code actuel (onglets, déplacement, carte) ; `tests/README.md` ; `README.md` racine à jour (versions, fonctions).
7. Si du code a changé : version v0.4.33 (numéro libre suivant), publication, essai sans le jeu. Sinon pas de version.

## Livrable
- `memory/technique/controle-depot-2026-11-27.md` : ce qui a été vérifié, corrigé, ce qui reste (avec priorité). Bilan.

## Interdit
- Changer le comportement du bot. Toucher `website-recipes/`. Réécrire l'historique Git.
