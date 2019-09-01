using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using FrHello.NetLib.Core.Mvx;
using FrHello.NetLib.Core.Windows.Windows;
using Models;
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
            
        }

        private void RefreshScreens()
        {
            var screens = new List<ScreenInfo>();

            for (var i = 0; i < WindowsApi.ScreenApi.AllScreens.Length; i++)
            {
                screens.Add(new ScreenInfo
                {
                    Index = i,
                    Primary = WindowsApi.ScreenApi.AllScreens[i].Primary,
                    Rectangle = WindowsApi.ScreenApi.AllScreens[i].Bounds
                });
            }

            Screens = new ObservableCollection<ScreenInfo>(screens);
        }
    }
}
