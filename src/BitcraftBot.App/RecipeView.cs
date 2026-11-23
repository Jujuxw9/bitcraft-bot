using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BitcraftBot.GameData;

namespace BitcraftBot.App;

// Fiche d'un objet de l'onglet « Recettes », dessinée comme un logigramme (demande de Julien, v0.4.6) :
// ingrédients → atelier ou ressource (métier, outil, durée) → objet obtenu.
// Souris sur un objet : info-bulle avec l'essentiel pour l'obtenir. Clic : sa fiche.
public sealed class RecipeView
{
    public const int MaxRecipesShown = 12;
    public const int MaxUsesShown = 40;

    private static readonly Brush TitleBrush = Frozen(233, 223, 196);
    private static readonly Brush DimBrush = Frozen(147, 168, 191);
    private static readonly Brush AccentBrush = Frozen(210, 182, 93);
    private static readonly Brush CardBrush = Frozen(16, 35, 58);
    private static readonly Brush TileBrush = Frozen(21, 48, 77);
    private static readonly Brush TileBorder = Frozen(43, 76, 112);
    private static readonly Brush WantedBorder = Frozen(129, 201, 149);
    private static readonly Brush CraftBrush = Frozen(21, 86, 126);
    private static readonly Brush HarvestBrush = Frozen(30, 92, 58);
    private static readonly Brush WarnBrush = Frozen(253, 214, 99);

    private readonly RecipeBook _book;
    private readonly ItemIcons _icons;
    private readonly Action<string> _open;

    public RecipeView(RecipeBook book, ItemIcons icons, Action<string> open)
    {
        _book = book;
        _icons = icons;
        _open = open;
    }

    public UIElement Build(string key)
    {
        var panel = new StackPanel();
        if (!_book.Entries.TryGetValue(key, out var entry))
        {
            panel.Children.Add(Text("Objet inconnu.", 14, DimBrush));
            return panel;
        }
        panel.Children.Add(Header(entry));

        var producing = _book.Simplest(key);
        panel.Children.Add(Section(producing.Count switch
        {
            0 => "COMMENT L'OBTENIR",
            1 => "COMMENT L'OBTENIR (1 façon)",
            _ => $"COMMENT L'OBTENIR ({producing.Count} façons, la plus simple en premier)",
        }));
        if (producing.Count == 0)
        {
            panel.Children.Add(Text("Aucune recette ni récolte connue : objet obtenu autrement (butin, coffre, quête, commerce…).", 13, DimBrush));
        }
        var number = 1;
        foreach (var recipe in producing.Take(MaxRecipesShown))
        {
            panel.Children.Add(FlowCard(recipe, key, number++));
        }
        if (producing.Count > MaxRecipesShown)
        {
            panel.Children.Add(Text($"… et {producing.Count - MaxRecipesShown} autres façons.", 13, DimBrush));
        }

        // Ce qu'on fabrique avec : une tuile par objet obtenu (sans doublon).
        var uses = _book.Using(key).SelectMany(r => r.Outputs).Select(o => o.Key).Where(k => k != key).Distinct().ToList();
        panel.Children.Add(Section(uses.Count == 0 ? "SERT À FABRIQUER" : $"SERT À FABRIQUER ({uses.Count} objet{(uses.Count > 1 ? "s" : "")})"));
        if (uses.Count == 0)
        {
            panel.Children.Add(Text("N'entre dans aucune recette connue.", 13, DimBrush));
        }
        var wrap = new WrapPanel();
        foreach (var use in uses.Take(MaxUsesShown))
        {
            wrap.Children.Add(Tile(new EntryAmount(use, 0), wanted: false, small: true));
        }
        panel.Children.Add(wrap);
        if (uses.Count > MaxUsesShown)
        {
            panel.Children.Add(Text($"… et {uses.Count - MaxUsesShown} autres.", 13, DimBrush));
        }
        return panel;
    }

