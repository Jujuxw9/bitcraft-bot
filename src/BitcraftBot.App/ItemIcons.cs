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

    // Une seule image décodée à la fois (v0.3.20) : le décodeur WebP de Windows bloque quand plusieurs fils
    // l'utilisent ensemble (mesuré : 20 images en 3,9 s en parallèle, 0,09 s une par une). Ces fils bloqués
    // privaient le reste du bot (capture du jeu, lecture du HUD) : l'image du jeu n'arrivait plus.
    private readonly SemaphoreSlim _decodes = new(1);
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

    // Chemins à essayer, dans l'ordre (v0.4.12) : le chemin du jeu, puis les dossiers où le CDN range réellement
    // certaines images (ex. « Resources/Wildflowers » du jeu est « GeneratedIcons/Other/Wildflowers » sur le CDN).
    public static IReadOnlyList<string> Candidates(string path)
    {
        var name = path[(path.LastIndexOf('/') + 1)..];
        return new[] { path, "GeneratedIcons/Other/" + name, "GeneratedIcons/Items/" + name, "GeneratedIcons/Cargo/" + name, "Items/" + name }
            .Distinct().ToList();
    }

    private static string FileOf(string path) => Path.Combine(Folder, path.Replace('/', Path.DirectorySeparatorChar) + ".webp");

    private async Task<ImageSource?> LoadAsync(string path)
    {
        string file;
        try
        {
            // Une image déjà sur le disque, sous l'un des chemins possibles ?
            file = Candidates(path).Select(FileOf).FirstOrDefault(File.Exists) ?? "";
            if (file.Length == 0)
            {
                await _downloads.WaitAsync();
                try
                {
                    foreach (var candidate in Candidates(path))
                    {
                        var target = FileOf(candidate);
                        try
                        {
                            var bytes = await Http.GetByteArrayAsync(CdnBase + Uri.EscapeDataString(candidate).Replace("%2F", "/") + ".webp");
                            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                            var temp = target + ".tmp";
                            await File.WriteAllBytesAsync(temp, bytes);
                            File.Move(temp, target, overwrite: true);
                            file = target;
                            break;
                        }
                        catch (HttpRequestException)
                        {
                            // Introuvable sous ce chemin : suivant.
                        }
                    }
                }
                finally
                {
                    _downloads.Release();
                }
                if (file.Length == 0)
                {
                    Interlocked.Increment(ref _failures);
                    return null;
                }
            }
            await _decodes.WaitAsync();
            try
            {
                return await Task.Run(() => Decode(file));
            }
            finally
            {
                _decodes.Release();
            }
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
