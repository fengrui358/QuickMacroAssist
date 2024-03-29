﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ModelsFx.Help;

namespace ModelsFx
{
    public class ScreenInfo
    {
        private List<ColorInfo> _uniqueColorInfos = new List<ColorInfo>();

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

        private Bitmap Bitmap { get; set; }

        public BitmapImage Capture { get; private set; }

        private void Clear()
        {
            Bitmap = null;
            Capture = null;
            _uniqueColorInfos.Clear();
        }

        public void CaptureProcess(bool showTaskBar)
        {
            Clear();

            var width = showTaskBar ? Screen.Bounds.Width : Screen.WorkingArea.Width;
            var height = showTaskBar ? Screen.Bounds.Height : Screen.WorkingArea.Height;

            var x = showTaskBar ? Screen.Bounds.X : Screen.WorkingArea.X;
            var y = showTaskBar ? Screen.Bounds.Y : Screen.WorkingArea.Y;

            var screenPixel = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (var dest = Graphics.FromImage(screenPixel))
            {
                using (var src = Graphics.FromHwnd(IntPtr.Zero))
                {
                    var hSrcDc = src.GetHdc();
                    var hDc = dest.GetHdc();
                    BitBlt(hDc, 0, 0, width, height, hSrcDc, x, y, (int)CopyPixelOperation.SourceCopy);
                    dest.ReleaseHdc();
                    src.ReleaseHdc();
                }
            }

            Bitmap = screenPixel;
            Capture = BitmapHelper.BitmapToImage(CopyBitmap());
        }

        public async Task<IEnumerable<ColorInfo>> ScanAllUniqueColors(CancellationToken cancellationToken)
        {
            if (Bitmap != null)
            {
                var uniqueColors = new HashSet<ColorInfo>();
                var repeatColors = new HashSet<ColorInfo>();

                var bitmap = CopyBitmap();

                return await Task.Run(() =>
                {
                    for (var i = 0; i < bitmap.Width; i++)
                    {
                        for (var j = 0; j < bitmap.Height; j++)
                        {
                            var color = bitmap.GetPixel(i, j);
                            var colorInfo = new ColorInfo(this, color, new Point(i, j));
                            if (!repeatColors.Contains(colorInfo))
                            {
                                if (uniqueColors.Contains(colorInfo))
                                {
                                    repeatColors.Add(colorInfo);
                                    uniqueColors.Remove(colorInfo);
                                }
                                else
                                {
                                    uniqueColors.Add(colorInfo);
                                }
                            }

                            if (cancellationToken.IsCancellationRequested)
                            {
                                return new List<ColorInfo>();
                            }
                        }
                    }

                    _uniqueColorInfos = uniqueColors.ToList();
                    return _uniqueColorInfos;
                }, cancellationToken);
            }

            return new List<ColorInfo>();
        }

        public Bitmap CopyBitmap()
        {
            return Bitmap.Clone(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), Bitmap.PixelFormat);
        }
    }
}
