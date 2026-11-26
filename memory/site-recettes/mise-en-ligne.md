# Mise en ligne du site des recettes (étape 4)

Démarrée le 2026-11-26. Julien valide chaque étape avant la suivante. Adresses privées (IP, domaine) : **jamais ici** (dépôt public) ; elles sont dans `MISE-EN-LIGNE.txt` du dépôt DOGGO-ESPORT et dans `website-recipes/deploy.local.json` (ignoré par Git).

## Ce qui existe déjà (vérifié le 26/11)
- Partage `appdata` du serveur Unraid joignable depuis le PC ; Nginx Proxy Manager (NPM) installé ; port 8790 libre sur le serveur.
- Box : ports 80 et 443 déjà redirigés vers NPM pour DOGGO-ESPORT → **rien à faire sur la box** (à confirmer par le test final).

## Choix techniques
- Conteneur sur l'**image officielle `python:3.13-slim`** : le site n'utilise que Python de base, pas besoin d'image fabriquée par GitHub (plus simple que DOGGO-ESPORT).
- Code dans `appdata/riverwood-recipes`, monté sur `/app` ; commande `python3 -B /app/container.py` ; port 8790.
- Le conteneur : abandonne les droits root (99:100), fabrique les données s'il n'y en a pas, les remet à jour chaque nuit à 5 h si le jeu a changé (seule la dernière version des tables est gardée), se relance seul après un déploiement.
- Déploiement : `website-recipes/tools/deploy.py` (essai à blanc, sauvegarde de l'ancienne version, attente de la confirmation du serveur, retour arrière). **Accord de Julien avant chaque déploiement.**

## Étapes
| # | Qui | Étape | État |
|---|---|---|---|
| 1 | Claude | Préparer le site pour Unraid (lanceur, mise à jour quotidienne, script de déploiement) et le tester sur le PC | Fait (26/11) |
| 2 | Claude, accord de Julien | Copier le site dans `appdata/riverwood-recipes` (9 fichiers, 1 604 images, données) | Fait (26/11, accord de Julien, 51,5 Mo) |
| 3 | Julien | Unraid : créer le conteneur (valeurs données par Claude) | Fait (26/11 20:52) ; Claude a vérifié depuis le PC : version, page, données (533 Ko compressé), images, chemins interdits refusés |
| 4 | Julien + Claude | Test chez soi : `http://<serveur>:8790` | En cours |
| 5 | Julien | OVH : entrée DNS de type A, sous-domaine `bitcraft-riverwood`, même cible que les autres sous-domaines | En cours |
| 6 | Julien | NPM : nouveau Proxy Host vers le port 8790, certificat Let's Encrypt | — |
| 7 | Julien | Test depuis le téléphone en 4G (Wi-Fi coupé) | — |
| 8 | Julien | Envoyer l'adresse aux amis et à la guilde | — |

## Après la mise en ligne
- Phase « user-friendly » demandée par Julien (26/11) : petites améliorations graphiques et réglages, d'après ses retours et ceux de ses amis.
