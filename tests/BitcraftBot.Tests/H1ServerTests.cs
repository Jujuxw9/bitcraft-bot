using System.Text;
using BitcraftBot.Server;
using BitcraftBot.Server.Auth;
using BitcraftBot.Server.Protocol;
using BitcraftBot.Server.World;

namespace BitcraftBot.Tests;

// Livrable H1 : lecture du serveur (sans réseau : messages et schéma fabriqués à la main).
public class H1ServerTests
{
    // Petit schéma au format réel de SpacetimeDB (mêmes champs, peu de tables).
    internal const string SchemaJson = """
    {
      "typespace": { "types": [
        { "Product": { "elements": [
          { "name": { "some": "identity" }, "algebraic_type": { "Product": { "elements": [] } } },
          { "name": { "some": "entity_id" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "can_sign_in" }, "algebraic_type": { "Bool": [] } } ] } },
        { "Product": { "elements": [
          { "name": { "some": "entity_id" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "chunk_index" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "timestamp" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "location_x" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "location_z" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "destination_x" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "destination_z" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "dimension" }, "algebraic_type": { "U32": [] } },
          { "name": { "some": "is_walking" }, "algebraic_type": { "Bool": [] } } ] } },
        { "Product": { "elements": [
          { "name": { "some": "entity_id" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "chunk_index" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "x" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "z" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "dimension" }, "algebraic_type": { "U32": [] } } ] } },
        { "Product": { "elements": [
          { "name": { "some": "entity_id" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "resource_id" }, "algebraic_type": { "I32": [] } },
          { "name": { "some": "direction_index" }, "algebraic_type": { "I32": [] } } ] } },
        { "Product": { "elements": [
          { "name": { "some": "entity_id" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "last_stamina_decrease_timestamp" }, "algebraic_type": { "Product": { "elements": [] } } },
          { "name": { "some": "stamina" }, "algebraic_type": { "F32": [] } } ] } },
        { "Product": { "elements": [
          { "name": { "some": "entity_id" }, "algebraic_type": { "U64": [] } },
          { "name": { "some": "health" }, "algebraic_type": { "I32": [] } } ] } }
      ] },
      "tables": [
        { "name": "user_state", "product_type_ref": 0, "primary_key": [1], "table_access": { "Public": [] } },
        { "name": "mobile_entity_state", "product_type_ref": 1, "primary_key": [0], "table_access": { "Public": [] } },
        { "name": "location_state", "product_type_ref": 2, "primary_key": [0], "table_access": { "Public": [] } },
        { "name": "resource_state", "product_type_ref": 3, "primary_key": [0], "table_access": { "Public": [] } },
        { "name": "stamina_state", "product_type_ref": 4, "primary_key": [0], "table_access": { "Private": [] } },
        { "name": "resource_health_state", "product_type_ref": 5, "primary_key": [0], "table_access": { "Public": [] } }
      ]
    }
    """;

    internal const string Identity = "c200aaaabbbbccccddddeeeeffff00001111222233334444555566667777b435";

    internal static string Rows(params string[] rows) => "[" + string.Join(",", rows.Select(r => System.Text.Json.JsonSerializer.Serialize(r))) + "]";

    internal static string Table(string name, string inserts, string deletes = "[]") =>
        $$"""{ "table_id": 1, "table_name": "{{name}}", "num_rows": 1, "updates": [ { "deletes": {{deletes}}, "inserts": {{inserts}} } ] }""";

    internal static string Initial(params string[] tables) =>
        $$"""{ "InitialSubscription": { "database_update": { "tables": [ {{string.Join(",", tables)}} ] }, "request_id": 2, "total_host_execution_duration": { "__time_duration_micros__": 10 } } }""";

    private static Schema LoadSchema() => Schema.Parse(SchemaJson);

    internal static ResourceLabel? Describe(int id) => id switch
    {
        10 => new ResourceLabel("Mature Beech Tree", 1, "Tree", 12000),
        11 => new ResourceLabel("Young Beech Tree", 1, "Tree", 3000),
        _ => null,
    };

    [Fact]
    public void Schema_donne_les_colonnes_la_cle_et_la_visibilite()
    {
        var schema = LoadSchema();
        Assert.Equal(6, schema.TableCount);
        var mobile = schema.Find("mobile_entity_state")!;
        Assert.Equal("location_x", mobile.Columns[3]);
        Assert.Equal([0], mobile.PrimaryKey);
        Assert.True(mobile.IsPublic);
        Assert.False(schema.Find("stamina_state")!.IsPublic);
        Assert.Null(schema.Find("inconnue"));
    }

