using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ModelsFx
{
    public class TargetBitmapInfo
    {
        private readonly string _filePath;

        private Bitmap Bitmap { get; set; }

        public BitmapImage Capture { get; private set; }

        public TargetBitmapInfo(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<bool> Init()
        {
            if (_filePath != null && File.Exists(_filePath))
            {
                try
                {
                    Bitmap = new Bitmap(_filePath);

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
            }

            return false;
        }
    }
}
