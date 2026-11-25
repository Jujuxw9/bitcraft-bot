using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BitcraftBot.App;

// Portraits des voyageurs (v0.4.15, demande de Julien) : le site d'images des objets n'a que celui de Svim.
// Les autres viennent du wiki communautaire du jeu (bitcraft.wiki.gg), téléchargés une seule fois dans
// donnees-jeu/icones/voyageurs/, jamais republiés. Seul le nom de l'image part dans la requête.
// Certaines images du wiki sont des cartes de présentation (texte à gauche, personnage à droite) : on n'en garde
// que la partie du personnage (Crop, en fraction de l'image).
public sealed class TravelerPortraits
{
    private const string WikiBase = "https://bitcraft.wiki.gg/images/thumb/";

    private sealed record Source(string File, int Width, Rect? Crop);

    private static readonly Dictionary<string, Source> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rumbagh"] = new("Rumbagh_Right_Concept_Art_Travelers.png", 120, null),
        ["Heimlich"] = new("Heimlich_Right_Concept_Art_Travelers.png", 120, null),
        ["The Twins"] = new("Varu_Laru_Concept_Art_Travelers.png", 120, null),
        ["Brico"] = new("Brico_The_Constructor.jpg", 320, new Rect(0.40, 0.18, 0.48, 0.80)),
        ["Alesi"] = new("Alesi_The_Keeper.jpg", 320, new Rect(0.40, 0.18, 0.48, 0.80)),
        ["Ramparte"] = new("Ramparte_The_Old_Guard.jpg", 320, new Rect(0.40, 0.18, 0.48, 0.80)),
    };

    private static readonly HttpClient Http = CreateClient();

    private readonly Dictionary<string, Task<ImageSource?>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public static string Folder => Path.Combine(ItemIcons.Folder, "voyageurs");

    // Le wiki demande un nom de programme dans les requêtes automatiques (nom générique, rien sur Julien).
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BitcraftBot/1.0");
        return client;
    }

    public static bool Has(string traveler) => Sources.ContainsKey(traveler);

    // Portrait (null si inconnu ou téléchargement impossible). À appeler depuis le fil de la fenêtre.
    public Task<ImageSource?> GetAsync(string traveler)
    {
        if (!Sources.TryGetValue(traveler, out var source))
        {
            return Task.FromResult<ImageSource?>(null);
        }
        if (!_cache.TryGetValue(traveler, out var task))
        {
            task = LoadAsync(source);
            _cache[traveler] = task;
        }
        return task;
    }

    private static async Task<ImageSource?> LoadAsync(Source source)
    {
        try
        {
            var file = Path.Combine(Folder, source.File);
            if (!File.Exists(file))
            {
                Directory.CreateDirectory(Folder);
                var url = $"{WikiBase}{source.File}/{source.Width}px-{source.File}";
                var bytes = await Http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(file, bytes);
            }
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(file);
            image.EndInit();
            image.Freeze();
            if (source.Crop is not { } crop)
            {
                return image;
            }
            var area = new Int32Rect((int)(crop.X * image.PixelWidth), (int)(crop.Y * image.PixelHeight),
                (int)(crop.Width * image.PixelWidth), (int)(crop.Height * image.PixelHeight));
            var cropped = new CroppedBitmap(image, area);
            cropped.Freeze();
            return cropped;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }
}
