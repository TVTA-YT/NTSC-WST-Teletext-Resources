using System.Drawing;
using System.Drawing.Imaging;

namespace TeletextSharedResources
{
    public class RenderedLayersNova : IDisposable
    {
        public int BorderSizeX;
        public int BorderSizeY;

        public Bitmap Background;
        public Bitmap Foreground;

        public int Level1XStart = 50;
        public int Level1YStart = 30;

        public float dpiX = 96;
        public float dpiY = 96;

        public Boolean[] doubleHeight2ndRows = new Boolean[31];

        public Color Transparency;
        public Mode Mode = new Mode();

        public void ClearAll(bool Mix)
        {
            using (Graphics gb = Graphics.FromImage(Background))
            {
                gb.Clear(Mix ? Transparency : Color.Black);
            }

            using (Graphics gf = Graphics.FromImage(Foreground))
            {
                gf.Clear(Mix ? Transparency : Color.Black);
            }
        }

        public void ResetDoubleHeights()
        {
            for (int n = doubleHeight2ndRows.GetLowerBound(0); n <= doubleHeight2ndRows.GetUpperBound(0); n++)
                doubleHeight2ndRows[n] = false;
        }

        public RenderedLayersNova(float deviceDPI, bool Borders = false, bool Mix = false)
        {
            if (Borders)
            {
                BorderSizeX = 220;
                BorderSizeY = 100;
                Level1XStart = 50;
                Level1YStart = 30;
            }
            else
            {
                BorderSizeX = 0;
                BorderSizeY = 0;
                Level1XStart = 0;
                Level1YStart = 0;
            }

            float scaleX = deviceDPI / 96;
            float scaleY = deviceDPI / 96;

            Background = new Bitmap((int)((480 + BorderSizeX) * scaleX), (int)((500 + BorderSizeY) * scaleY), PixelFormat.Format32bppArgb);
            Foreground = new Bitmap((int)((480 + BorderSizeX) * scaleX), (int)((500 + BorderSizeY) * scaleY), PixelFormat.Format32bppArgb);

            Background.SetResolution(deviceDPI, deviceDPI);
            Foreground.SetResolution(deviceDPI, deviceDPI);

            ClearAll(Mix);
        }

        public void Dispose()
        {
            Background?.Dispose();
            Foreground?.Dispose();
        }
    }
}