    [Fact]
    public void Ligne_en_tableau_est_nommee_avec_le_schema()
    {
        var row = Row.Parse("""[{"__identity__":"0xC200AB"}, 42, true]""", LoadSchema().Find("user_state"));
        Assert.Equal("c200ab", row.Identity("identity"));
        Assert.Equal(42UL, row.U64("entity_id"));
        Assert.True(row.Bool("can_sign_in"));
        Assert.Equal("42", row.Key);
    }

    [Theory]
    [InlineData("\"0xABCD\"", "abcd")]
    [InlineData("[\"0xabcd\"]", "abcd")]
    [InlineData("{\"__identity__\":\"abcd\"}", "abcd")]
    public void Identite_lue_sous_toutes_ses_formes(string json, string expected)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(expected, Row.ReadIdentity(doc.RootElement));
    }

    [Fact]
    public void Message_identite_ignore_le_jeton()
    {
        var json = """{ "IdentityToken": { "identity": { "__identity__": "0xC200FF" }, "token": "eyJhbGciOi.secret.sig", "connection_id": { "__connection_id__": 1 } } }""";
        var message = Assert.IsType<IdentityEvent>(ServerMessages.Parse(json, null));
        Assert.Equal("c200ff", message.HexIdentity);
        Assert.DoesNotContain("secret", message.ToString());
    }

    [Fact]
    public void Abonnement_puis_changements_mettent_a_jour_le_personnage()
    {
        var schema = LoadSchema();
        var cache = new TableCache();
        var initial = Initial(
            Table("user_state", Rows($$"""[{"__identity__":"0x{{Identity}}"}, 7, true]""")),
            Table("mobile_entity_state", Rows("[7, 0, 0, 12500, 30000, 12500, 30000, 0, false]")));
        var sub = Assert.IsType<SubscriptionEvent>(ServerMessages.Parse(initial, schema));
        cache.Reset(sub.Tables);

        var snapshot = SnapshotBuilder.Build(cache, "0x" + Identity.ToUpperInvariant(), Describe);
        Assert.Equal(7UL, snapshot.EntityId);
        Assert.Equal(new ServerPosition(12.5, 30), snapshot.Position);
        Assert.False(snapshot.Walking);

        // Le personnage marche : le serveur envoie l'ancienne ligne (suppression) et la nouvelle (ajout).
        var update = $$"""
        { "TransactionUpdate": { "status": { "Committed": { "tables": [
          {{Table("mobile_entity_state", Rows("[7, 0, 1, 14000, 30000, 20000, 30000, 0, true]"), Rows("[7, 0, 0, 12500, 30000, 12500, 30000, 0, false]"))}}
        ] } }, "timestamp": { "__timestamp_micros_since_unix_epoch__": 1 }, "reducer_call": { "reducer_name": "player_move", "reducer_id": 3, "args": "[]", "request_id": 0 } } }
        """;
        var tx = Assert.IsType<TransactionEvent>(ServerMessages.Parse(update, schema));
        Assert.Equal("player_move", tx.Reducer);
        foreach (var change in tx.Tables)
        {
            cache.Apply(change);
        }
        snapshot = SnapshotBuilder.Build(cache, Identity, Describe);
        Assert.Equal(new ServerPosition(14, 30), snapshot.Position);
        Assert.Equal(new ServerPosition(20, 30), snapshot.Destination);
        Assert.True(snapshot.Walking);
        Assert.Equal(1, cache.Counts()["mobile_entity_state"]);
    }

    [Fact]
    public void Ressources_proches_triees_et_nommees()
    {
        var schema = LoadSchema();
        var cache = new TableCache();
        var initial = Initial(
            Table("user_state", Rows($$"""[{"__identity__":"0x{{Identity}}"}, 7, true]""")),
            Table("mobile_entity_state", Rows("[7, 0, 0, 100000, 100000, 100000, 100000, 0, false]")),
            Table("location_state", Rows("[501, 0, 110, 100, 0]", "[502, 0, 102, 100, 0]", "[503, 0, 100, 130, 0]")),
            Table("resource_state", Rows("[501, 10, 0]", "[502, 11, 0]", "[503, 99, 0]")));
        cache.Reset(Assert.IsType<SubscriptionEvent>(ServerMessages.Parse(initial, schema)).Tables);

        var resources = SnapshotBuilder.Build(cache, Identity, Describe).Resources;
        Assert.Equal([502UL, 501UL, 503UL], resources.Select(r => r.EntityId));
        Assert.Equal("Young Beech Tree", resources[0].Name);
        Assert.Equal(2, resources[0].Distance, 3);
        Assert.Equal("ressource n° 99", resources[2].Name);
        Assert.Equal(30 * 0.8660254, resources[2].Distance, 3);
    }

    [Fact]
    public void Compte_absent_de_la_region_donne_un_instantane_vide()
    {
        var cache = new TableCache();
        var snapshot = SnapshotBuilder.Build(cache, Identity, Describe);
        Assert.Null(snapshot.EntityId);
        Assert.Null(snapshot.Position);
        Assert.Empty(snapshot.Resources);
    }

    [Fact]
    public void Erreurs_et_messages_inconnus_sont_reconnus()
    {
        Assert.Equal("table inconnue", Assert.IsType<SubscriptionErrorEvent>(
            ServerMessages.Parse("""{ "SubscriptionError": { "total_host_execution_duration_micros": 1, "request_id": 3, "error": "table inconnue" } }""", null)).Error);
        Assert.Equal("Echec", Assert.IsType<TransactionEvent>(
            ServerMessages.Parse("""{ "TransactionUpdate": { "status": { "Failed": "Echec" } } }""", null)).Failure);
        Assert.IsType<UnknownEvent>(ServerMessages.Parse("""{ "ProcedureResult": {} }""", null));
    }

    [Fact]
    public void Requetes_de_lecture_seulement()
    {
        var area = Area.Around(100.4, 200.6, 40);
        Assert.Equal(new Area(60, 141, 160, 241), area);
        var queries = Queries.FollowPlayer("abcd", 7, area);
        Assert.Contains("SELECT * FROM user_state WHERE identity = 0xabcd", queries);
        Assert.Contains("SELECT * FROM mobile_entity_state WHERE entity_id = 7", queries);
        Assert.Contains(queries, q => q.StartsWith("SELECT l.* FROM location_state l JOIN resource_state r ON l.entity_id = r.entity_id WHERE l.x >= 60 AND l.x <= 141", StringComparison.Ordinal));
        Assert.All(queries, q => Assert.StartsWith("SELECT ", q));
        Assert.DoesNotContain(Queries.FollowPlayer("abcd", 7, null), q => q.Contains("resource_state", StringComparison.Ordinal));

        var message = ServerMessages.Subscribe(queries, 5);
        Assert.StartsWith("{\"Subscribe\":{\"query_strings\":[", message);
        Assert.EndsWith("\"request_id\":5}}", message);
    }

    [Fact]
    public void La_brique_ne_peut_envoyer_aucune_action()
    {
        // Garde-fou D-039 : aucune trace d'appel de reducer dans la brique Server.
        var assembly = typeof(ServerReader).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            Assert.DoesNotContain("Reducer", type.Name, StringComparison.OrdinalIgnoreCase);
            foreach (var method in type.GetMethods())
            {
                Assert.DoesNotContain("CallReducer", method.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("\"eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln\"")]
    [InlineData("{\"token\":\"eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln\"}")]
    [InlineData("eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln")]
    public void Jeton_trouve_quelle_que_soit_la_forme_de_la_reponse(string body)
    {
        Assert.Equal("eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln", AuthClient.ExtractJwt(body));
    }

    [Fact]
    public void Reponse_sans_jeton()
    {
        Assert.Null(AuthClient.ExtractJwt("{\"error\":\"Invalid code\"}"));
    }

    [Fact]
    public void Infos_du_jeton_sans_le_secret()
    {
        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var jwt = $"{B64("{\"alg\":\"ES256\"}")}.{B64("{\"hex_identity\":\"c20077c9c9316ab6\",\"iat\":1795030024,\"exp\":1795116424}")}.c2ln";
        var info = TokenInfo.TryRead(jwt)!;
        Assert.Equal("c20077c9c9316ab6", info.HexIdentity);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1795030024), info.IssuedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1795116424), info.ExpiresAt);
        Assert.Equal("c200…16ab6"[..4], TokenInfo.ShortIdentity(info.HexIdentity)[..4]);
        Assert.Null(TokenInfo.TryRead("pas-un-jeton"));
    }

    [Fact]
    public void Jeton_chiffre_sur_le_disque_et_relu()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bitcraftbot-test-{Guid.NewGuid():N}", "jeton.bin");
        var store = new TokenStore(path);
        try
        {
            Assert.Null(store.Load());
            store.Save("eyJ.jeton-de-test.sig");
            Assert.True(store.Exists);
            // Le fichier ne contient pas le jeton en clair.
            Assert.DoesNotContain("jeton-de-test", Encoding.UTF8.GetString(File.ReadAllBytes(path)));
            Assert.Equal("eyJ.jeton-de-test.sig", store.Load());
            store.Delete();
            Assert.False(store.Exists);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Module_de_la_region()
    {
        Assert.Equal("idc-18", ServerEndpoints.RegionModule(18));
        Assert.Equal("wss://bitcraft-early-access.spacetimedb.com/v1/database/idc-18/subscribe",
            ServerEndpoints.SubscribeUri(ServerEndpoints.Host, "idc-18").ToString());
    }
}

