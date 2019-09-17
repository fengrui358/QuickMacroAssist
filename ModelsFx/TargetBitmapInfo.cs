using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FrHello.NetLib.Core.Windows.Windows;
using ModelsFx.Help;
using NLog;

namespace ModelsFx
{
    public class TargetBitmapInfo : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _filePath;

        public string FilePath => _filePath;

        private Bitmap Bitmap { get; set; }

        public BitmapImage BitmapImage { get; private set; }

        public string SizeInfo => $" W:{Bitmap.Size.Width} H:{Bitmap.Size.Height}";

        public string Name
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(_filePath))
                {
                    sb.AppendLine(_filePath);
                }

                sb.AppendLine(SizeInfo);

                return sb.ToString();
            }
        }

        public TargetBitmapInfo(string filePath)
        {
            _filePath = filePath;
        }

        public bool Init()
        {
            if (_filePath != null && File.Exists(_filePath))
            {
                try
                {
                    Bitmap = new Bitmap(_filePath);
                    BitmapImage = BitmapHelper.BitmapToImage(CopyBitmap());

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }

            return false;
        }

        public async Task<Rectangle?> Match(Bitmap srcBitmap, CancellationToken cancellationToken)
        {
            try
            {
                return await WindowsApi.ScreenApi.ScanBitmapLocation(Bitmap, srcBitmap,
                    cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public Bitmap CopyBitmap()
        {
            return Bitmap.Clone(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), Bitmap.PixelFormat);
        }

        public void Dispose()
        {
            BitmapImage = null;
            Bitmap?.Dispose();
        }
    }
}
