using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BitcraftBot.App;

// Images des objets (demande de Julien, 2026-11-19) : téléchargées une fois depuis le CDN public de brico.app
// (site d'aide de la communauté), puis gardées sur le PC dans donnees-jeu/icones/. Jamais republiées.
// Seul le chemin de l'image part dans la requête (aucune donnée de Julien).
public sealed class ItemIcons
{
    public const string CdnBase = "https://cdn.brico.app/sprites/";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private readonly ConcurrentDictionary<string, Task<ImageSource?>> _cache = new();
    private readonly SemaphoreSlim _downloads = new(4);
    private int _failures;

    public static string Folder => Path.Combine(GameDataLoader.Folder, "icones");

    // Nombre d'images introuvables (journal).
    public int Failures => _failures;

    // Chemin du jeu → chemin sur le CDN : sans la partie « [,3,10] » (variantes par quantité).
    public static string? Normalize(string icon)
    {
        var path = icon.Trim();
        var bracket = path.IndexOf('[');
        if (bracket >= 0)
        {
            path = path[..bracket];
        }
        path = path.Trim('/');
        return path.Length == 0 || path.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '/' or '_' or '-' or ' ' or '.'))
            ? null
            : path;
    }

    // Image décodée (petite taille), ou null si absente. Le résultat est gardé en mémoire.
    public Task<ImageSource?> GetAsync(string icon)
    {
        var path = Normalize(icon);
        return path is null ? Task.FromResult<ImageSource?>(null) : _cache.GetOrAdd(path, LoadAsync);
    }

    private async Task<ImageSource?> LoadAsync(string path)
    {
        var file = Path.Combine(Folder, path.Replace('/', Path.DirectorySeparatorChar) + ".webp");
        try
        {
            if (!File.Exists(file))
            {
                await _downloads.WaitAsync();
                try
                {
                    if (!File.Exists(file))
                    {
                        var bytes = await Http.GetByteArrayAsync(CdnBase + Uri.EscapeDataString(path).Replace("%2F", "/") + ".webp");
                        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                        var temp = file + ".tmp";
                        await File.WriteAllBytesAsync(temp, bytes);
                        File.Move(temp, file, overwrite: true);
                    }
                }
                finally
                {
                    _downloads.Release();
                }
            }
            return await Task.Run(() => Decode(file));
        }
        catch (Exception)
        {
            Interlocked.Increment(ref _failures);
            return null;
        }
    }

    // Décodage par Windows (décodeur WebP intégré), réduit à 96 px pour la mémoire.
    private static ImageSource? Decode(string file)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 96;
        image.UriSource = new Uri(file);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
