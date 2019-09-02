using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        public FirstViewModel()
        {
            CaptureCommand = new MvxCommand(CaptureCommandHandler);

            Task.Run(async () => { await Initialize(); });
        }

        public override async Task Initialize()
        {
            await Task.Run(RefreshScreens);
        }

        private async void CaptureCommandHandler()
        {
            if (SelectedScreenInfo != null)
            {
                try
                {
                    SelectedScreenInfo.CaptureProcess();

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

            await RaisePropertyChanged(nameof(SelectedScreenInfo));
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
            CaptureCommandHandler();
        }
    }
}
