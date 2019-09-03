using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;

namespace ModelsFx
{
    public class ColorInfo : IEquatable<ColorInfo>
    {
        public ScreenInfo ScreenInfo { get; }

        public Color Color { get; }

        public Point Point { get; }

        public SolidColorBrush SolidColorBrush => new SolidColorBrush(System.Windows.Media.Color.FromArgb(Color.A, Color.R, Color.G, Color.B));

        public string ColorHex => BitConverter.ToString(new[] {Color.R, Color.G, Color.B}).Replace("-", string.Empty);

        public UIElement UiElement { get; set; }

        public ColorInfo(ScreenInfo screenInfo, Color color, Point point)
        {
            ScreenInfo = screenInfo;
            Color = color;
            Point = point;
        }

        public bool Equals(ColorInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(ScreenInfo, other.ScreenInfo) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ColorInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ScreenInfo != null ? ScreenInfo.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"X:{Point.X} Y:{Point.Y} #{ColorHex}";
        }
    }
}
