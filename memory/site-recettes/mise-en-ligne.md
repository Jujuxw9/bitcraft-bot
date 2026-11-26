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

## Leçon (26/11)
- L'adresse publique notée dans `MISE-EN-LIGNE.txt` de DOGGO-ESPORT n'est **plus la bonne** : la box a changé d'adresse. Toujours prendre la cible actuelle des autres sous-domaines (doggo, minecraft) ou l'adresse publique vue depuis le PC, jamais celle du document.

## Étapes
| # | Qui | Étape | État |
|---|---|---|---|
| 1 | Claude | Préparer le site pour Unraid (lanceur, mise à jour quotidienne, script de déploiement) et le tester sur le PC | Fait (26/11) |
| 2 | Claude, accord de Julien | Copier le site dans `appdata/riverwood-recipes` (9 fichiers, 1 604 images, données) | Fait (26/11, accord de Julien, 51,5 Mo) |
| 3 | Julien | Unraid : créer le conteneur (valeurs données par Claude) | Fait (26/11 20:52) ; Claude a vérifié depuis le PC : version, page, données (533 Ko compressé), images, chemins interdits refusés |
| 4 | Julien + Claude | Test chez soi : `http://<serveur>:8790` | Fait (26/11, Julien : « fonctionne très bien ») |
| 5 | Julien | OVH : entrée DNS de type A, sous-domaine `bitcraft-riverwood`, même cible que les autres sous-domaines | Fait (26/11) : entrée corrigée par Julien vers l'adresse actuelle de la box ; vérifié par Claude chez OVH, Google et Cloudflare ; ports 80 et 443 de la box joignables |
| 6 | Julien | NPM : nouveau Proxy Host vers le port 8790, certificat Let's Encrypt | Fait (26/11) : certificat Let's Encrypt valable jusqu'au 24/02/2027 (renouvelé seul par NPM), site joignable en HTTPS (vérifié par Claude). **Force SSL et HSTS pas actifs** (le site répond aussi en http) : à cocher |
| 7 | Julien | Test depuis le téléphone en 4G (Wi-Fi coupé) | En cours |
| 8 | Julien | Envoyer l'adresse aux amis et à la guilde | — |

## DynHost OVH : à faire un jour par Julien — **ne plus lui reposer la question** (Julien, 26/11)
- Constat du 26/11 : l'adresse publique de la box a changé au moins une fois (ancienne adresse encore écrite dans `MISE-EN-LIGNE.txt` de DOGGO-ESPORT). Si elle rechange, **tous** les sous-domaines tombent d'un coup (site des recettes, doggo, minecraft, satisfactory) jusqu'à la mise à jour à la main des entrées A chez OVH.
- Solution : DynHost d'OVH (mise à jour automatique des entrées quand l'adresse change).
  1. OVH → Web Cloud → Noms de domaine → le domaine → onglet **DynHost** → « Gérer les accès » → créer un identifiant DynHost (suffixe, sous-domaine `*` pour tous, mot de passe).
  2. Dans le même onglet, « Ajouter un DynHost » pour chaque sous-domaine (les entrées A actuelles doivent d'abord être supprimées, OVH refuse un DynHost sur une entrée A existante — « À vérifier »).
  3. Un client qui prévient OVH à chaque changement d'adresse : soit la box Bouygues (rubrique DynDNS, si elle propose OVH — « À vérifier »), soit un petit conteneur Unraid (ex. ddclient ou « OVH DynHost updater »), réglé avec l'identifiant DynHost.
  4. Mettre aussi à jour l'adresse écrite dans `MISE-EN-LIGNE.txt` de DOGGO-ESPORT (avec l'accord de Julien : autre dépôt).
- En attendant : si un site ne répond plus de l'extérieur, comparer l'adresse publique (vue depuis le PC) avec la cible des entrées A chez OVH.

## Après la mise en ligne
- Phase « user-friendly » demandée par Julien (26/11) : petites améliorations graphiques et réglages, d'après ses retours et ceux de ses amis.