// Contrôle sur le vrai schéma de la région (fichier téléchargé à la main) : lancé seulement si la variable
// d'environnement BITCRAFT_SCHEMA donne le chemin du fichier. Vérifie que les tables et colonnes utilisées existent.
public class H1RealSchemaTests
{
    [Fact]
    public void Tables_et_colonnes_utilisees_existent_dans_le_vrai_schema()
    {
        var path = Environment.GetEnvironmentVariable("BITCRAFT_SCHEMA");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }
        var schema = Schema.Parse(File.ReadAllText(path));
        Assert.True(schema.TableCount > 100, $"schéma trop petit : {schema.TableCount} tables");
        var expected = new Dictionary<string, string[]>
        {
            ["user_state"] = ["identity", "entity_id"],
            ["mobile_entity_state"] = ["entity_id", "location_x", "location_z", "destination_x", "destination_z", "is_walking"],
            ["health_state"] = ["entity_id", "health"],
            ["stamina_state"] = ["entity_id", "stamina"],
            ["satiation_state"] = ["entity_id", "satiation"],
            ["signed_in_player_state"] = ["entity_id"],
            ["location_state"] = ["entity_id", "x", "z"],
            ["resource_state"] = ["entity_id", "resource_id"],
            ["resource_health_state"] = ["entity_id", "health"],
            ["inventory_state"] = ["entity_id", "pockets", "inventory_index", "owner_entity_id", "player_owner_entity_id"],
            ["bank_state"] = ["building_entity_id", "coordinates"],
            ["active_buff_state"] = ["entity_id", "active_buffs"],
            ["character_stats_state"] = ["entity_id", "values"],
            ["experience_state"] = ["entity_id", "experience_stacks"],
            ["player_action_state"] = ["entity_id", "start_time", "duration", "action_type", "layer", "target", "recipe_id"],
            ["extract_outcome_state"] = ["entity_id", "target_entity_id", "damage"],
            ["player_state"] = ["entity_id", "time_played", "signed_in"],
            ["teleportation_energy_state"] = ["entity_id", "energy"],
            ["equipment_state"] = ["entity_id", "equipment_slots"],
            ["world_region_state"] = ["region_index"],
            ["world_region_name_state"] = ["id", "player_facing_name"],
        };
        foreach (var (table, columns) in expected)
        {
            var t = schema.Find(table);
            Assert.True(t is not null, $"table absente : {table}");
            Assert.True(t!.IsPublic, $"table non publique : {table}");
            foreach (var c in columns)
            {
                Assert.Contains(c, t.Columns);
            }
            Assert.NotEmpty(t.PrimaryKey);
        }
    }
}

