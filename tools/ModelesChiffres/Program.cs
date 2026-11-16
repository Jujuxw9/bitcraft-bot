using System.Globalization;
using System.Text;
using BitcraftBot.Perception.Capture;
using BitcraftBot.Perception.Hud;
using Windows.Graphics.Imaging;
using Windows.Storage;

// Fabrique les modèles des caractères des coordonnées à partir des découpes étiquetées
// de images/reference-ui/coordonnees (nom du fichier = texte attendu, ex. N8945_E6704_xxx.png).
// 1. Contrôle « un contre tous » : chaque image est lue avec des modèles appris sur les autres.
// 2. Écrit src/BitcraftBot.Perception/Hud/GlyphTemplates.cs avec les modèles appris sur toutes les images.
// Usage : dotnet run --project tools/ModelesChiffres (depuis la racine du dépôt)

var root = FindRepoRoot();
var folder = Path.Combine(root, "images", "reference-ui", "coordonnees");
var samples = new List<(string File, string Expected, List<Glyph> Glyphs)>();
foreach (var file in Directory.GetFiles(folder, "*.png").Order())
{
    var name = Path.GetFileNameWithoutExtension(file);
    var parts = name.Split('_');
    var expected = $"{parts[0]},{parts[1]}";
    var frame = await LoadFrameAsync(file);
    var area = new ScreenRect(0, 0, frame.Width, frame.Height);
    List<Glyph>? found = null;
    foreach (var glyphs in GlyphReader.FindLines(frame, area))
    {
        if (args.Contains("--debug"))
        {
            Console.WriteLine($"  {name} : {glyphs.Count} car. en y={glyphs[0].Y} " + string.Join(" ", glyphs.Select(g => $"{g.X}:{g.Width}x{g.Height}{(g.IsComma ? "," : "")}")));
        }
        if (found is null && glyphs.Count == expected.Length && glyphs.Select(g => g.IsComma).SequenceEqual(expected.Select(c => c == ',')))
        {
            found = glyphs;
        }
    }
    if (found is null)
    {
        Console.WriteLine($"ÉCARTÉE (ligne non trouvée) : {name}");
        continue;
    }
    samples.Add((name, expected, found));
}
Console.WriteLine($"{samples.Count} images utilisables.");

// 1. Contrôle un contre tous.
var errors = 0;
var worst = 0.0;
foreach (var sample in samples)
{
    var templates = Learn(samples.Where(s => s.File != sample.File));
    var text = GlyphReader.Classify(sample.Glyphs, templates);
    foreach (var g in sample.Glyphs.Where(g => !g.IsComma))
    {
        worst = Math.Max(worst, GlyphReader.BestTemplate(g.Features, templates).Distance);
    }
    if (text != sample.Expected)
    {
        errors++;
        Console.WriteLine($"ERREUR : {sample.File} lu « {text ?? "refusé"} » : " + string.Join(" ", sample.Glyphs.Where(g => !g.IsComma).Select(g => { var (c, d) = GlyphReader.BestTemplate(g.Features, templates); return $"{c}={d:0.000}"; })));
    }
}
Console.WriteLine($"Un contre tous : {samples.Count - errors}/{samples.Count} justes, écart le plus grand {worst:0.000} (seuil {GlyphReader.MaxDistance}).");

// Marge entre caractères : écart minimal entre deux modèles différents.
var all = Learn(samples);
var minGap = double.MaxValue;
var pair = "";
foreach (var (a, ta) in all)
{
    foreach (var (b, tb) in all)
    {
        if (a < b && GlyphReader.Distance(ta, tb) < minGap)
        {
            minGap = GlyphReader.Distance(ta, tb);
            pair = $"{a}/{b}";
        }
    }
}
Console.WriteLine($"Caractères les plus proches : {pair}, écart {minGap:0.000}.");

