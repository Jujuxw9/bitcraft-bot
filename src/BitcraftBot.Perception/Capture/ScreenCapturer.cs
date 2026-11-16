using System.Runtime.InteropServices;
using BitcraftBot.Perception.Native;

namespace BitcraftBot.Perception.Capture;

// Capture une zone de l'écran (méthode GDI : copie de ce qui est affiché).
// Fonctionne avec le jeu en fenêtré sans bordure (D-006). Lecture seule, aucune interaction avec le jeu.
public sealed class ScreenCapturer
{
    public Frame Capture(ScreenRect area)
    {
        if (area.IsEmpty)
        {
            throw new ArgumentException("Zone de capture vide.", nameof(area));
        }

        var screenDc = Win32.GetDC(IntPtr.Zero);
        var memoryDc = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        try
        {
            memoryDc = Win32.CreateCompatibleDC(screenDc);
            bitmap = Win32.CreateCompatibleBitmap(screenDc, area.Width, area.Height);
            var previous = Win32.SelectObject(memoryDc, bitmap);

            if (!Win32.BitBlt(memoryDc, 0, 0, area.Width, area.Height, screenDc, area.X, area.Y,
                    Win32.SRCCOPY | Win32.CAPTUREBLT))
            {
                throw new InvalidOperationException($"Échec de la copie d'écran (code {Marshal.GetLastWin32Error()}).");
            }
            Win32.SelectObject(memoryDc, previous);

            // Hauteur négative = image rangée du haut vers le bas.
            var header = new Win32.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
                biWidth = area.Width,
                biHeight = -area.Height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = Win32.BI_RGB,
            };
            var pixels = new byte[area.Width * area.Height * 4];
            var lines = Win32.GetDIBits(memoryDc, bitmap, 0, (uint)area.Height, pixels, ref header, Win32.DIB_RGB_COLORS);
            if (lines == 0)
            {
                throw new InvalidOperationException("Échec de la lecture des pixels capturés.");
            }

            return new Frame
            {
                Pixels = pixels,
                Width = area.Width,
                Height = area.Height,
                CapturedAt = DateTime.Now,
                Source = area,
            };
        }
        finally
        {
            if (bitmap != IntPtr.Zero)
            {
                Win32.DeleteObject(bitmap);
            }
            if (memoryDc != IntPtr.Zero)
            {
                Win32.DeleteDC(memoryDc);
            }
            Win32.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}
