using System.Drawing;
using System.Windows.Forms;

namespace ModelsFx
{
    public class ScreenInfo
    {
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
    }
}
