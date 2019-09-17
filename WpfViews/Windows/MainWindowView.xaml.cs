using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
using ModelsFx.Help;
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
        private System.Windows.Point? _startPaintPoint;
        private readonly System.Windows.Shapes.Rectangle _paintRectangle;

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

            _paintRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = 4,
                Height = 4,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                StrokeDashOffset = 0.2,
                Fill = (SolidColorBrush) Application.Current.TryFindResource("AccentColorBrush"),
                Opacity = 0.8
            };
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

            _viewModel.StartPaint = false;

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
                var screenshotDir =
                    new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots"));

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

                Clipboard.SetFileDropList(new StringCollection {fileName});
                PromptHelper.Instance.Prompt = fileName;
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
                        Fill = (SolidColorBrush) Application.Current.TryFindResource("AccentColorBrush"),
                        StrokeThickness = 2,
                        Stroke = new SolidColorBrush(Colors.Black),
                        Opacity = 0.3,
                        ContextMenu = new ContextMenu
                        {
                            ItemsSource = new List<MenuItem>
                            {
                                new MenuItem
                                {
                                    Header = "复制小图", Command = _viewModel.CopyTargetBitmapCommand,
                                    CommandParameter = _viewModel.SelectedBitmapInfo
                                },
                                new MenuItem
                                {
                                    Header = "复制代码", Command = _viewModel.CopyCodeCommand,
                                    CommandParameter = _viewModel.SelectedBitmapInfo
                                },
                                new MenuItem
                                {
                                    Header = "删除", Command = _viewModel.DeleteTargetBitmapCommand,
                                    CommandParameter = _viewModel.SelectedBitmapInfo
                                }
                            }
                        }
                    };

                    Canvas.SetLeft(uiRectangle, rectangle.Value.X * _canvasScreenWidthRatio);
                    Canvas.SetTop(uiRectangle, rectangle.Value.Y * _canvasScreenHeightRatio);
                    Sketchpad.Children.Add(uiRectangle);
                }
            });
        }

        private void StartPaintButton_OnChecked(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectedBitmapInfo = null;

            Canvas.SetLeft(_paintRectangle, -5);
            Canvas.SetTop(_paintRectangle, -5);
            Sketchpad.Children.Add(_paintRectangle);
        }

        private void StartButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _startPaintPoint = null;
            Sketchpad.Children.Clear();
            _paintRectangle.Width = 2;
            _paintRectangle.Height = 2;
        }

        private void Sketchpad_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel.StartPaint)
            {
                var point = Mouse.GetPosition(Sketchpad);

                if (point.X >= 0 && point.Y >= 0 && point.X <= Sketchpad.ActualWidth &&
                    point.Y <= Sketchpad.ActualHeight)
                {
                    if (_startPaintPoint == null)
                    {
                        Canvas.SetLeft(_paintRectangle, point.X - 2);
                        Canvas.SetTop(_paintRectangle, point.Y - 2);
                    }
                    else
                    {
                        var width = point.X - _startPaintPoint.Value.X;
                        var height = point.Y - _startPaintPoint.Value.Y;

                        if (width < 0)
                        {
                            Canvas.SetLeft(_paintRectangle, _startPaintPoint.Value.X + width);
                        }
                        else
                        {
                            Canvas.SetLeft(_paintRectangle, _startPaintPoint.Value.X);
                        }

                        if (height < 0)
                        {
                            Canvas.SetTop(_paintRectangle, _startPaintPoint.Value.Y + height);
                        }
                        else
                        {
                            Canvas.SetTop(_paintRectangle, _startPaintPoint.Value.Y);
                        }

                        _paintRectangle.Width = Math.Abs(width);
                        _paintRectangle.Height = Math.Abs(height);
                    }
                }
            }
        }

        private void Sketchpad_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.StartPaint)
            {
                var point = Mouse.GetPosition(Sketchpad);

                if (_startPaintPoint == null)
                {
                    if (point.X >= 0 && point.Y >= 0 && point.X <= Sketchpad.ActualWidth &&
                        point.Y <= Sketchpad.ActualHeight)
                    {
                        _startPaintPoint = point;
                    }
                }
                else
                {
                    var locationPoint = _startPaintPoint.Value;

                    var width = point.X - _startPaintPoint.Value.X;
                    var height = point.Y - _startPaintPoint.Value.Y;

                    if (width < 0)
                    {
                        locationPoint.X = _startPaintPoint.Value.X + width;
                    }
                    else
                    {
                        locationPoint.X = _startPaintPoint.Value.X;
                    }

                    if (height < 0)
                    {
                        locationPoint.Y = _startPaintPoint.Value.Y + height;
                    }
                    else
                    {
                        locationPoint.Y = _startPaintPoint.Value.Y;
                    }

                    var rectangle = new Rectangle((int) (locationPoint.X / _canvasScreenWidthRatio),
                        (int) (locationPoint.Y / _canvasScreenHeightRatio),
                        (int) Math.Abs(width / _canvasScreenWidthRatio),
                        (int) Math.Abs(height / _canvasScreenHeightRatio));

                    //保存图片
                    using (var bitmap = _viewModel.SelectedScreenInfo.CopyBitmap())
                    using (var subBitmap = bitmap.Clone(rectangle, bitmap.PixelFormat))
                    {
                        var path = BitmapHelper.SaveFile(subBitmap);

                        var targetBitmapInfo = new TargetBitmapInfo(path);
                        targetBitmapInfo.Init();
                        _viewModel.BitmapInfos.Add(targetBitmapInfo);
                        _viewModel.SelectedBitmapInfo = targetBitmapInfo;
                    }

                    //结束画图
                    _viewModel.StartPaint = false;
                }
            }
        }
    }
}