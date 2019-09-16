using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FrHello.NetLib.Core.Mvx;
using FrHello.NetLib.Core.Windows.Windows;
using ModelsFx;
using MvvmCross.Commands;
using Clipboard = System.Windows.Clipboard;

namespace CoreFx.ViewModels
{
    public class FirstViewModel : BaseViewModel
    {
        private CancellationTokenSource _lastOperateTokenSource;
        private CancellationTokenSource _lastMatchBitmap;

        public ObservableCollection<ScreenInfo> Screens { get; set; }

        public List<ColorInfo> ColorInfos { get; set; }

        public ObservableCollection<TargetBitmapInfo> BitmapInfos { get; set; } = new ObservableCollection<TargetBitmapInfo>();

        public MvxCommand CaptureCommand { get; }

        public ScreenInfo SelectedScreenInfo { get; set; }

        public ColorInfo SelectedColorInfo { get; set; }

        public TargetBitmapInfo SelectedBitmapInfo { get; set; }

        public Rectangle? SelectedBitmapRectangle { get; set; }

        public MvxCommand AddPictureFile { get; }

        public bool UseBuffer { get; set; }

        public int Buffer { get; set; } = 10;

        public bool ShowTaskBar { get; set; }

        public bool IsBusy { get; set; }

        public bool MouseEnter { get; set; }

        public int CopyBitmapToClipboardIndex { get; set; } = 1;

        public event EventHandler<List<ColorInfo>> ColorInfosChangedEvent;

        public MvxCommand<ColorInfo> CopyCommand { get; }
        public MvxCommand<ColorInfo> CopyAreaCommand { get; }
        public MvxCommand<ColorInfo> CopyRowCommand { get; }
        public MvxCommand<ColorInfo> CopyColumnCommand { get; }

        public FirstViewModel()
        {
            WindowsApi.ReceiveApiOperateLogEvent += (sender, s) => { Debug.WriteLine(s); };

            CaptureCommand = new MvxCommand(CaptureCommandHandler);
            CopyCommand = new MvxCommand<ColorInfo>(CopyCommandHandler);
            CopyAreaCommand = new MvxCommand<ColorInfo>(CopyAreaCommandHandler);
            CopyRowCommand = new MvxCommand<ColorInfo>(CopyRowCommandHandler);
            CopyColumnCommand = new MvxCommand<ColorInfo>(CopyColumnCommandHandler);
            AddPictureFile = new MvxCommand(AddPictureFileHandler);

            Task.Run(async () => { await Initialize(); });
        }

        public override async Task Initialize()
        {
            await Task.Run(RefreshScreens);
        }

        private void CaptureCommandHandler()
        {
            CaptureCommandHandler(true);
        }

        private void CopyCommandHandler(ColorInfo colorInfo)
        {
            var str =
                $"await WindowsApi.ScreenApi.WaitColorAt({colorInfo.Point.X}, {colorInfo.Point.Y}, Color.FromArgb({colorInfo.Color.R}, {colorInfo.Color.G}, {colorInfo.Color.B}));";

            Clipboard.SetDataObject(str);
        }

        private void CopyAreaCommandHandler(ColorInfo colorInfo)
        {
            var str =
                $"await WindowsApi.ScreenApi.WaitScanColorLocation(Color.FromArgb({colorInfo.Color.R}, {colorInfo.Color.G}, {colorInfo.Color.B}), WindowsApi.ScreenApi.AllScreens[{SelectedScreenInfo.Index}], new Rectangle({colorInfo.Point.X - Buffer / 2}, {colorInfo.Point.Y - Buffer / 2}, {Buffer}, {Buffer}));";

            Clipboard.SetDataObject(str);
        }

        private void CopyRowCommandHandler(ColorInfo colorInfo)
        {
            var str =
                $"await WindowsApi.ScreenApi.WaitScanColorLocation(Color.FromArgb({colorInfo.Color.R}, {colorInfo.Color.G}, {colorInfo.Color.B}), WindowsApi.ScreenApi.AllScreens[{SelectedScreenInfo.Index}], WindowsApi.ScreenApi.AllScreens[{SelectedScreenInfo.Index}].GetScreenRow({colorInfo.Point.Y - Buffer / 2}, {Buffer}));";

            Clipboard.SetDataObject(str);
        }

        private void CopyColumnCommandHandler(ColorInfo colorInfo)
        {
            var str =
                $"await WindowsApi.ScreenApi.WaitScanColorLocation(Color.FromArgb({colorInfo.Color.R}, {colorInfo.Color.G}, {colorInfo.Color.B}), WindowsApi.ScreenApi.AllScreens[{SelectedScreenInfo.Index}], WindowsApi.ScreenApi.AllScreens[{SelectedScreenInfo.Index}].GetScreenColumn({colorInfo.Point.X - Buffer / 2}, {Buffer}));";

            Clipboard.SetDataObject(str);
        }

        private async void CaptureCommandHandler(bool notify)
        {
            if (SelectedScreenInfo != null)
            {
                try
                {
                    IsBusy = true;

                    SelectedScreenInfo.CaptureProcess(ShowTaskBar);

                    _lastOperateTokenSource?.Cancel();
                    _lastOperateTokenSource = new CancellationTokenSource();

                    ColorInfos = new List<ColorInfo>();
                    ColorInfos = (await SelectedScreenInfo.ScanAllUniqueColors(_lastOperateTokenSource.Token)).ToList();
                }
                catch (OperationCanceledException canceledException)
                {
                    Debug.WriteLine(canceledException);
                }
                finally
                {
                    IsBusy = false;
                }
            }

            if (notify)
            {
                await RaisePropertyChanged(nameof(SelectedScreenInfo));
            }
            
            ColorInfosChangedEvent?.Invoke(this, ColorInfos.ToList());
        }

        private void RefreshScreens()
        {
            var screens = new List<ScreenInfo>();

            for (var i = 0; i < WindowsApi.ScreenApi.AllScreens.Length; i++)
            {
                screens.Add(new ScreenInfo(i, WindowsApi.ScreenApi.AllScreens[i]));
            }

            Screens = new ObservableCollection<ScreenInfo>(screens);
        }

        private void AddPictureFileHandler()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Bitmap (*.bmp)|*.bmp",
                Multiselect = false
            };

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (dialog.FileNames.Any())
                {
                    var targetBitmap = new TargetBitmapInfo(dialog.FileName);
                    targetBitmap.Init();

                    BitmapInfos.Add(targetBitmap);
                }
            }
        }

        private void OnSelectedScreenInfoChanged()
        {
            CaptureCommandHandler(false);
            OnSelectedBitmapInfoChanged();
        }

        private async void OnSelectedBitmapInfoChanged()
        {
            if (SelectedScreenInfo != null)
            {
                _lastMatchBitmap?.Cancel();
                _lastMatchBitmap = new CancellationTokenSource();

                if (SelectedBitmapInfo != null)
                {
                    using (var bitmap = SelectedScreenInfo.CopyBitmap())
                    {
                        var rectangle =
                            await SelectedBitmapInfo?.Match(bitmap, _lastMatchBitmap.Token);

                        SelectedBitmapRectangle = rectangle;
                    }
                }
            }
        }

        private void OnShowTaskBarChanged()
        {
            CaptureCommandHandler(true);
        }
    }
}
