# D-050 — Caméra : bouton du milieu tourne la vue, W revient en 3 s et remet le zoom

- **Statut** : Validée ; point 2 (3 s après W) remplacé par D-053 (1 s, 2026-11-23)
- **Date** : 2026-11-22
- **Validée par** : Julien (réponses à Q-024)
- **Liens** : Q-024, D-049, R1 (v0.4.0), v0.4.1

## Réponses de Julien
1. « Oui, le bouton du milieu fait tourner la caméra. Il faut rester appuyé sur le bouton du milieu et déplacer la souris. »
2. « Après un appui sur W, la caméra revient en douceur. Le bot doit attendre 3 secondes. »
3. « W remet aussi le zoom que j'ai changé avec la molette. »

## Décision
- Bouton du milieu = caméra tournée (déjà le cas en v0.4.0).
- Après W : **3 s d'attente** avant de croire un clic ou de viser (v0.4.1, avant : 1 s).
- Molette = zoom changé ; W le remet.

## Constat qui change la suite (2026-11-22)
W ne remet **pas** la caméra dans la même direction du monde : les clics de l'essai R1 montrent des arbres au nord **et** au sud tous « devant » la caméra. W remet sans doute la caméra derrière le personnage (À vérifier, Q-025). La direction réelle de la caméra est visible sur la mini-carte (cône clair autour du personnage). Voir `technique/hybride/analyse-visee-2026-11-22.md`.
