using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace ModelsFx
{
    public class ScreenInfo
    {
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern int BitBlt(IntPtr hDc, int x, int y, int nWidth, int nHeight, IntPtr hSrcDc, int xSrc, int ySrc, int dwRop);

        public Screen Screen { get; }

        public int Index { get; }

        public string DeviceName { get; }

        public bool Primary { get; }

        public Rectangle Rectangle { get; }

        public string Name
        {
            get
            {
                var name = $"Screen{Index + 1}";
                if (Primary)
                {
                    name = string.Concat(name, $"({nameof(Primary)})");
                }

                return name;
            }
        }

        public string SizeInfo => $" W:{Rectangle.Width} H:{Rectangle.Height}";

        public ScreenInfo(int index, Screen screen)
        {
            Screen = screen;
            Index = index;
            DeviceName = screen.DeviceName;
            Primary = screen.Primary;
            Rectangle = screen.Bounds;
        }

        public BitmapImage Capture { get; private set; }

        public void CaptureProcess()
        {
            var screenPixel = new Bitmap(Screen.Bounds.Width, Screen.Bounds.Height, PixelFormat.Format32bppArgb);

            using (var dest = Graphics.FromImage(screenPixel))
            {
                using (var src = Graphics.FromHwnd(IntPtr.Zero))
                {
                    var hSrcDc = src.GetHdc();
                    var hDc = dest.GetHdc();
                    BitBlt(hDc, 0, 0, Screen.Bounds.Width, Screen.Bounds.Height, hSrcDc, Screen.Bounds.X,
                        Screen.Bounds.Y, (int)CopyPixelOperation.SourceCopy);
                    dest.ReleaseHdc();
                    src.ReleaseHdc();
                }
            }

            Capture = BitmapToImage(screenPixel);
        }

        private BitmapImage BitmapToImage(Bitmap bitmap)
        {
            var bitmapImage = new BitmapImage();
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Bmp);

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }
    }
}
