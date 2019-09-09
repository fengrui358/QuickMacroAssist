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
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ModelsFx
{
    public class TargetBitmapInfo
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _filePath;

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
                    Bitmap = new Bitmap(_filePath, true);
                    BitmapImage = BitmapHelper.BitmapToImage(Bitmap);

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
                WindowsApi.ScreenApi.BitmapMatchOption = BitmapMatchOptions.SiftMatch;

                var x = FindPicFromImage(srcBitmap, Bitmap);

                return await WindowsApi.ScreenApi.ScanBitmapLocation(Bitmap, srcBitmap,
                    cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return null;
        }

        public static System.Drawing.Point FindPicFromImage(Bitmap imgSrc, Bitmap imgSub, double threshold = 0.9, TemplateMatchModes templateMatch = TemplateMatchModes.CCoeffNormed)
        {
            OpenCvSharp.Mat srcMat = null;
            OpenCvSharp.Mat dstMat = null;
            OpenCvSharp.OutputArray outArray = null;
            try
            {
                srcMat = imgSrc.ToMat();
                dstMat = imgSub.ToMat();
                outArray = OpenCvSharp.OutputArray.Create(srcMat);

                OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, templateMatch);
                double minValue, maxValue;
                OpenCvSharp.Point location, point;
                OpenCvSharp.Cv2.MinMaxLoc(OpenCvSharp.InputArray.Create(outArray.GetMat()), out minValue, out maxValue, out location, out point);
                Console.WriteLine(maxValue);
                if (maxValue >= threshold)
                    return new System.Drawing.Point(point.X, point.Y);
                return System.Drawing.Point.Empty;
            }
            catch (Exception ex)
            {
                return System.Drawing.Point.Empty;
            }
            finally
            {
                if (srcMat != null)
                    srcMat.Dispose();
                if (dstMat != null)
                    dstMat.Dispose();
                if (outArray != null)
                    outArray.Dispose();
            }
        }
    }
}