// Essai réel, lancé seulement si BITCRAFT_LIVE=1 : un faux jeton doit être refusé par le serveur,
// sans ouvrir de liaison (aucune identité créée, aucune lecture).
public class H1LiveTests
{
    [Fact]
    public async Task Faux_jeton_refuse_par_le_serveur()
    {
        if (Environment.GetEnvironmentVariable("BITCRAFT_LIVE") != "1")
        {
            return;
        }
        await using var connection = new SpacetimeConnection();
        var uri = ServerEndpoints.SubscribeUri(ServerEndpoints.Host, ServerEndpoints.RegionModule(18));
        var ex = await Assert.ThrowsAsync<System.Net.WebSockets.WebSocketException>(() =>
            connection.ConnectAsync(uri, "eyJhbGciOiJFUzI1NiJ9.eyJmYXV4IjoiamV0b24ifQ.ZmF1eA", CancellationToken.None));
        Assert.Contains("401", ex.Message);
    }
}

// v0.3.6 : découverte de la région du personnage par le module global.
public class H1RegionFinderTests
{
    private const string Id = "c200aaaabbbbccccddddeeeeffff00001111222233334444555566667777cc03";

    private static Row R(string json) => Row.Parse(json, null);

    private static TableCache Cache(params (string Table, string[] Rows)[] tables)
    {
        var cache = new TableCache();
        foreach (var (table, rows) in tables)
        {
            cache.Apply(new TableChange(table, rows.Select(R).ToList(), []));
        }
        return cache;
    }

