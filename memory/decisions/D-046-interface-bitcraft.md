# D-046 — Interface aux couleurs de BitCraft, peu de texte, adaptative

- **Statut** : Validée
- **Date** : 2026-11-19
- **Validée par** : Julien (« il y a bcp de texte dans la fenêtre du bot, simplifie tout […] supprime les explications/descriptions » ; « un nouveau thème […] inspire-toi du code couleur et de la DA de BitCraft. Je veux une interface responsive, propre, moderne »)
- **Liens** : D-043 (onglets par usage), D-005

## Décision
- **Thème BitCraft** (`src/BitcraftBot.App/Themes/BitCraft.xaml`), couleurs relevées sur une capture de la fenêtre « Bank » du jeu :
  - fond bleu nuit `#0B1828`, cartes `#15304D`, bandeau des onglets en dégradé `#225178` → `#2C3863` ;
  - **or** `#D2B65D` (titres des cartes, onglet choisi, cases cochées, bouton principal) ;
  - texte **parchemin** `#E9DFC4`, texte secondaire `#93A8BF` ;
  - boutons bleus `#15567E` comme ceux du jeu ; barres : vie verte `#11B64A`, endurance jaune, faim orange.
- **Peu de texte** : plus de phrases d'explication dans la fenêtre. Libellés courts, valeurs, pastilles ● / ◌ / ○. Les détails restent dans le journal.
- **Adaptative** : selon la largeur, le tableau de bord passe de 4 à 2 ou 1 colonne ; sous 1 050 px, les panneaux s'empilent (commandes sous l'image, journal sous les réglages) et l'onglet Bot défile.
- **Sécurité** déplacée dans **Paramètres** (avec Affichage : overlay, zones lues, images/s).

## Conséquences
- Toute nouvelle fenêtre ou carte utilise les styles du thème (`Card`, `CardTitle`, `MutedText`, `Label`, `Value`, `PrimaryButton`, `DangerButton`, `SmallButton`).
- D-043 reste valable (mêmes 5 onglets) ; Julien prévoit encore des retouches.
