using BitcraftBot.Server.Aim;
using BitcraftBot.Server.World;
using Xunit;

namespace BitcraftBot.Tests;

// Caméra de jeu simulée : vue de 3/4 en perspective, tournée, personnage au centre.
// Sert à vérifier que la formule apprise (R1) retrouve les pixels, même avec des clics imprécis et des indices faux.
internal static class FakeCamera
{
    public const int Width = 1920;
    public const int Height = 1080;

    // (east, north) au sol, height : hauteur du point cliqué (tronc) → pixel.
    public static (int X, int Y) Pixel(double east, double north, double height = 1.0, double yawDeg = 25, double pitchDeg = 50, double distance = 30)
    {
        var yaw = yawDeg * Math.PI / 180;
        var pitch = pitchDeg * Math.PI / 180;
        // Repère : x = est, y = haut, z = nord. La caméra regarde le personnage (origine) depuis le sud (tournée de yaw).
        var fx = Math.Sin(yaw) * Math.Cos(pitch);
        var fz = Math.Cos(yaw) * Math.Cos(pitch);
        var fy = -Math.Sin(pitch);
        var forward = (X: fx, Y: fy, Z: fz);
        var cam = (X: -forward.X * distance, Y: -forward.Y * distance, Z: -forward.Z * distance);
        var right = Normalize(Cross(forward, (0, 1, 0)));
        var up = Cross(right, forward);
        var d = (X: east - cam.X, Y: height - cam.Y, Z: north - cam.Z);
        var depth = Dot(d, forward);
        var focal = 0.5 / Math.Tan(30 * Math.PI / 180) * Height;
        var x = Dot(d, right) / depth * focal + Width / 2.0;
        var y = -Dot(d, up) / depth * focal + Height / 2.0;
        return ((int)Math.Round(x), (int)Math.Round(y));
    }

    private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b) =>
        (a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static (double X, double Y, double Z) Normalize((double X, double Y, double Z) a)
    {
        var l = Math.Sqrt(Dot(a, a));
        return (a.X / l, a.Y / l, a.Z / l);
    }

    public static CameraPair Pair(double east, double north, Random noise, double noisePx = 8, double height = 1.0)
    {
        var (x, y) = Pixel(east, north, height);
        return new CameraPair
        {
            East = east,
            North = north,
            PixelX = x + (int)Math.Round((noise.NextDouble() * 2 - 1) * noisePx),
            PixelY = y + (int)Math.Round((noise.NextDouble() * 2 - 1) * noisePx),
            ClientWidth = Width,
            ClientHeight = Height,
            Resource = "Mature Oak Tree",
            Tag = "Tree",
        };
    }

    public static List<CameraPair> Clicks(int count, int seed, double noisePx = 8)
    {
        var random = new Random(seed);
        var list = new List<CameraPair>();
        for (var i = 0; i < count; i++)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var dist = 2 + random.NextDouble() * 10;
            // Hauteur du clic sur le tronc : variable (Julien ne clique pas toujours au même endroit).
            list.Add(Pair(Math.Cos(angle) * dist, Math.Sin(angle) * dist, random, noisePx, 0.5 + random.NextDouble() * 1.5));
        }
        return list;
    }
}

public class V040CameraFitTests
{
    [Fact]
    public void Trop_peu_de_clics_pas_de_formule()
    {
        Assert.Null(CameraFit.Compute(FakeCamera.Clicks(2, 1)));
        var three = CameraFit.Compute(FakeCamera.Clicks(3, 1));
        Assert.NotNull(three);
        // 3 clics : la formule passe exactement par eux, précision pas mesurable.
        Assert.Null(three!.MedianErrorPx);
    }

