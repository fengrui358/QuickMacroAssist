using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrHello.NetLib.Core.Mvx;
using FrHello.NetLib.Core.Windows.Windows;
using ModelsFx;
using MvvmCross.Commands;

namespace CoreFx.ViewModels
{
    public class FirstViewModel : BaseViewModel
    {
        private CancellationTokenSource _lastOperateTokenSource;

        public ObservableCollection<ScreenInfo> Screens { get; set; }

        public List<ColorInfo> ColorInfos { get; set; }

        public MvxCommand CaptureCommand { get; }

        public ScreenInfo SelectedScreenInfo { get; set; }

        public bool UseBuffer { get; set; }

        public int Buffer { get; set; } = 10;

        public bool ShowTaskBar { get; set; }

        public event EventHandler<List<ColorInfo>> ColorInfosChangedEvent;

        public FirstViewModel()
        {
            CaptureCommand = new MvxCommand(CaptureCommandHandler);

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

        private async void CaptureCommandHandler(bool notify)
        {
            if (SelectedScreenInfo != null)
            {
                try
                {
                    SelectedScreenInfo.CaptureProcess(ShowTaskBar);

                    _lastOperateTokenSource?.Cancel();
                    _lastOperateTokenSource = new CancellationTokenSource();

                    ColorInfos = new List<ColorInfo>();
                    ColorInfos = await SelectedScreenInfo.ScanAllUniqueColors(_lastOperateTokenSource.Token);
                }
                catch (OperationCanceledException canceledException)
                {
                    Debug.WriteLine(canceledException);
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

        private void OnSelectedScreenInfoChanged()
        {
            CaptureCommandHandler(false);
        }

        private void OnShowTaskBarChanged()
        {
            CaptureCommandHandler(true);
        }
    }
}
