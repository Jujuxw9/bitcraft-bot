using System.Windows;
using System.Windows.Controls;
using BitcraftBot.Perception.Hud;

namespace BitcraftBot.App;

// Mise en page adaptative (v0.3.18, demande de Julien) : la fenêtre se réorganise selon sa largeur.
public partial class MainWindow
{
    private const double WideWidth = 1500;
    private const double MediumWidth = 1050;

    private void InitLayout()
    {
        SizeChanged += (_, _) => ApplyLayout(ActualWidth);
        ApplyLayout(ActualWidth);
        Application.Current.SessionEnding += (_, _) => CloseServerReaderNow();
    }

    // Colonnes selon la largeur : 4 / 2 / 1 cartes au tableau de bord ; panneaux côte à côte ou empilés.
    private void ApplyLayout(double width)
    {
        if (width <= 0)
        {
            return;
        }
        DashboardGrid.Columns = LayoutRules.DashboardColumns(width);
        var narrow = width < MediumWidth;

        // Onglet Bot : commandes à droite, ou sous l'image si la fenêtre est étroite.
        SideBySide(BotSideColumn, BotSide, BotMain, !narrow, 300);
        // Étroit : l'onglet défile, avec une hauteur minimale pour que rien ne se chevauche.
        BotScroll.VerticalScrollBarVisibility = narrow ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
        BotGrid.MinHeight = narrow ? 1050 : 0;
        // Personnage : « En direct » et « Bonus » côte à côte, ou l'un sous l'autre.
        Stack(PersoGrid, PersoLive, PersoBuffs, PersoBag, narrow, 360);
        // Carte : carte et tableau côte à côte, ou l'un sous l'autre.
        Stack2(MapGrid, MapCard, MapList, narrow, GridLength.Auto);
        // Paramètres : journal à droite, ou sous les réglages.
        Stack2(SettingsGrid, SettingsLeft, SettingsLog, narrow, new GridLength(1, GridUnitType.Star));
        SettingsCards.Columns = width >= WideWidth ? 2 : 1;
    }

    private void SideBySide(ColumnDefinition sideColumn, FrameworkElement side, FrameworkElement main, bool beside, double sideWidth)
    {
        sideColumn.Width = beside ? new GridLength(sideWidth) : new GridLength(0);
        Grid.SetColumn(side, beside ? 1 : 0);
        Grid.SetRow(side, beside ? 0 : 1);
        Grid.SetColumnSpan(main, beside ? 1 : 2);
        side.MaxHeight = beside ? double.PositiveInfinity : 260;
    }

    // Grille à 2 colonnes et 2 lignes : first | second en haut, third en bas ; empilé si étroit.
    private static void Stack(Grid grid, FrameworkElement first, FrameworkElement second, FrameworkElement third, bool narrow, double firstWidth)
    {
        grid.ColumnDefinitions[0].Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(firstWidth);
        grid.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetColumn(second, narrow ? 0 : 1);
        Grid.SetColumnSpan(first, narrow ? 2 : 1);
        Grid.SetColumnSpan(second, narrow ? 2 : 1);
        // Étroit : les trois cartes l'une sous l'autre (« En direct », « Bonus », « Sac ») dans une pile qui défile.
        if (grid.RowDefinitions.Count < 3)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
        }
        grid.RowDefinitions[1].Height = narrow ? GridLength.Auto : new GridLength(1, GridUnitType.Star);
        grid.RowDefinitions[2].Height = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetRow(second, narrow ? 1 : 0);
        Grid.SetRow(third, narrow ? 2 : 1);
    }

    // Grille à 2 colonnes et 2 lignes : first | second, ou first au-dessus de second.
    private static void Stack2(Grid grid, FrameworkElement first, FrameworkElement second, bool narrow, GridLength firstWidth)
    {
        grid.ColumnDefinitions[1].Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        grid.ColumnDefinitions[0].Width = narrow ? new GridLength(1, GridUnitType.Star) : firstWidth;
        grid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
        grid.RowDefinitions[1].Height = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Grid.SetColumn(second, narrow ? 0 : 1);
        Grid.SetRow(second, narrow ? 1 : 0);
    }
}

// Règles de mise en page, testables sans fenêtre.
public static class LayoutRules
{
    public static int DashboardColumns(double width) => width >= 1500 ? 4 : width >= 760 ? 2 : 1;
}
