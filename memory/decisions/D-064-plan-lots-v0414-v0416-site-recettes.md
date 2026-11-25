# D-064 — Plan des lots v0.4.14 à v0.4.16 et site des recettes à part

- **Statut** : Validée
- **Date** : 2026-11-25
- **Validée par** : Julien (réponses du 2026-11-25 soir, session 28)
- **Liens** : D-039, D-063, [demandes du 25/11](../echanges/2026-11-25-demandes-julien-session-28.md)

## Contexte
Julien a donné une liste de demandes le 25/11 (fenêtre du bot, capacités, archive du chat, site des recettes). Plan en 4 lots proposé par Claude.

## Décision
1. **Ordre** : une version par lot, Julien teste entre chaque. v0.4.14 = lot 1 (fenêtre du bot), v0.4.15 = lot 2 (archive du chat), v0.4.16 = lot 3 (capacités).
2. **Overlay** : collé en bas au centre, par-dessus la barre d'action. **Pas de touche** pour le masquer : seulement l'interrupteur dans le bot.
3. **Capacités (lot 3)** : un interrupteur général activé/désactivé (comme le repas automatique ou la réponse du chat), plus un interrupteur par capacité. Seulement **pendant que Julien fait l'action lui-même** (la récolte automatique viendra plus tard). Réserve d'endurance : pas de capacité qui coûte de l'endurance sous **30 %**.
4. **Archive du chat (lot 2)** : base SQLite locale dans un dossier ignoré par Git, plus un tableau de consultation dans l'onglet CHAT (recherche par joueur et par canal).
5. **Site des recettes (lot 4)** : autre session, prompt préparé par Claude. Hébergé sur le serveur **Unraid** de Julien (petit PC chez lui, à côté de la box). Code dans le **même dépôt GitHub** `bitcraft-bot`, dans un dossier à part. Mode d'hébergement sur Unraid : à définir dans cette session (« À vérifier »).
6. **Voyageurs** : rien à changer, Julien doit relancer pour juger.

## Alternatives écartées
- Touche de l'overlay (F8) : refusée par Julien.
- Lots 1 et 2 dans une seule version : refusé, Julien veut tester chaque lot.

## Conséquences
- L'onglet RECETTES quittera le bot une fois le site en ligne.
