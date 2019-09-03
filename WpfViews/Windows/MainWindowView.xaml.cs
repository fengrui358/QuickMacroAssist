using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreFx.ViewModels;
using FrHello.NetLib.Core.Mvx;
using ModelsFx;
using WpfViews.Controls;

namespace WpfViews.Windows
{
    /// <summary>
    /// MainWindowView.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindowView
    {
        private CancellationTokenSource _lastOperateTokenSource;

        public MainWindowView()
        {
            InitializeComponent();
        }

        private void MainWindowView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var viewModel = (FirstViewModel) DataContext;
            viewModel.ColorInfosChangedEvent += OnColorInfosChangedEvent;
        }

        private void OnColorInfosChangedEvent(object sender, List<ColorInfo> e)
        {
            UiDispatcherHelper.Invoke(() =>
            {
                _lastOperateTokenSource?.Cancel();
                _lastOperateTokenSource = new CancellationTokenSource();
                DrawingColors(((FirstViewModel)sender).ColorInfos, _lastOperateTokenSource.Token);
            });
        }

        private void DrawingColors(List<ColorInfo> colorInfos, CancellationToken cancellationToken = default)
        {
            try
            {
                Mask.Children.Clear();

                if (colorInfos != null && colorInfos.Any())
                {
                    var x = ScreenImage.ActualWidth;
                    var y = ScreenImage.ActualHeight;

                    foreach (var colorInfo in colorInfos)
                    {
                        //var colorRectangle = new ColorRectangle(colorInfo, Mask);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
