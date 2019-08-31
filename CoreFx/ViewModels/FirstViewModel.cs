using System.Collections.Generic;
using FrHello.NetLib.Core.Mvx;
using Models;
using MvvmCross.Commands;

namespace CoreFx.ViewModels
{
    public class FirstViewModel : BaseViewModel
    {
        public List<ScreenInfo> Screens { get; set; }

        public MvxCommand CaptureCommand { get; }

        /// <summary>
        /// 数据变化会自动改变
        /// </summary>
        public bool IsChanged { get; set; }

        public FirstViewModel()
        {
            CaptureCommand = new MvxCommand(CaptureCommandHandler);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();

            RefreshScreens();
        }

        private void CaptureCommandHandler()
        {
            
        }

        private void RefreshScreens()
        {
            
        }
    }
}
