using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ModelsFx;

namespace WpfViews.Controls
{
    /// <summary>
    /// ColorRectangle.xaml 的交互逻辑
    /// </summary>
    public partial class ColorRectangle
    {
        private ColorInfo _colorInfo;

        public static readonly DependencyProperty ShowBufferProperty = DependencyProperty.Register(
            "ShowBuffer", typeof(bool), typeof(ColorRectangle), new PropertyMetadata(default(bool), ShowBufferChangedCallback));

        private static void ShowBufferChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }

        public bool ShowBuffer
        {
            get => (bool) GetValue(ShowBufferProperty);
            set => SetValue(ShowBufferProperty, value);
        }

        public ColorRectangle(ColorInfo colorInfo)
        {
            InitializeComponent();

            _colorInfo = colorInfo;
            Inner.Background = new SolidColorBrush(Color.FromArgb(colorInfo.Color.A, colorInfo.Color.R,
                colorInfo.Color.G, colorInfo.Color.B));
        }

        public void UpdateColorInfo(ColorInfo colorInfo)
        {
            _colorInfo = colorInfo;
            Inner.Background = new SolidColorBrush(Color.FromArgb(colorInfo.Color.A, colorInfo.Color.R,
                colorInfo.Color.G, colorInfo.Color.B));
        }

        private void ColorRectangle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }
    }
}
