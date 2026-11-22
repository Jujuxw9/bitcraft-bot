# D-053 — Après W, la caméra revient en 1 s (et pas 3 s)

- **Statut** : Validée
- **Date** : 2026-11-23
- **Validée par** : Julien (« C'est 1s pour w l'attente »)
- **Liens** : remplace le point 2 de D-050 ; Q-026 (réglage « caméra adoucie »)

## Contexte
D-050 (22/11) disait : après W, la caméra revient en douceur, attendre 3 s. Julien corrige après l'essai de la v0.4.2 : l'attente est de **1 s**.

## Décision
- Après W : **1 s d'attente** avant de croire un clic de Julien ou de viser (v0.4.3).
- Le reste de D-050 ne change pas (bouton du milieu = caméra tournée ; W remet le zoom).
- Le réglage du jeu « caméra adoucie » (`CameraPanSmoothing`) vaut toujours 1 le 23/11 : avec 0, W serait peut-être instantané (Q-026).

## Conséquences
- `CameraLearner` : 1 s (avant : 3 s). La méthode de visée (D-052) attend en plus que le cône de la mini-carte soit lu 2 fois de suite pareil.
