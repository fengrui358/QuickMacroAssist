using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CoreFx.ViewModels;
using ModelsFx;

namespace WpfViews.Controls
{
    /// <summary>
    /// ColorRectangle.xaml 的交互逻辑
    /// </summary>
    public partial class ColorRectangle
    {
        private ColorInfo _colorInfo;

        public static event EventHandler<ColorInfo> ColorInfoSelected;


        public ColorRectangle(ColorInfo colorInfo)
        {
            InitializeComponent();

            _colorInfo = colorInfo;
            Inner.Background = new SolidColorBrush(Color.FromArgb(colorInfo.Color.A, colorInfo.Color.R,
                colorInfo.Color.G, colorInfo.Color.B));

            DataContext = _colorInfo;
        }

        public void UpdateColorInfo(ColorInfo colorInfo)
        {
            _colorInfo = colorInfo;
            Inner.Background = new SolidColorBrush(Color.FromArgb(colorInfo.Color.A, colorInfo.Color.R,
                colorInfo.Color.G, colorInfo.Color.B));

            DataContext = _colorInfo;
        }

        private void ColorRectangle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ColorInfoSelected?.Invoke(this, _colorInfo);
        }

        private void ColorRectangle_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Panel.SetZIndex(this, 2);
        }

        private void ColorRectangle_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Panel.SetZIndex(this, 1);
        }

        private void CopyBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (FirstViewModel)((Canvas)Parent).DataContext;
            viewModel.CopyCommand.Execute(_colorInfo);
        }

        private void CopyArea_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (FirstViewModel)((Canvas)Parent).DataContext;
            viewModel.CopyAreaCommand.Execute(_colorInfo);
        }

        private void CopyRow_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (FirstViewModel)((Canvas)Parent).DataContext;
            viewModel.CopyRowCommand.Execute(_colorInfo);
        }

        private void CopyColumn_OnClick(object sender, RoutedEventArgs e)
        {
            var viewModel = (FirstViewModel)((Canvas)Parent).DataContext;
            viewModel.CopyColumnCommand.Execute(_colorInfo);
        }
    }
}