    [Fact]
    public void Quinze_clics_imprecis_donnent_moins_de_25_px()
    {
        var fit = CameraFit.Compute(FakeCamera.Clicks(15, 7));
        Assert.NotNull(fit);
        Assert.NotNull(fit!.MedianErrorPx);
        Assert.True(fit.MedianErrorPx < 25, $"précision {fit.MedianErrorPx:0.0} px");
        // Un arbre jamais cliqué, à 6 cases au nord-est : le pixel prévu est proche du vrai.
        var truth = FakeCamera.Pixel(4.2, 4.2);
        var predicted = fit.ToPixel(4.2, 4.2, FakeCamera.Width, FakeCamera.Height)!.Value;
        var error = Math.Sqrt(Math.Pow(predicted.X - truth.X, 2) + Math.Pow(predicted.Y - truth.Y, 2));
        Assert.True(error < 30, $"écart {error:0} px");
    }

    [Fact]
    public void Les_faux_indices_sont_ecartes()
    {
        var pairs = FakeCamera.Clicks(14, 3);
        // 3 faux indices : caméra tournée sans W, clic sur autre chose…
        pairs.Add(pairs[0] with { PixelX = pairs[0].PixelX + 300 });
        pairs.Add(pairs[1] with { PixelY = pairs[1].PixelY - 250 });
        pairs.Add(pairs[2] with { PixelX = 100, PixelY = 900 });
        var fit = CameraFit.Compute(pairs)!;
        Assert.Equal(3, fit.Rejected);
        Assert.True(fit.MedianErrorPx < 25, $"précision {fit.MedianErrorPx:0.0} px");
    }

    [Fact]
    public void Perspective_choisie_quand_elle_predit_mieux()
    {
        // Clics précis, sur une grande zone : la perspective se voit.
        var random = new Random(5);
        var pairs = new List<CameraPair>();
        for (var e = -12; e <= 12; e += 4)
        {
            for (var n = -12; n <= 12; n += 4)
            {
                if (e != 0 || n != 0)
                {
                    pairs.Add(FakeCamera.Pair(e, n, random, noisePx: 1));
                }
            }
        }
        var fit = CameraFit.Compute(pairs, thresholdPx: 60)!;
        Assert.Equal(CameraFormula.Perspective, fit.Formula);
        Assert.True(fit.MedianErrorPx < 5, $"précision {fit.MedianErrorPx:0.0} px");
    }

    [Fact]
    public void Taille_de_fenetre_differente_meme_formule()
    {
        var fit = CameraFit.Compute(FakeCamera.Clicks(12, 11, noisePx: 2))!;
        // Fenêtre deux fois plus petite (960 × 540) : même angle de vue, pixels divisés par 2.
        var full = fit.ToPixel(3, -5, 1920, 1080)!.Value;
        var half = fit.ToPixel(3, -5, 960, 540)!.Value;
        Assert.InRange(half.X, full.X / 2 - 1, full.X / 2 + 1);
        Assert.InRange(half.Y, full.Y / 2 - 1, full.Y / 2 + 1);
    }

    [Fact]
    public void Clics_alignes_pas_de_formule()
    {
        var random = new Random(1);
        var pairs = Enumerable.Range(1, 5).Select(i => FakeCamera.Pair(i, 2 * i, random, 0)).ToList();
        Assert.Null(CameraFit.Compute(pairs));
    }
}

public class V040CameraLearnerTests
{
    private static readonly DateTime T0 = new(2026, 11, 19, 21, 0, 0);
    private static readonly ServerPosition Here = new(30000, 30000);

    private static NearbyResource Tree(ulong id, double dEast, double dNorth) =>
        new(id, 1, "Mature Oak Tree", 1, "Tree", new ServerPosition(Here.X + dEast * 3, Here.Z + dNorth * 3), 0);

    private static readonly NearbyResource[] Trees = [Tree(101, 3, 4), Tree(102, -5, 2), Tree(103, 2, -6), Tree(104, -3, -3), Tree(105, 7, 1)];

    private static PlayerSnapshot Snap(ServerPosition? at = null, ActionInfo? action = null, ExtractInfo? extract = null) => new()
    {
        Position = at ?? Here,
        Action = action,
        LastExtract = extract,
        Resources = Trees,
    };