    [Fact]
    public void Region_du_personnage_trouvee()
    {
        var cache = Cache(
            ("user_state", [$$"""{"identity":{"__identity__":"0x{{Id}}"},"entity_id":77,"can_sign_in":true}"""]),
            ("user_region_state", [$$"""{"identity":{"__identity__":"0x{{Id}}"},"region_id":12}"""]),
            ("region_connection_info", ["""{"id":12,"host":"https://autre-hote.spacetimedb.com:443","module":"idc-12"}""", """{"id":18,"host":"x","module":"idc-18"}"""]),
            ("world_region_name_state", ["""{"id":12,"player_facing_name":"Ashveil","module_name_prefix":"idc"}"""]));
        var d = RegionFinder.Read(cache, Id);
        Assert.True(d.Success);
        Assert.Equal(77UL, d.EntityId);
        Assert.Equal(12, d.RegionId);
        Assert.Equal("Ashveil", d.RegionName);
        Assert.Equal("autre-hote.spacetimedb.com", d.Host);
        Assert.Equal("idc-12", d.Module);
        Assert.Contains("idc-12", RegionFinder.Describe(d));
    }

    [Fact]
    public void Compte_inconnu()
    {
        var d = RegionFinder.Read(Cache(("user_state", [])), Id);
        Assert.False(d.Success);
        Assert.False(d.AccountKnown);
        Assert.Contains("aucun personnage", d.Error);
    }

    [Fact]
    public void Region_sans_adresse_prend_le_nom_par_defaut()
    {
        var cache = Cache(
            ("user_state", [$$"""{"identity":"0x{{Id}}","entity_id":5}"""]),
            ("user_region_state", [$$"""{"identity":"0x{{Id}}","region_id":18}"""]));
        var d = RegionFinder.Read(cache, Id);
        Assert.True(d.Success);
        Assert.Equal("idc-18", d.Module);
        Assert.Equal(ServerEndpoints.Host, d.Host);
    }

    [Theory]
    [InlineData("bitcraft-early-access.spacetimedb.com", "bitcraft-early-access.spacetimedb.com")]
    [InlineData("https://h.example.com:443", "h.example.com")]
    [InlineData("", null)]
    public void Hote_nettoye(string input, string? expected) => Assert.Equal(expected, RegionFinder.HostOnly(input));

    [Fact]
    public void Requetes_de_decouverte_en_lecture_seule()
    {
        Assert.All(RegionFinder.Queries("abcd"), q => Assert.StartsWith("SELECT ", q));
        Assert.Equal("idc-global", RegionFinder.GlobalModule);
    }
}

// v0.3.8 : journaux détaillés de la liaison, sans jamais le jeton.
public class H1LogTests
{
    [Fact]
    public void Resume_de_message_masque_le_jeton()
    {
        var json = """{ "IdentityToken": { "identity": { "__identity__": "0xc200ff" }, "token": "eyJhbGciOiJFUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl" } }""";
        var text = ServerMessages.Summarize(1, json, withExcerpt: true);
        Assert.Contains("Message n° 1 : IdentityToken", text);
        Assert.Contains("[jeton masqué]", text);
        Assert.DoesNotContain("c2lnbmF0dXJl", text);
        Assert.Equal($"Message n° 7 : IdentityToken, {json.Length} caractères.", ServerMessages.Summarize(7, json, withExcerpt: false));
    }

    [Fact]
    public void Resume_long_tronque_et_json_illisible()
    {
        var text = ServerMessages.Summarize(2, "{\"InitialSubscription\":\"" + new string('a', 2000) + "\"}", withExcerpt: true);
        Assert.EndsWith("…", text);
        Assert.Contains("JSON illisible", ServerMessages.Summarize(3, "pas du json", withExcerpt: false));
    }

    [Fact]
    public async Task Refus_de_poignee_de_main_journalise_le_code_HTTP()
    {
        if (Environment.GetEnvironmentVariable("BITCRAFT_LIVE") != "1")
        {
            return;
        }
        var lines = new List<string>();
        await using var connection = new SpacetimeConnection();
        connection.Diagnostic += lines.Add;
        await Assert.ThrowsAsync<System.Net.WebSockets.WebSocketException>(() => connection.ConnectAsync(
            ServerEndpoints.SubscribeUri(ServerEndpoints.Host, ServerEndpoints.RegionModule(18)),
            "eyJhbGciOiJFUzI1NiJ9.eyJmYXV4IjoiamV0b24ifQ.ZmF1eA", CancellationToken.None));
        Assert.Contains(lines, l => l.Contains("HTTP 401", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.Contains("ZmF1eA", StringComparison.Ordinal));
        File.WriteAllLines(Path.Combine(Path.GetTempPath(), "bitcraftbot-diag-test.txt"), lines);
    }
}
