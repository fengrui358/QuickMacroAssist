using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using FrHello.NetLib.Core.Mvx;
using FrHello.NetLib.Core.Windows.Windows;
using ModelsFx;
using MvvmCross.Commands;

namespace CoreFx.ViewModels
{
    public class FirstViewModel : BaseViewModel
    {


        public ObservableCollection<ScreenInfo> Screens { get; set; }

        public MvxCommand CaptureCommand { get; }

        public ScreenInfo SelectedScreenInfo { get; set; }

        /// <summary>
        /// 数据变化会自动改变
        /// </summary>
        public bool IsChanged { get; set; }

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
            if (SelectedScreenInfo != null)
            {

            }
        }

        private void RefreshScreens()
        {
            var screens = new List<ScreenInfo>();

            for (var i = 0; i < WindowsApi.ScreenApi.AllScreens.Length; i++)
            {
                screens.Add(new ScreenInfo(i + 1, WindowsApi.ScreenApi.AllScreens[i]));
            }

            Screens = new ObservableCollection<ScreenInfo>(screens);
        }

        private void OnSelectedScreenInfoChanged()
        {
            CaptureCommandHandler();
        }
    }
}