    // En-tête : grande image, nom, palier, catégorie, description.
    private UIElement Header(GameEntry entry)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        var icon = Icon(entry.Key, 72);
        icon.Margin = new Thickness(0, 0, 12, 0);
        grid.Children.Add(icon);
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(Text(entry.Name, 22, TitleBrush, FontWeights.Bold));
        var chips = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        chips.Children.Add(Chip(entry.IsCargo ? "Cargaison" : "Objet", TileBrush));
        if (entry.Tier >= 0) chips.Children.Add(Chip($"Palier {entry.Tier}", TileBrush));
        if (entry.Tag.Length > 0) chips.Children.Add(Chip(entry.Tag, TileBrush));
        if (entry.Rarity.Length > 0) chips.Children.Add(Chip(entry.Rarity, TileBrush));
        text.Children.Add(chips);
        if (entry.Description.Length > 0)
        {
            var description = Text(entry.Description, 13, TitleBrush);
            description.FontStyle = FontStyles.Italic;
            description.Margin = new Thickness(0, 6, 0, 0);
            text.Children.Add(description);
        }
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    // Une façon d'obtenir l'objet : [ingrédients] → [atelier ou ressource] → [objets obtenus].
    private Border FlowCard(Recipe r, string wanted, int number)
    {
        var harvest = r.Kind == "Récolte";
        var card = new StackPanel();

        var title = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        title.Children.Add(Chip(number.ToString(System.Globalization.CultureInfo.InvariantCulture), AccentBrush, Brushes.Black));
        title.Children.Add(Chip(harvest ? "Récolte" : "Fabrication", harvest ? HarvestBrush : CraftBrush));
        var name = Text(r.Name, 14, AccentBrush, FontWeights.SemiBold);
        name.VerticalAlignment = VerticalAlignment.Center;
        title.Children.Add(name);
        card.Children.Add(title);

        var flow = new StackPanel { Orientation = Orientation.Horizontal };
        if (r.Inputs.Count > 0)
        {
            var inputs = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            foreach (var input in r.Inputs)
            {
                inputs.Children.Add(Tile(input, wanted: false, small: false));
            }
            flow.Children.Add(inputs);
            flow.Children.Add(Arrow());
        }
        flow.Children.Add(Station(r));
        flow.Children.Add(Arrow());
        var outputs = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        foreach (var output in r.Outputs)
        {
            outputs.Children.Add(Tile(output, wanted: output.Key == wanted, small: false));
        }
        flow.Children.Add(outputs);
        card.Children.Add(new ScrollViewer
        {
            Content = flow,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        });

        return new Border
        {
            Background = CardBrush,
            BorderBrush = TileBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 10),
            Margin = new Thickness(0, 5, 0, 5),
            Child = card,
        };
    }

    // Boîte du milieu : où (atelier ou ressource), avec quel métier, quel outil, combien de temps.
    private static Border Station(Recipe r)
    {
        var harvest = r.Kind == "Récolte";
        var box = new StackPanel();
        box.Children.Add(Text(harvest ? "RÉCOLTER SUR" : r.Place == "à la main" ? "FABRIQUER" : "ATELIER", 11, DimBrush, FontWeights.SemiBold));
        box.Children.Add(Text(r.Place, 14, TitleBrush, FontWeights.Bold));
        var skills = r.Skills.Count == 0 ? "aucun" : string.Join(", ", r.Skills.Select(s => $"{s.Name} {s.Level}"));
        box.Children.Add(Line("Métier", skills));
        var tools = r.Tools.Count == 0
            ? (r.HandsAllowed ? "à la main" : "aucun")
            : string.Join(", ", r.Tools.Select(t => $"{t.Name} {t.Level}")) + (r.HandsAllowed ? " (ou à la main)" : "");
        box.Children.Add(Line("Outil", tools));
        var duration = r.Actions > 0
            ? $"≈ {Duration(r.Actions * r.SecondsPerAction)} ({r.Actions} × {r.SecondsPerAction:0.##} s)"
            : $"{r.SecondsPerAction:0.##} s par coup";
        box.Children.Add(Line("Durée", duration));
        if (r.StaminaPerAction > 0)
        {
            box.Children.Add(Line("Endurance", $"{r.StaminaPerAction:0.##} par {(r.Actions > 0 ? "action" : "coup")}"));
        }
        if (r.NeedsKnowledge)
        {
            box.Children.Add(Text("⚠ connaissance à découvrir avant", 12, WarnBrush));
        }
        if (r.NeedsClaimTech)
        {
            box.Children.Add(Text("⚠ technologie de colonie à débloquer", 12, WarnBrush));
        }
        return new Border
        {
            Background = harvest ? HarvestBrush : CraftBrush,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 6, 10, 8),
            MinWidth = 190,
            MaxWidth = 300,
            VerticalAlignment = VerticalAlignment.Center,
            Child = box,
        };
    }

    // Tuile d'un objet : image, quantité, nom. Souris : l'essentiel pour l'obtenir. Clic : sa fiche.
    private Border Tile(EntryAmount amount, bool wanted, bool small)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(Icon(amount.Key, small ? 26 : 36));
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
        var name = new TextBlock { FontSize = small ? 12 : 13, Foreground = TitleBrush, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = small ? 170 : 200 };
        if (amount.Quantity > 0)
        {
            name.Inlines.Add(new System.Windows.Documents.Run($"{amount.Quantity} × ") { Foreground = AccentBrush, FontWeight = FontWeights.Bold });
        }
        name.Inlines.Add(new System.Windows.Documents.Run(_book.NameOf(amount.Key)));
        text.Children.Add(name);
        var details = new List<string>();
        if (!small && _book.Entries.TryGetValue(amount.Key, out var entry) && entry.Tier >= 0)
        {
            details.Add($"palier {entry.Tier}");
        }
        if (amount.Probability is > 0 and < 1)
        {
            details.Add($"{amount.Probability:P0} de chance");
        }
        else if (amount.Probability > 1)
        {
            details.Add($"poids {amount.Probability:0.##}");
        }
        if (details.Count > 0)
        {
            text.Children.Add(Text(string.Join(" · ", details), 11, DimBrush));
        }
        row.Children.Add(text);

        var tile = new Border
        {
            Background = TileBrush,
            BorderBrush = wanted ? WantedBorder : TileBorder,
            BorderThickness = new Thickness(wanted ? 2 : 1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5, 3, 8, 3),
            Margin = new Thickness(0, 2, 6, 2),
            Cursor = Cursors.Hand,
            Child = row,
            ToolTip = LazyToolTip(amount.Key),
        };
        ToolTipService.SetInitialShowDelay(tile, 200);
        ToolTipService.SetShowDuration(tile, 60_000);
        var key = amount.Key;
        tile.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            _open(key);
        };
        return tile;
    }

    // Info-bulle construite au premier survol seulement (une fiche peut avoir des dizaines de tuiles).
    private ToolTip LazyToolTip(string key)
    {
        var tip = new ToolTip { Padding = new Thickness(10, 8, 10, 8) };
        tip.Opened += (_, _) => tip.Content ??= ToolTipContent(key);
        return tip;
    }

    // L'essentiel pour obtenir un objet : image, palier, les façons les plus simples, une ligne chacune.
    public UIElement ToolTipContent(string key)
    {
        var panel = new StackPanel { MaxWidth = 460 };
        var head = new StackPanel { Orientation = Orientation.Horizontal };
        head.Children.Add(Icon(key, 56));
        var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        title.Children.Add(Text(_book.NameOf(key), 15, TitleBrush, FontWeights.Bold));
        if (_book.Entries.TryGetValue(key, out var entry))
        {
            title.Children.Add(Text($"{(entry.IsCargo ? "Cargaison" : "Objet")}{(entry.Tier >= 0 ? $" · palier {entry.Tier}" : "")}{(entry.Tag.Length > 0 ? " · " + entry.Tag : "")}", 12, DimBrush));
        }
        head.Children.Add(title);
        panel.Children.Add(head);
        if (entry is { Description.Length: > 0 })
        {
            var description = Text(entry.Description.Length > 180 ? entry.Description[..180] + "…" : entry.Description, 12, TitleBrush);
            description.FontStyle = FontStyles.Italic;
            description.Margin = new Thickness(0, 6, 0, 0);
            panel.Children.Add(description);
        }

        panel.Children.Add(Text("COMMENT L'OBTENIR", 11, DimBrush, FontWeights.SemiBold, top: 8));
        var ways = _book.Simplest(key);
        if (ways.Count == 0)
        {
            panel.Children.Add(Text("Pas de recette ni de récolte : butin, coffre, quête ou commerce.", 12, TitleBrush));
        }
        foreach (var way in ways.Take(3))
        {
            panel.Children.Add(Text("• " + _book.Summary(way), 12, way.Kind == "Récolte" ? WantedBorder : TitleBrush, top: 2));
        }
        if (ways.Count > 3)
        {
            panel.Children.Add(Text($"+ {ways.Count - 3} autre(s) façon(s)", 12, DimBrush, top: 2));
        }
        panel.Children.Add(Text("Clic : ouvrir sa fiche.", 11, AccentBrush, top: 8));
        return panel;
    }

    private Image Icon(string key, double size)
    {
        var image = new Image { Width = size, Height = size, VerticalAlignment = VerticalAlignment.Center };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        if (_book.Entries.TryGetValue(key, out var entry) && entry.Icon.Length > 0)
        {
            _ = SetIconAsync(image, entry.Icon);
        }
        return image;
    }

    private async Task SetIconAsync(Image image, string icon)
    {
        var source = await _icons.GetAsync(icon);
        if (source is not null)
        {
            image.Source = source;
        }
    }

    // Flèche du logigramme.
    private static UIElement Arrow() => new Path
    {
        Data = Geometry.Parse("M0,12 L34,12 M25,4 L34,12 L25,20"),
        Stroke = AccentBrush,
        StrokeThickness = 2.5,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Width = 36,
        Height = 24,
        Margin = new Thickness(6, 0, 10, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static Border Chip(string text, Brush background, Brush? foreground = null) => new()
    {
        Background = background,
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(7, 1, 7, 2),
        Margin = new Thickness(0, 0, 6, 0),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = foreground ?? TitleBrush },
    };

    private static TextBlock Line(string label, string value)
    {
        var line = new TextBlock { FontSize = 12, Foreground = TitleBrush, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
        line.Inlines.Add(new System.Windows.Documents.Run(label + " : ") { Foreground = DimBrush });
        line.Inlines.Add(new System.Windows.Documents.Run(value));
        return line;
    }

    private static TextBlock Section(string title) => Text(title, 12, DimBrush, FontWeights.SemiBold, top: 16);

    private static TextBlock Text(string text, double size, Brush color, FontWeight? weight = null, double top = 0) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = weight ?? FontWeights.Normal,
        Foreground = color,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, top, 0, 2),
    };

    public static string Duration(double seconds) =>
        seconds < 90 ? $"{seconds:0} s" : seconds < 5400 ? $"{seconds / 60:0.#} min" : $"{seconds / 3600:0.#} h";

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
