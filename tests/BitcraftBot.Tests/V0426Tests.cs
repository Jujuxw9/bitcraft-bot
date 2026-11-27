using System.IO;
using BitcraftBot.GameData;
using Xunit;

namespace BitcraftBot.Tests;

// v0.4.26 (D-083) : onglet Recettes retiré ; ce qui sert ailleurs est gardé
// (livre des recettes, images, barre de fabrication, voyageurs, données du jeu chargées).
public class V0426Tests
{
    private static string App(string file) => Path.Combine(HudReaderReferenceTests.FindRepoRoot(), "src", "BitcraftBot.App", file);

    [Fact]
    public void RecipesTabIsGone()
    {
        var xaml = File.ReadAllText(App("MainWindow.xaml"));
        Assert.DoesNotContain("Header=\"RECETTES\"", xaml);
        Assert.DoesNotContain("RecipeSearchBox", xaml);
        Assert.False(File.Exists(App("RecipeView.cs")));
        Assert.False(File.Exists(App("MainWindow.Recipes.cs")));
    }

    [Fact]
    public void OtherTabsAreKept()
    {
        var xaml = File.ReadAllText(App("MainWindow.xaml"));
        foreach (var tab in new[] { "BotTabItem", "CharacterTabItem", "MapTabItem", "TravelerTabItem", "ChatTabItem", "SettingsTabItem" })
        {
            Assert.Contains($"x:Name=\"{tab}\"", xaml);
        }
    }

    [Fact]
    public void BookAndImagesStillLoaded()
    {
        var code = File.ReadAllText(App("MainWindow.ItemData.cs"));
        Assert.Contains("RecipeBook.Load", code);
        Assert.Contains("StartIconDownload", code);
        Assert.Contains("IconImage", code);
        Assert.Contains("LoadRecipesInBackground", File.ReadAllText(App("MainWindow.xaml.cs")));
    }

    [Fact]
    public void GameDataTablesStillDownloaded()
    {
        // Lues par le livre des recettes (barre de fabrication D-078, onglet Voyageurs) : à garder.
        Assert.Contains("crafting_recipe_desc", GameDataStore.Tables);
        Assert.Contains("building_type_desc", GameDataStore.Tables);
        Assert.Contains("item_conversion_recipe_desc", GameDataStore.Tables);
        Assert.Contains("traveler_task_desc", GameDataStore.Tables);
    }
}
