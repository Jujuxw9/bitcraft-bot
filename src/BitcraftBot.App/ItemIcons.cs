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

    // Images introuvables (v0.4.13) : notées dans « introuvables.txt » pour ne plus les redemander au site à chaque lancement.
    private static string MissingFile => Path.Combine(Folder, "introuvables.txt");

    private readonly Lazy<HashSet<string>> _missing = new(() =>
    {
        try
        {
            return File.Exists(MissingFile) ? File.ReadAllLines(MissingFile).Where(l => l.Length > 0).ToHashSet() : [];
        }
        catch (IOException)
        {
            return [];
        }
    });

    private void NoteMissing(string path)
    {
        lock (_missing.Value)
        {
            if (!_missing.Value.Add(path))
            {
                return;
            }
            try
            {
                Directory.CreateDirectory(Folder);
                File.AppendAllText(MissingFile, path + Environment.NewLine);
            }
            catch (IOException)
            {
                // Sans gravité : l'image sera simplement redemandée au prochain lancement.
            }
        }
    }

    // Marqueur du téléchargement complet (v0.4.13) : une fois fait, on ne le refait pas (supprimer le fichier pour le refaire).
    public static string DoneMarker => Path.Combine(Folder, "tout-telecharge.txt");

    public int MissingCount => _missing.Value.Count;

    // Garantit que l'image est sur le disque (essaie les chemins possibles). Renvoie son fichier, ou null si elle n'existe pas.
    private async Task<string?> EnsureFileAsync(string path)
    {
        var onDisk = Candidates(path).Select(FileOf).FirstOrDefault(File.Exists);
        if (onDisk is not null)
        {
            return onDisk;
        }
        if (_missing.Value.Contains(path))
        {
            return null;
        }
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
                    return target;
                }
                catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Forbidden)
                {
                    // Introuvable sous ce chemin : suivant.
                }
            }
        }
        finally
        {
            _downloads.Release();
        }
        NoteMissing(path);
        return null;
    }

    // Télécharge toutes les images de la liste (v0.4.13, demande de Julien : tout garder en local). Progression tous les 500.
    public async Task<(int Done, int Missing, int Errors)> DownloadAllAsync(IEnumerable<string> icons, Action<int, int> progress, CancellationToken token)
    {
        var paths = icons.Select(Normalize).Where(p => p is not null).Select(p => p!).Distinct().ToList();
        var done = 0;
        var missing = 0;
        var errors = 0;
        await Parallel.ForEachAsync(paths, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token }, async (path, _) =>
        {
            try
            {
                if (await EnsureFileAsync(path) is null)
                {
                    Interlocked.Increment(ref missing);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
            {
                Interlocked.Increment(ref errors); // réseau coupé ou lent : reprise au prochain essai
            }
            var n = Interlocked.Increment(ref done);
            if (n % 500 == 0)
            {
                progress(n, paths.Count);
            }
        });
        return (done, missing, errors);
    }

    private async Task<ImageSource?> LoadAsync(string path)
    {
        try
        {
            var file = await EnsureFileAsync(path);
            if (file is null)
            {
                Interlocked.Increment(ref _failures);
                return null;
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
