using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CoreFx.ViewModels;
using FrHello.NetLib.Core.Mvx;
using ModelsFx;
using WpfViews.Controls;
using Point = System.Drawing.Point;

namespace WpfViews.Windows
{
    /// <summary>
    /// MainWindowView.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindowView
    {
        /// <summary>
        /// 用于复用的UI矩形
        /// </summary>
        private readonly ConcurrentQueue<ColorRectangle> _colorRectanglesCache = new ConcurrentQueue<ColorRectangle>();
        private readonly ConcurrentDictionary<Point, ColorInfo> _uniqueColorInfos = new ConcurrentDictionary<Point, ColorInfo>();
        private FirstViewModel _viewModel;

        private CancellationTokenSource _lastOperateTokenSource;

        private readonly Timer _mouseMoveTimer;

        public MainWindowView()
        {
            InitializeComponent();

            _mouseMoveTimer = new Timer(MouseMoveHandler);
        }

        private void MainWindowView_OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = (FirstViewModel) DataContext;
            _viewModel.ColorInfosChangedEvent += OnColorInfosChangedEvent;
        }

        private void OnColorInfosChangedEvent(object sender, List<ColorInfo> e)
        {
            _lastOperateTokenSource?.Cancel();
            _lastOperateTokenSource = new CancellationTokenSource();

            UiDispatcherHelper.Invoke(() =>
            {
                CacheColors(((FirstViewModel) sender).ColorInfos, _lastOperateTokenSource.Token);
            });
        }

        private void CacheColors(List<ColorInfo> colorInfos, CancellationToken cancellationToken = default)
        {
            try
            {
                RemoveAll();

                if (colorInfos != null && colorInfos.Any())
                {
                    foreach (var colorInfo in colorInfos)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        _uniqueColorInfos.TryAdd(colorInfo.Point, colorInfo);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e);
            }
        }

        private void Mask_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            //调整计时器，准备重新计算，停留1S开始绘制
            _mouseMoveTimer.Change(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        }

        private void MouseMoveHandler(object obj)
        {
            
        }

        private (double, double) GetLocationInCanvas(Canvas canvas, ColorInfo colorInfo)
        {
            int screenWidth;
            int screenHeight;

            if (_viewModel.ShowTaskBar)
            {
                screenWidth = colorInfo.ScreenInfo.Screen.Bounds.Width;
                screenHeight = colorInfo.ScreenInfo.Screen.Bounds.Height;
            }
            else
            {
                screenWidth = colorInfo.ScreenInfo.Screen.WorkingArea.Width;
                screenHeight = colorInfo.ScreenInfo.Screen.WorkingArea.Height;
            }

            var left = (canvas.ActualWidth / screenWidth) * colorInfo.Point.X;
            var right = (canvas.ActualHeight / screenHeight) * colorInfo.Point.Y;

            return (left, right);
        }

        private void Mask_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }

        private void RemoveAll()
        {
            foreach (var uniqueColorInfo in _uniqueColorInfos)
            {
                uniqueColorInfo.Value.UiElement = null;
            }

            _uniqueColorInfos.Clear();

            UiDispatcherHelper.Invoke(() =>
            {
                foreach (ColorRectangle maskChild in Mask.Children)
                {
                    _colorRectanglesCache.Enqueue(maskChild);
                }

                Mask.Children.Clear();
            });
        }
    }
}