    private static ActionInfo Chop(ulong target, long start) => new("Extract", start, 1500, target, null);

    // Le personnage est immobile quelques secondes : l'historique des positions est rempli.
    private static CameraLearner Learner(CameraState state = CameraState.Recentree)
    {
        var learner = new CameraLearner();
        if (state == CameraState.Recentree)
        {
            learner.OnReset(T0.AddSeconds(-10));
        }
        for (var i = 0; i < 6; i++)
        {
            learner.Update(T0.AddSeconds(-3 + i * 0.5), Snap());
        }
        return learner;
    }

    private static (int X, int Y) PixelOf(NearbyResource tree) =>
        FakeCamera.Pixel((tree.Position.X - Here.X) / 3, (tree.Position.Z - Here.Z) / 3);

    [Fact]
    public void Clic_puis_recolte_confirmee_donne_une_paire()
    {
        var learner = Learner();
        var (x, y) = PixelOf(Trees[0]);
        Assert.Null(learner.OnRightClick(T0, x, y, 1920, 1080, Snap()));
        Assert.True(learner.WaitingConfirmation);
        // Le personnage marche vers l'arbre : pas encore de récolte.
        Assert.Empty(learner.Update(T0.AddSeconds(1), Snap(new ServerPosition(Here.X + 2, Here.Z + 3))));
        var events = learner.Update(T0.AddSeconds(3), Snap(new ServerPosition(Here.X + 6, Here.Z + 9), Chop(101, 5000)));
        var learned = Assert.Single(events);
        Assert.True(learned.Important);
        Assert.Contains("Clic appris n° 1", learned.Message);
        var pair = Assert.Single(learner.Pairs);
        // Position par rapport au personnage AU MOMENT DU CLIC, pas après la marche.
        Assert.Equal(3, pair.East, 3);
        Assert.Equal(4, pair.North, 3);
        Assert.Equal(x, pair.PixelX);
    }

    [Fact]
    public void Camera_tournee_clic_ignore()
    {
        var learner = Learner();
        Assert.NotNull(learner.OnMoved(T0.AddSeconds(-5), "Q"));
        // Deuxième mouvement : pas de nouvelle ligne dans le journal.
        Assert.Null(learner.OnMoved(T0.AddSeconds(-4), "molette"));
        var refused = learner.OnRightClick(T0, 900, 500, 1920, 1080, Snap());
        Assert.Contains("appuyer sur W", refused!.Message);
        Assert.False(learner.WaitingConfirmation);
        // W recentre : le clic suivant compte (après 1 s).
        learner.OnReset(T0.AddSeconds(1));
        Assert.NotNull(learner.OnRightClick(T0.AddSeconds(1.5), 900, 500, 1920, 1080, Snap()));
        Assert.Null(learner.OnRightClick(T0.AddSeconds(2.5), 900, 500, 1920, 1080, Snap()));
        Assert.True(learner.WaitingConfirmation);
    }

    [Fact]
    public void Camera_jamais_touchee_clic_garde_comme_indice()
    {
        var learner = Learner(CameraState.Inconnue);
        Assert.Equal(CameraState.Inconnue, learner.State);
        Assert.Null(learner.OnRightClick(T0, 900, 500, 1920, 1080, Snap()));
        Assert.True(learner.WaitingConfirmation);
    }

