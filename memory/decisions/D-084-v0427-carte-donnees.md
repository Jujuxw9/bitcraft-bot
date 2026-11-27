# D-084 — v0.4.27 : carte, données (zoom 200, mémoire des ressources 72 h, terrain gardé pas redemandé)

- **Statut** : Validée (tâche de nuit T-07, d'après les demandes de Julien du 26-27/11)
- **Date** : 2026-11-27
- **Validée par** : Julien (« la carte la plus complète possible », dézoomer à 200 cases, ressources gardées 72 h ; fiche `taches-planifiees/T-07-v0427-carte-donnees.md`, D-081)
- **Liens** : D-071 (requêtes par morceau), D-079 (zones découvertes gardées), D-081, [architecture-suite.md](../technique/architecture-suite.md) section 31

## Contexte
Julien veut dézoomer jusqu'à 200 cases et voir partout où il est passé. Il voyait des zones noires à 100 cases. Les ressources croisées doivent rester sur la carte quand il s'éloigne, même après avoir fermé le bot.

## Cause des zones noires (journal v0.4.24 de Julien, 27/11 00:14)
- Le personnage n'a découvert que **74 morceaux** dans cette région (monde de 400 morceaux de large) ; 51 étaient gardés. Les 23 autres étaient **plus loin que 130 cases** : jamais demandés (le rayon de recherche était trop court).
- Partout ailleurs, les morceaux **pas découverts** ne sont jamais lus (normal : le jeu ne les montre pas non plus). Ils étaient dessinés dans le bleu nuit du fond, presque noir : on croyait à un manque.
- Le lien zones découvertes ↔ morceaux a été vérifié dans le code du serveur (`exploration_chunks_state.rs` : indice = z × largeur du monde + x) : il est juste.

## Décision
1. **Zoom** : crans 10, 15, 20, 30, 45, 65, 100, 140, 200 cases. L'affichage rapide à 200 est fait par T-08.
2. **Morceaux découverts** cherchés jusqu'à **230 cases** (au lieu de 130), toujours 24 au plus par abonnement.
3. **Zones inconnues** (pas découvertes) en **gris-bleu discret** (#18222E), et le fond de la carte dans la même couleur, au lieu du bleu nuit presque noir.
4. **Mémoire des ressources sur le disque** (`carte/ressources/ressources.bin` à côté du .exe, format BCR1, ignoré par Git comme `carte/`) : chaque ressource avec la date où elle a été **vue pour la dernière fois**. Toutes les familles (bois, pierre, minerai, plantes, nourriture, sable, pêche, autres).
   - Dans la zone lue en direct, le serveur fait foi (coupée = retirée). Hors zone : gardée, dessinée pâle ; survol = « Nom (Tn) — vue il y a N h ».
   - **Nettoyage** au démarrage puis toutes les heures : retiré si pas revu depuis **72 h** ; une ligne de journal « Mémoire des ressources : N gardées, M retirées (plus de 72 h) ».
   - 200 000 ressources au plus (les plus anciennes partent d'abord). Lecture au démarrage en arrière-plan ; écriture groupée toutes les 2 min si quelque chose a changé, et à la fermeture.
5. **Contrôle des données** : ressource sans identifiant, sans position lisible ou hors du monde rejetée et comptée ; doublons comptés ; morceaux de terrain illisibles comptés (journal).
6. **Échanges allégés, sans changer le protocole** : un morceau de terrain gardé et reçu il y a moins de **24 h** n'est plus redemandé, sauf les **3 × 3** autour du personnage. Gain modeste (0 à 7 morceaux d'environ 18 Ko par abonnement). Mesure ajoutée au journal : taille de chaque envoi, abonnements par minute, total reçu.
7. **Pas fait (à vérifier en réel, avec Julien)** : lire une seule fois les parties fixes (colonies `claim_state` + `claim_local_state` : 1 280 lignes, banques) demanderait plusieurs abonnements séparés ou un désabonnement : changement de protocole, interdit sans essai réel. Mesure avant (v0.4.24) : envoi complet ≈ 1,36 Mo, dont ≈ 360 Ko de parties fixes, ≈ 170 Ko de terrain (9 morceaux), le reste = 2 747 ressources × 3 tables.
8. **Ennemis (plus tard)** : ils se brancheront à côté de `ResourceMemory`, avec la même règle « vu le … », mais **sans mémoire longue** (ils bougent) : seulement la lecture en direct, et une famille à part dans `MapCategories`.

## Conséquences
- Tests 662/662 (9 nouveaux dans `V0427Tests`).
