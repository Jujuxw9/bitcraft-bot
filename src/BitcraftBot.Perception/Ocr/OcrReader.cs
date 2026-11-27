using System.Runtime.InteropServices.WindowsRuntime;
using BitcraftBot.Perception.Capture;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace BitcraftBot.Perception.Ocr;

// Type de texte à lire : clair sur fond sombre/coloré (HUD) ou sombre sur fond clair (panneaux crème).
public enum TextStyle
{
    BrightText,
    DarkText,
}

// Lecture de texte (OCR) avec le moteur intégré à Windows, en anglais (langue du jeu, D-015).
// Avant la lecture, la zone est agrandie et passée en noir et blanc pour fiabiliser le résultat.
public sealed class OcrReader
{
    public const int DefaultScale = 3;
    private readonly OcrEngine _engine;

    private OcrReader(OcrEngine engine) => _engine = engine;

    public string LanguageTag => _engine.RecognizerLanguage.LanguageTag;

    // Renvoie null si l'OCR anglais n'est pas installé sur ce Windows.
    public static OcrReader? TryCreate()
    {
        var engine = OcrEngine.TryCreateFromLanguage(new Language("en-US"))
                     ?? OcrEngine.TryCreateFromUserProfileLanguages();
        return engine is null ? null : new OcrReader(engine);
    }

    public async Task<string> ReadAsync(Frame frame, ScreenRect zone, TextStyle style, int threshold, int scale = DefaultScale)
    {
        var prepared = Prepare(frame, zone, style, threshold, scale);
        if (prepared is null)
        {
            return string.Empty;
        }
        var (pixels, width, height) = prepared.Value;
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels.AsBuffer(), BitmapPixelFormat.Bgra8, width, height);
        // Le moteur de Windows ne lit qu'une image à la fois (« Another RecognizeAsync operation is already running! »,
        // journal du 22/11) : la lecture du HUD et « ce que voit le bot » passent chacun leur tour.
        await _gate.WaitAsync();
        try
        {
            var result = await _engine.RecognizeAsync(bitmap);
            return string.Join(' ', result.Lines.Select(l => l.Text)).Trim();
        }
        finally
        {
            _gate.Release();
        }
    }

    private readonly SemaphoreSlim _gate = new(1, 1);

    // v0.4.29 (fenêtre « à terre ») : lignes lues avec leur rectangle, en pixels de l'image d'origine (frame).
    public async Task<IReadOnlyList<(string Text, ScreenRect Box)>> ReadLinesAsync(Frame frame, ScreenRect zone, TextStyle style, int threshold, int scale = DefaultScale)
    {
        var prepared = Prepare(frame, zone, style, threshold, scale);
        if (prepared is null)
        {
            return [];
        }
        var (pixels, width, height) = prepared.Value;
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(pixels.AsBuffer(), BitmapPixelFormat.Bgra8, width, height);
        await _gate.WaitAsync();
        try
        {
            var result = await _engine.RecognizeAsync(bitmap);
            var lines = new List<(string, ScreenRect)>();
            foreach (var line in result.Lines)
            {
                if (line.Words.Count == 0)
                {
                    continue;
                }
                var left = line.Words.Min(w => w.BoundingRect.X);
                var top = line.Words.Min(w => w.BoundingRect.Y);
                var right = line.Words.Max(w => w.BoundingRect.X + w.BoundingRect.Width);
                var bottom = line.Words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height);
                lines.Add((line.Text, ToFrame(zone, scale, left, top, right, bottom)));
            }
            return lines;
        }
        finally
        {
            _gate.Release();
        }
    }

    // Rectangle de l'image agrandie (avec marge) → rectangle de l'image d'origine.
    public static ScreenRect ToFrame(ScreenRect zone, int scale, double left, double top, double right, double bottom)
    {
        var x0 = (int)Math.Round((left - Margin) / scale) + zone.X;
        var y0 = (int)Math.Round((top - Margin) / scale) + zone.Y;
        var x1 = (int)Math.Round((right - Margin) / scale) + zone.X;
        var y1 = (int)Math.Round((bottom - Margin) / scale) + zone.Y;
        return new ScreenRect(x0, y0, Math.Max(1, x1 - x0), Math.Max(1, y1 - y0));
    }

    // Marge blanche ajoutée autour du texte : l'OCR lit mieux un texte qui ne touche pas les bords.
    private const int Margin = 12;

    // Largeur de la transition progressive autour du seuil (évite les lettres « en escalier »).
    private const double Softness = 40;

    // Découpe la zone, la passe en niveaux de gris (texte foncé sur fond blanc),
    // l'agrandit avec lissage et ajoute une marge blanche.
    public static (byte[] Pixels, int Width, int Height)? Prepare(Frame frame, ScreenRect zone, TextStyle style, int threshold, int scale = DefaultScale)
    {
        if (zone.X < 0 || zone.Y < 0 || zone.X + zone.Width > frame.Width || zone.Y + zone.Height > frame.Height)
        {
            return null;
        }

        // 1. Niveau de gris de chaque pixel : 0 = encre (texte), 255 = papier (fond).
        var gray = new double[zone.Width * zone.Height];
        for (var y = 0; y < zone.Height; y++)
        {
            for (var x = 0; x < zone.Width; x++)
            {
                var i = ((zone.Y + y) * frame.Width + zone.X + x) * 4;
                var luminance = 0.114 * frame.Pixels[i] + 0.587 * frame.Pixels[i + 1] + 0.299 * frame.Pixels[i + 2];
                // Distance au seuil, côté texte positif.
                var toward = style == TextStyle.BrightText ? luminance - threshold : threshold - luminance;
                var ink = Math.Clamp((toward + Softness / 2) / Softness, 0, 1);
                gray[y * zone.Width + x] = 255 * (1 - ink);
            }
        }

        // 2. Agrandissement avec interpolation bilinéaire, dans une image avec marge.
        var width = zone.Width * scale + 2 * Margin;
        var height = zone.Height * scale + 2 * Margin;
        var output = new byte[width * height * 4];
        Array.Fill(output, (byte)255);
        for (var oy = 0; oy < zone.Height * scale; oy++)
        {
            var sy = Math.Clamp((oy + 0.5) / scale - 0.5, 0, zone.Height - 1);
            var y0 = (int)sy;
            var y1 = Math.Min(y0 + 1, zone.Height - 1);
            var fy = sy - y0;
            for (var ox = 0; ox < zone.Width * scale; ox++)
            {
                var sx = Math.Clamp((ox + 0.5) / scale - 0.5, 0, zone.Width - 1);
                var x0 = (int)sx;
                var x1 = Math.Min(x0 + 1, zone.Width - 1);
                var fx = sx - x0;
                var top = gray[y0 * zone.Width + x0] * (1 - fx) + gray[y0 * zone.Width + x1] * fx;
                var bottom = gray[y1 * zone.Width + x0] * (1 - fx) + gray[y1 * zone.Width + x1] * fx;
                var value = (byte)Math.Round(top * (1 - fy) + bottom * fy);
                var o = ((oy + Margin) * width + ox + Margin) * 4;
                output[o] = value;
                output[o + 1] = value;
                output[o + 2] = value;
            }
        }
        return (output, width, height);
    }
}
