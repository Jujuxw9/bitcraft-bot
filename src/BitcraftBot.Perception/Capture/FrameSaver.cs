using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BitcraftBot.Perception.Capture;

// Enregistre une image capturée en PNG (images de diagnostic, à côté du journal ; jamais envoyées sur GitHub, D-026).
public static class FrameSaver
{
    public static async Task SavePngAsync(Frame frame, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Copie : le canal alpha est forcé à 255 (image opaque).
        var pixels = (byte[])frame.Pixels.Clone();
        for (var i = 3; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;
        }
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)frame.Width, (uint)frame.Height, 96, 96, pixels);
        await encoder.FlushAsync();
        var bytes = new byte[stream.Size];
        stream.Seek(0);
        await stream.ReadAsync(bytes.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
        await File.WriteAllBytesAsync(path, bytes);
    }
}
