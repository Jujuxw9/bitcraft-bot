namespace BitcraftBot.Server.World;

// v0.4.28 : image des hexagones du fond de carte, calculée point par point (sans objets graphiques).
// Avant (v0.4.20 à v0.4.27), chaque case était une géométrie dessinée sur le fil de l'interface :
// à 200 cases de vue, ≈ 260 000 géométries, la fenêtre gelait. Ici, chaque point de l'image prend la
// couleur de la case dont le centre est le plus proche : c'est exactement le pavage en hexagones
// (pointe en haut, une ligne sur deux décalée d'une demi-case), sans trou entre les cases.
// Pensé pour tourner en arrière-plan ; aucun lien avec WPF, donc testable.
public static class HexRaster
{
    public static readonly double RowFactor = Math.Sqrt(3) / 2;

    // Bord sombre entre les cases, seulement quand elles sont assez grandes (même règle qu'avant).
    public const int EdgeFromPixels = 8;

    // Largeur et hauteur de l'image, en points, pour hexPixels points par case.
    public static (int Width, int Height) SizeOf(TerrainImage image, int hexPixels) =>
        ((int)Math.Ceiling((image.Width + 0.5) * hexPixels), (int)Math.Ceiling(image.Height * hexPixels * RowFactor));

    // Points par case : image limitée à environ maxSide points de côté (vue large = beaucoup de morceaux).
    public static int PixelsPerCell(TerrainImage image, int maxSide = 2400) =>
        Math.Clamp(maxSide / Math.Max(image.Width, image.Height), 2, 12);

    // Points ARGB prémultipliés (format Pbgra32), ligne par ligne ; 0 (transparent) hors des cases.
    public static uint[] Render(TerrainImage image, int hexPixels, out int width, out int height)
    {
        (width, height) = SizeOf(image, hexPixels);
        var w = width;
        var pixels = new uint[width * height];
        var rowPixels = hexPixels * RowFactor;
        var edges = hexPixels >= EdgeFromPixels;
        // Ligne de bord : environ 0,8 point de large, comme le trait d'avant.
        var edgeWidth = 0.8;
        // Décalage d'une ligne d'image : la ligne du nord (row 0) a la case Z = NorthCell.
        var northOdd = (image.NorthCell & 1) == 1;

        Parallel.For(0, height, y =>
        {
            var py = y + 0.5;
            var approxRow = (int)Math.Floor(py / rowPixels);
            for (var x = 0; x < w; x++)
            {
                var px = x + 0.5;
                double best = double.MaxValue, second = double.MaxValue;
                var bestIndex = -1;
                for (var row = approxRow - 1; row <= approxRow + 1; row++)
                {
                    if (row < 0 || row >= image.Height)
                    {
                        continue;
                    }
                    // Même règle que l'ancien dessin : décalage d'une demi-case vers l'est pour les lignes Z impaires.
                    var odd = ((row & 1) == 1) != northOdd;
                    var shift = odd ? 0.5 : 0.0;
                    var cy = (row + 0.5) * rowPixels;
                    var dy = py - cy;
                    var nearCol = (int)Math.Round(px / hexPixels - 0.5 - shift);
                    var from = edges ? nearCol - 1 : nearCol;
                    var to = edges ? nearCol + 1 : nearCol;
                    for (var col = from; col <= to; col++)
                    {
                        if (col < 0 || col >= image.Width)
                        {
                            continue;
                        }
                        var dx = px - (col + 0.5 + shift) * hexPixels;
                        var d = dx * dx + dy * dy;
                        if (d < best)
                        {
                            second = best;
                            best = d;
                            bestIndex = row * image.Width + col;
                        }
                        else if (d < second)
                        {
                            second = d;
                        }
                    }
                }
                if (bestIndex < 0)
                {
                    continue;
                }
                var color = image.Pixels[bestIndex];
                if (edges && second < double.MaxValue)
                {
                    // Distance au bord entre les deux centres les plus proches : (d2 − d1) / (2 × écart des centres).
                    var gap = (second - best) / (2 * hexPixels);
                    if (gap < edgeWidth / 2)
                    {
                        color = Darken(color);
                    }
                }
                pixels[y * w + x] = color;
            }
        });
        return pixels;
    }

    // Bord : environ 22 % plus sombre (le trait d'avant était noir à 55/255).
    private static uint Darken(uint argb)
    {
        const uint keep = 200;
        var r = ((argb >> 16) & 0xFF) * keep / 255;
        var g = ((argb >> 8) & 0xFF) * keep / 255;
        var b = (argb & 0xFF) * keep / 255;
        return (argb & 0xFF000000u) | (r << 16) | (g << 8) | b;
    }
}
