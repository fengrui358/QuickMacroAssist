using System.Drawing;
using System.Text;

namespace Models
{
    public class ScreenInfo
    {
        public int Index { get; set; }

        public bool Primary { get; set; }

        public Rectangle Rectangle { get; set; }

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
    }
}
