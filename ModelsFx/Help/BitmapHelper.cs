using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace ModelsFx.Help
{
    public class BitmapHelper
    {
        public static BitmapImage BitmapToImage(Bitmap bitmap)
        {
            var bitmapImage = new BitmapImage();
            using (var ms = new System.IO.MemoryStream())
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
    }
}
