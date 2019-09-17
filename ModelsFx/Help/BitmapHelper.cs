using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ModelsFx.Help
{
    public class BitmapHelper
    {
        public static BitmapImage BitmapToImage(Bitmap bitmap)
        {
            var bitmapImage = new BitmapImage();
            using (var ms = new MemoryStream())
            using (bitmap)
            {
                bitmap.Save(ms, ImageFormat.Png);

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        public static string SaveFile(Bitmap bitmap)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pictures");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, Guid.NewGuid().ToString("N"));
            path += ".bmp";

            bitmap.Save(path, ImageFormat.Bmp);
            return path;
        }
    }
}
