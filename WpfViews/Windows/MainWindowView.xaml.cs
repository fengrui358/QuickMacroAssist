using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
using Size = System.Drawing.Size;

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

        private float _canvasScreenWidthRatio;
        private float _canvasScreenHeightRatio;

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
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FirstViewModel.Buffer) || e.PropertyName == nameof(FirstViewModel.UseBuffer) ||
                e.PropertyName == nameof(FirstViewModel.ShowTaskBar))
            {
                _lastOperateTokenSource?.Cancel();
            }

            if (e.PropertyName == nameof(FirstViewModel.ShowTaskBar) ||
                e.PropertyName == nameof(FirstViewModel.SelectedScreenInfo))
            {
                var screenSize = GetScreenSize();

                _canvasScreenWidthRatio = (float) Mask.ActualWidth / screenSize.Width;
                _canvasScreenHeightRatio = (float) Mask.ActualHeight / screenSize.Height;
            }
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
            //调整计时器，准备重新计算，停留20mS开始绘制
            _mouseMoveTimer.Change(TimeSpan.FromMilliseconds(20), Timeout.InfiniteTimeSpan);
        }

        private async void MouseMoveHandler(object obj)
        {
            if (CheckGoOn())
            {
                System.Windows.Point? point = null;

                await UiDispatcherHelper.InvokeAsync(() =>
                {
                    if (Mask.IsMouseOver)
                    {
                        if (!CheckGoOn())
                        {
                            return;
                        }

                        //获取鼠标位置
                        point = Mouse.GetPosition(Mask);
                    }
                });

                if (point != null)
                {
                    //查找屏幕长宽10%的矩形范围
                    var screenPoint = new PointF((float) point.Value.X / _canvasScreenWidthRatio,
                        (float) point.Value.Y / _canvasScreenHeightRatio);

                    var screenSize = GetScreenSize();

                    var rectangle = new Rectangle((int) (screenPoint.X - screenSize.Width * 0.1 / 2),
                        (int) (screenPoint.Y - screenSize.Height * 0.1 / 2), (int) (screenSize.Width * 0.1),
                        (int) (screenSize.Height * 0.1));

                    //需要呈现的点
                    var contains = _uniqueColorInfos.Where(s => rectangle.Contains(s.Key)).Select(s => s.Value)
                        .ToDictionary(s => s.Point);

                    if (!CheckGoOn())
                    {
                        return;
                    }

                    var tobeRemoves = new List<ColorRectangle>();
                    await UiDispatcherHelper.InvokeAsync(() =>
                    {
                        foreach (ColorRectangle maskChild in Mask.Children)
                        {
                            if (!CheckGoOn())
                            {
                                return;
                            }

                            var colorInfo = (ColorInfo) maskChild.Tag;

                            if (!contains.ContainsKey(colorInfo.Point))
                            {
                                tobeRemoves.Add(maskChild);
                            }
                            else
                            {
                                contains.Remove(colorInfo.Point);
                            }
                        }

                        foreach (var tobeRemove in tobeRemoves)
                        {
                            if (!CheckGoOn())
                            {
                                return;
                            }

                            Mask.Children.Remove(tobeRemove);
                            _colorRectanglesCache.Enqueue(tobeRemove);
                        }

                        //最多呈现10个元素
                        foreach (var keyValuePair in contains.Take(10))
                        {
                            if (!CheckGoOn())
                            {
                                return;
                            }

                            if (_colorRectanglesCache.TryDequeue(out var colorRectangle))
                            {
                                colorRectangle.UpdateColorInfo(keyValuePair.Value);
                            }
                            else
                            {
                                colorRectangle = new ColorRectangle(keyValuePair.Value);
                            }

                            colorRectangle.Tag = keyValuePair.Value;
                            var location = GetLocationInCanvas(Mask, keyValuePair.Value);

                            Canvas.SetLeft(colorRectangle, location.X);
                            Canvas.SetTop(colorRectangle, location.Y);

                            Mask.Children.Add(colorRectangle);
                        }
                    });
                }
            }
        }

        private bool CheckGoOn()
        {
            if (_lastOperateTokenSource == null || (_lastOperateTokenSource != null &&
                _lastOperateTokenSource.IsCancellationRequested))
            {
                return false;
            }

            return true;
        }

        private PointF GetLocationInCanvas(Canvas canvas, ColorInfo colorInfo)
        {
            var left = _canvasScreenWidthRatio * colorInfo.Point.X;
            var right = _canvasScreenHeightRatio * colorInfo.Point.Y;

            return new PointF(left, right);
        }

        private void Mask_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var screenSize = GetScreenSize();

            _canvasScreenWidthRatio = (float) Mask.ActualWidth / screenSize.Width;
            _canvasScreenHeightRatio = (float) Mask.ActualHeight / screenSize.Height;
        }

        private void RemoveAll()
        {
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

        private Size GetScreenSize()
        {
            if (_viewModel.ShowTaskBar)
            {
                return new Size(_viewModel.SelectedScreenInfo.Screen.Bounds.Width,
                    _viewModel.SelectedScreenInfo.Screen.Bounds.Height);
            }
            else
            {
                return new Size(_viewModel.SelectedScreenInfo.Screen.WorkingArea.Width,
                    _viewModel.SelectedScreenInfo.Screen.WorkingArea.Height);
            }
        }
    }
}
