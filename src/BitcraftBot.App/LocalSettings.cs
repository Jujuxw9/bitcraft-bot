using System.IO;
using System.Text.Json;

namespace BitcraftBot.App;

// Réglages propres au PC (position de la fenêtre, sécurité), enregistrés dans config.local.json
// à côté du .exe. Ce fichier n'est jamais versionné (.gitignore).
public sealed class LocalSettings
{
    public const string FileName = "config.local.json";

    // Taille de départ : moitié de 1920 × 1080 (D-031).
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = 960;
    public double WindowHeight { get; set; } = 540;

    public bool EmergencyKeyEnabled { get; set; } = true;
    public bool MaxDurationEnabled { get; set; } = true;
    public int MaxDurationMinutes { get; set; } = 15;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, FileName);

    public static LocalSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<LocalSettings>(File.ReadAllText(path)) ?? new LocalSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Fichier illisible : on repart des valeurs par défaut.
        }
        return new LocalSettings();
    }

    public void Save(string path)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Échec d'enregistrement : sans gravité, la fenêtre reprendra sa taille par défaut.
        }
    }
}
