using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private readonly ConcurrentDictionary<Point, ColorInfo> _uniqueColorInfos =
            new ConcurrentDictionary<Point, ColorInfo>();

        private FirstViewModel _viewModel;

        private float _canvasScreenWidthRatio;
        private float _canvasScreenHeightRatio;

        private CancellationTokenSource _lastOperateTokenSource;

        private readonly Timer _mouseMoveTimer;

        public MainWindowView()
        {
            InitializeComponent();

            _mouseMoveTimer = new Timer(MouseMoveHandler);
            ColorRectangle.ColorInfoSelected += ColorRectangleOnColorInfoSelected;
        }

        private void ColorRectangleOnColorInfoSelected(object sender, ColorInfo e)
        {
            _viewModel.SelectedColorInfo = e;
            ColorInfosListBox.ScrollIntoView(e);
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

            if (e.PropertyName == nameof(FirstViewModel.SelectedBitmapRectangle))
            {
                OnSelectedBitmapRectangleChanged();
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

            SetMaskMouseBoundsRect();
        }

        private void SetMaskMouseBoundsRect()
        {
            var point = Mouse.GetPosition(Mask);

            //需要呈现的点
            MaskMouseBoundsRect.Width = Mask.ActualWidth * 0.1;
            MaskMouseBoundsRect.Height = Mask.ActualHeight * 0.1;

            Canvas.SetLeft(MaskMouseBoundsRect, point.X - Mask.ActualWidth * 0.1 / 2);
            Canvas.SetTop(MaskMouseBoundsRect, point.Y - Mask.ActualHeight * 0.1 / 2);
        }

        private async void MouseMoveHandler(object obj)
        {
            _lastOperateTokenSource?.Cancel();
            _lastOperateTokenSource = new CancellationTokenSource();

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
                        var maxCount = 500;

                        foreach (var keyValuePair in contains.Take(maxCount))
                        {
                            if (!CheckGoOn() || Mask.Children.Count >= maxCount)
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

                            Panel.SetZIndex(colorRectangle, 1);
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

            _canvasScreenWidthRatio = (float) ScreenImage.ActualWidth / screenSize.Width;
            _canvasScreenHeightRatio = (float) ScreenImage.ActualHeight / screenSize.Height;

            OnSelectedBitmapRectangleChanged();
        }

        private void RemoveAll()
        {
            _uniqueColorInfos.Clear();

            UiDispatcherHelper.Invoke(() =>
            {
                foreach (ColorRectangle maskChild in Mask.Children)
                {
                    maskChild.Tag = null;
                    _colorRectanglesCache.Enqueue(maskChild);
                }

                Mask.Children.Clear();
            });
        }

        private Size GetScreenSize()
        {
            if (_viewModel == null)
            {
                return Size.Empty;
            }

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

        private void UIElement_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.MouseEnter = true;
            }
        }

        private void UIElement_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (_viewModel != null)
            {
                var point = Mouse.GetPosition(Mask);
                if (!(point.X >= 0 && point.Y >= 0 && point.X <= Mask.Width && point.Y <= Mask.Height))
                {
                    _viewModel.MouseEnter = false;
                    _lastOperateTokenSource?.Cancel();

                    foreach (ColorRectangle maskChild in Mask.Children)
                    {
                        maskChild.Tag = null;
                        _colorRectanglesCache.Enqueue(maskChild);
                    }

                    Mask.Children.Clear();
                }
            }
        }

        private void CopyOriginalPicture_OnClick(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedScreenInfo != null)
            {
                var screenshotDir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots"));

                if (screenshotDir.Exists)
                {
                    screenshotDir.Delete(true);
                    screenshotDir.Create();
                }
                else
                {
                    screenshotDir.Create();
                }

                var fileName = Path.Combine(screenshotDir.FullName,
                    $"W{_viewModel.SelectedScreenInfo.Rectangle.Width}H{_viewModel.SelectedScreenInfo.Rectangle.Height}I{_viewModel.CopyBitmapToClipboardIndex++}.bmp");

                using (var bitmap = _viewModel.SelectedScreenInfo.CopyBitmap())
                {
                    bitmap.Save(fileName);
                }

                Clipboard.SetFileDropList(new StringCollection { fileName });
            }
        }

        private void OnSelectedBitmapRectangleChanged()
        {
            var rectangle = _viewModel.SelectedBitmapRectangle;

            UiDispatcherHelper.Invoke(() =>
            {
                Sketchpad.Children.Clear();

                if (rectangle != null)
                {
                    var uiRectangle = new System.Windows.Shapes.Rectangle
                    {
                        Width = rectangle.Value.Width * _canvasScreenWidthRatio,
                        Height = rectangle.Value.Height * _canvasScreenHeightRatio,
                        Fill = new SolidColorBrush(Colors.Blue),
                        StrokeThickness = 2,
                        Stroke = new SolidColorBrush(Colors.Black),
                        Opacity = 0.3
                    };

                    Canvas.SetLeft(uiRectangle, rectangle.Value.X * _canvasScreenWidthRatio);
                    Canvas.SetTop(uiRectangle, rectangle.Value.Y * _canvasScreenHeightRatio);
                    Sketchpad.Children.Add(uiRectangle);
                }
            });
        }
    }
}