using System.IO;
using System.Text.Json;

namespace BitcraftBot.App;

// Réglages propres au PC (position de la fenêtre, sécurité, affichage), enregistrés dans config.local.json
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

    // Cadence de capture visée (images par seconde). 5 par défaut ; on pourra monter si besoin.
    public int TargetFps { get; set; } = 5;

    // Affiche les zones lues sur l'image du jeu.
    public bool ShowZones { get; set; } = true;

    // Fenêtre agrandie au démarrage, sur l'écran où elle était (demande de Julien).
    public bool StartMaximized { get; set; } = true;

    // Arrêt sur anomalie (RAM du bot critique).
    public bool AnomalyStopEnabled { get; set; } = true;

    // Seuils de surveillance CPU / RAM (onglet Système).
    public BitcraftBot.Core.Monitoring.ResourceLimits Limits { get; set; } = new();

    // Onglet Serveur (H1) : région lue (R18 = module bitcraft-live-18) et adresse e-mail du compte, gardées sur ce PC seulement.
    public int ServerRegion { get; set; } = 18;
    public string ServerEmail { get; set; } = string.Empty;

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
