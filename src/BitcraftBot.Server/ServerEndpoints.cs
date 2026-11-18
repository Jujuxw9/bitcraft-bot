namespace BitcraftBot.Server;

// Adresses du serveur officiel (vérifiées le 2026-11-18, voir memory/technique/hybride/constats.md).
public static class ServerEndpoints
{
    // Service de connexion de BitCraft : envoi du code par e-mail, puis échange du code contre un jeton.
    public const string AuthBase = "https://api.bitcraftonline.com/authentication";

    // Hôte SpacetimeDB du jeu (le nom date de l'accès anticipé mais sert toujours en 1.0).
    public const string Host = "bitcraft-early-access.spacetimedb.com";

    // Sous-protocole texte (JSON) de SpacetimeDB.
    public const string JsonProtocol = "v1.json.spacetimedb";

    // Nom générique : aucune donnée personnelle dans l'en-tête.
    public const string UserAgent = "BitcraftBot-lecture";

    // Module d'une région du monde 1.0 : « R18 » dans le jeu → « idc-18 » (vérifié le 2026-11-18 ;
    // les modules « bitcraft-live-N » sont un ancien monde, où le personnage n'existe pas).
    public static string RegionModule(int region) => $"idc-{region}";

    public static Uri SubscribeUri(string host, string module) => new($"wss://{host}/v1/database/{module}/subscribe");

    public static Uri SchemaUri(string host, string module) => new($"https://{host}/v1/database/{module}/schema?version=9");
}