    [Fact]
    public void Arbre_deja_coupe_ne_confirme_pas_un_nouveau_clic()
    {
        var learner = Learner();
        // Julien coupe déjà l'arbre 101, et fait un clic droit ailleurs (sur un coffre, un PNJ…).
        var cutting = Snap(action: Chop(101, 1000), extract: new ExtractInfo(101, 5));
        learner.OnRightClick(T0, 300, 300, 1920, 1080, cutting);
        // Coup suivant sur le même arbre : nouvelle action du serveur, mais pas une nouvelle cible.
        Assert.Empty(learner.Update(T0.AddSeconds(1.5), Snap(action: Chop(101, 2500), extract: new ExtractInfo(101, 5))));
        Assert.Empty(learner.Pairs);
        Assert.True(learner.WaitingConfirmation);
        // Après 20 s sans autre récolte : abandon.
        var expired = Assert.Single(learner.Update(T0.AddSeconds(21), Snap()));
        Assert.Contains("pas appris", expired.Message);
        Assert.False(learner.WaitingConfirmation);
    }

    [Fact]
    public void Personnage_en_marche_au_clic_pas_appris()
    {
        var learner = new CameraLearner();
        learner.OnReset(T0.AddSeconds(-10));
        // Il court : 1 case écran par demi-seconde.
        for (var i = 0; i < 6; i++)
        {
            learner.Update(T0.AddSeconds(-2.5 + i * 0.5), Snap(new ServerPosition(Here.X + i * 3, Here.Z)));
        }
        learner.OnRightClick(T0, 900, 500, 1920, 1080, Snap());
        var e = Assert.Single(learner.Update(T0.AddSeconds(2), Snap(action: Chop(102, 9000))));
        Assert.Contains("en mouvement", e.Message);
        Assert.Empty(learner.Pairs);
    }

    [Fact]
    public void Ressource_inconnue_pas_apprise()
    {
        var learner = Learner();
        learner.OnRightClick(T0, 900, 500, 1920, 1080, Snap());
        var e = Assert.Single(learner.Update(T0.AddSeconds(2), Snap(action: Chop(999, 9000))));
        Assert.Contains("inconnue", e.Message);
    }

    [Fact]
    public void Sans_serveur_clic_ignore()
    {
        var learner = new CameraLearner();
        Assert.Contains("serveur", learner.OnRightClick(T0, 1, 1, 1920, 1080, null)!.Message);
    }

    [Fact]
    public void Douze_arbres_coupes_formule_precise_et_ecart_annonce()
    {
        var learner = Learner();
        var random = new Random(4);
        var clock = T0;
        long start = 10000;
        var targets = new List<NearbyResource>();
        for (var i = 0; i < 12; i++)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var dist = 2 + random.NextDouble() * 9;
            targets.Add(Tree((ulong)(200 + i), Math.Cos(angle) * dist, Math.Sin(angle) * dist));
        }
        var snapshot = new PlayerSnapshot { Position = Here, Resources = targets };
        foreach (var tree in targets)
        {
            // Retour au point de départ, immobile 2 s, W, clic (imprécis de quelques pixels), récolte.
            for (var k = 0; k < 4; k++)
            {
                clock = clock.AddSeconds(0.5);
                learner.Update(clock, snapshot);
            }
            var (x, y) = PixelOf(tree);
            learner.OnRightClick(clock, x + random.Next(-6, 7), y + random.Next(-6, 7), 1920, 1080, snapshot);
            clock = clock.AddSeconds(2);
            start += 60000;
            learner.Update(clock, snapshot with { Action = Chop(tree.EntityId, start), Position = new ServerPosition(Here.X + 5, Here.Z) });
            clock = clock.AddSeconds(30);
            learner.Update(clock, snapshot with { Action = null });
        }
        Assert.Equal(12, learner.Pairs.Count);
        Assert.True(learner.Fit!.MedianErrorPx < 25, learner.Summary());
        Assert.NotNull(learner.LastPredictionErrorPx);
        Assert.True(learner.LastPredictionErrorPx < 40, $"écart {learner.LastPredictionErrorPx:0}");
        Assert.Contains("précision", learner.Summary());

        // Rechargement depuis le fichier (même liste) : même formule.
        var reloaded = new CameraLearner();
        reloaded.Load(learner.Pairs);
        Assert.Equal(learner.Fit.MedianErrorPx, reloaded.Fit!.MedianErrorPx);
    }
}
