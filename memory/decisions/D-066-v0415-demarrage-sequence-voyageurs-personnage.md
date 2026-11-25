# D-066 — v0.4.15 : démarrage en séquence, voyageurs, personnage, overlay ; suite planifiée

- **Statut** : Validée
- **Date** : 2026-11-26
- **Validée par** : Julien (réponses du 26/11, session 28)
- **Liens** : D-039, D-064, D-065, [demandes](../echanges/2026-11-25-demandes-julien-session-28.md) section 5

## Décision
1. **Démarrage en séquence** au lancement du bot, sans bouton : (1) connexion au serveur ; (2) quand tout est reçu (personnage, inventaires, rien en attente), lecture de l'écran ; (3) quand la lecture est confirmée, **le bot lance le jeu** (par Steam). Le jeu ne doit pas être lancé avant le bot : s'il l'est, pas de connexion (sinon le jeu est éjecté) et un message le dit. Boutons restants : « Se déconnecter » et « Arrêter ». Messages d'erreur gardés (ex. module global refusé). Pas de reconnexion automatique (D-039 inchangé).
2. **Durée max** : inchangée (la lecture de l'écran s'arrête au bout de 60 min).
3. **Voyageurs** : moins de texte, portraits de tous les voyageurs, matières premières à récolter par tâche ; un produit intermédiaire en stock (sac, ceinture, banques) est déduit avant de descendre l'arbre.
4. **Personnage** : image de chaque objet, logo Sac / Ceinture / Banques, colonne « Tier », tri par défaut : sac et banques par quantité, ceinture par nom.
5. **Overlay** : en bas, centré entre les icônes de gauche (chat, amis…) et la barre d'action.
6. **Paramètres** : « Jeton enregistré » déplacé dans Compte ; rien d'autre.
7. **Carte** : zone lue passée de ~13 à ~20 cases (charge du PC surveillée, on augmentera petit à petit). Analyse du site bitjita.com/map (requêtes, données, fonctionnement) pour la v0.4.18.
8. **Suite** : v0.4.16 (archive du chat), v0.4.17 (capacités), v0.4.18 (carte) et l'exploration automatique (session à part) : Julien veut des **tâches planifiées** ; Claude doit lui expliquer comment faire (aucune lancée sans son accord).
