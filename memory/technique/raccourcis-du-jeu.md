# Raccourcis du jeu (touches de BitCraft)

Mis à jour le 2026-11-19. Sources : captures des réglages de touches (2026-11-16, voir `analyse-captures-2026-11-16.md` § 2), infos de Julien, info-bulles du jeu.

## Réglages de Julien

- Clavier **AZERTY**. Le jeu affiche les touches en AZERTY.
- Touches **par défaut** : le registre Windows du jeu (`HKCU\Software\ClockworkLabs\BitCraft`, valeurs `BindingOverrides_*`) ne contient **aucune** touche modifiée (vérifié le 2026-11-19, lecture seule).
- Le bot envoie les touches par **position physique** (scancode) : une touche « E » est la même en AZERTY et en QWERTY.
- Si Julien change ses touches, le bot doit être reconfiguré (D-024).

## Souris

| Action | Touche |
|---|---|
| Aller vers le curseur | Clic gauche |
| Cibler + aller + interagir (récolter, ouvrir la Banque) | Clic droit |
| Cibler + menu contextuel | Maj + clic droit |
| Déplacer la caméra | Clic molette |
| Aller à un point de la minicarte | Clic sur la minicarte |

## Clavier

| Action | Touche | Remarque |
|---|---|---|
| **Manger (« Quick Eat »)** | **Maj + E** | Mange le premier aliment de l'inventaire (info-bulle du jeu). Utilisé par le repas automatique (D-042). **À vérifier** : « premier » = premier aliment du sac ? |
| Cibler | A | |
| Changer de cible | Tab | |
| Tourner la caméra | Z Q S D et flèches | |
| Recentrer la caméra | W | |
| **Cacher les arbres proches** | X | **Interdite au bot** (D-024) |
| Escalade rapide | F | |
| Barre d'action, cases 1 à 10 | & é " ' ( - è _ ç à | Rangée des chiffres en AZERTY |
| Barre d'action précédente / suivante | Pg. Préc / Pg. Suiv | 4 barres |
| Inventaire | I | |
| Compendium | C | |
| Banque personnelle (Vault) | V | |
| Carte | **À vérifier** | Non lisible sur les captures (M9) |
| Chat | Entrée | Permise au bot **seulement pour répondre dans le chat** (D-054) |
| Se déplacer au clavier | Non assignées | Le bot se déplace à la souris |

## Touches du bot (hors du jeu)

| Touche | Effet | Condition |
|---|---|---|
| Num 5 | Arrêt d'urgence (D-022) | Seulement quand BitCraft est au premier plan (D-041) |
| Num 0 | Capture du jeu seul (dans `images/analyse/`) | Seulement quand le jeu est visible |

Le jeu n'utilise ni Num 0 ni Num 5 (**à vérifier** une fois dans les réglages de touches).