// 2. Écriture du fichier de modèles.
var code = new StringBuilder();
code.AppendLine("namespace BitcraftBot.Perception.Hud;");
code.AppendLine();
code.AppendLine("// Modèles des caractères des coordonnées (N, E, 0 à 9), appris sur les découpes de images/reference-ui/coordonnees.");
code.AppendLine($"// Fichier généré par tools/ModelesChiffres le {DateTime.Now:yyyy-MM-dd}, à partir de {samples.Count} images. Ne pas modifier à la main.");
code.AppendLine("public static class GlyphTemplates");
code.AppendLine("{");
code.AppendLine("    public static IReadOnlyDictionary<char, double[]> Coordinates { get; } = new Dictionary<char, double[]>");
code.AppendLine("    {");
foreach (var (c, t) in all.OrderBy(p => p.Key))
{
    code.AppendLine($"        ['{c}'] = [{string.Join(", ", t.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)))}],");
}
code.AppendLine("    };");
code.AppendLine("}");
var output = Path.Combine(root, "src", "BitcraftBot.Perception", "Hud", "GlyphTemplates.cs");
File.WriteAllText(output, code.ToString(), new UTF8Encoding(true));
Console.WriteLine($"Modèles écrits : {output} ({all.Count} caractères : {string.Concat(all.Keys.Order())}).");
// 3. Icônes des métiers : découpes de images/reference-ui/jauge-metier (coin = (1480, 0) de l'écran),
//    nom = Metier_niveau_pourcentage_xxx.png (ex. Forestry_25_95_capture-bot-direct_001.png).
var iconSums = new Dictionary<string, (double[] Sum, int Count)>();
foreach (var file in Directory.GetFiles(Path.Combine(root, "images", "reference-ui", "jauge-metier"), "*.png").Order())
{
    var parts = Path.GetFileNameWithoutExtension(file).Split('_');
    var frame = await LoadFrameAsync(file);
    var features = ProfessionGaugeReader.IconFeatures(frame, new ScreenRect(38, 252, 54, 42));
    if (features is null)
    {
        Console.WriteLine($"ÉCARTÉE (icône non trouvée) : {Path.GetFileName(file)}");
        continue;
    }
    var entry = iconSums.TryGetValue(parts[0], out var e) ? e : (new double[features.Length], 0);
    for (var k = 0; k < features.Length; k++)
    {
        entry.Item1[k] += features[k];
    }
    iconSums[parts[0]] = (entry.Item1, entry.Item2 + 1);
}
var icons = new StringBuilder();
icons.AppendLine("namespace BitcraftBot.Perception.Hud;");
icons.AppendLine();
icons.AppendLine("// Modèles des icônes de métier de la jauge, appris sur les découpes de images/reference-ui/jauge-metier.");
icons.AppendLine($"// Fichier généré par tools/ModelesChiffres le {DateTime.Now:yyyy-MM-dd}. Ne pas modifier à la main.");
icons.AppendLine("public static class ProfessionIconTemplates");
icons.AppendLine("{");
icons.AppendLine("    public static IReadOnlyDictionary<string, double[]> Icons { get; } = new Dictionary<string, double[]>");
icons.AppendLine("    {");
foreach (var (name, (sum, count)) in iconSums.OrderBy(p => p.Key))
{
    icons.AppendLine($"        [\"{name}\"] = [{string.Join(", ", sum.Select(v => (v / count).ToString("0.###", CultureInfo.InvariantCulture)))}],");
}
icons.AppendLine("    };");
icons.AppendLine("}");
File.WriteAllText(Path.Combine(root, "src", "BitcraftBot.Perception", "Hud", "ProfessionIconTemplates.cs"), icons.ToString(), new UTF8Encoding(true));
Console.WriteLine($"Icônes de métier écrites : {string.Join(", ", iconSums.Select(p => $"{p.Key} ({p.Value.Count} images)"))}.");

return errors == 0 ? 0 : 1;

static Dictionary<char, double[]> Learn(IEnumerable<(string File, string Expected, List<Glyph> Glyphs)> samples)
{
    var sums = new Dictionary<char, (double[] Sum, int Count)>();
    foreach (var (_, expected, glyphs) in samples)
    {
        for (var i = 0; i < glyphs.Count; i++)
        {
            if (glyphs[i].IsComma)
            {
                continue;
            }
            var c = expected[i];
            if (!sums.TryGetValue(c, out var entry))
            {
                entry = (new double[glyphs[i].Features.Length], 0);
            }
            for (var k = 0; k < entry.Sum.Length; k++)
            {
                entry.Sum[k] += glyphs[i].Features[k];
            }
            sums[c] = (entry.Sum, entry.Count + 1);
        }
    }
    return sums.ToDictionary(p => p.Key, p => p.Value.Sum.Select(v => v / p.Value.Count).ToArray());
}

static async Task<Frame> LoadFrameAsync(string path)
{
    var file = await StorageFile.GetFileFromPathAsync(path);
    using var stream = await file.OpenReadAsync();
    var decoder = await BitmapDecoder.CreateAsync(stream);
    using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
    bitmap.CopyToBuffer(System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.AsBuffer(pixels));
    return new Frame
    {
        Pixels = pixels,
        Width = bitmap.PixelWidth,
        Height = bitmap.PixelHeight,
        CapturedAt = DateTime.Now,
        Source = new ScreenRect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
    };
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BitcraftBot.sln")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
}
