# Q-024 — Caméra : bouton du milieu, retour de W, molette

- **Statut** : Ouverte
- **Date** : 2026-11-19
- **Priorité** : Moyenne
- **Liens** : D-049, R1 (v0.4.0)

## Question
1. Dans BitCraft, le **bouton du milieu** de la souris (clic sur la molette) fait-il tourner la caméra ? Aujourd'hui le bot le suppose : après lui, les clics ne comptent plus jusqu'au prochain W.
2. Après **W**, la caméra revient-elle **d'un coup** ou **en douceur** (combien de temps) ? Le bot attend 1 s.
3. **Molette** = zoom : le bot suppose que W remet aussi le zoom (D-049). Confirmé ?

## Pourquoi
Si une de ces suppositions est fausse, le bot jette de bons clics, ou garde des clics faits avec une caméra tournée.
