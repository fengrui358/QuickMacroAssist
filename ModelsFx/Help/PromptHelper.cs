using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using FrHello.NetLib.Core.ClassFoundation;

namespace ModelsFx.Help
{
    public class PromptHelper : Singleton<PromptHelper>, INotifyPropertyChanged
    {
        private readonly Timer _timer;

        private bool _show;

        public bool Show
        {
            get => _show;
            set
            {
                if (_show != value)
                {
                    _show = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _prompt;

        public string Prompt
        {
            get => _prompt;
            set
            {
                if (_prompt != value)
                {
                    _prompt = value;

                    Show = true;
                    _timer.Change(TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);

                    OnPropertyChanged();
                }
            }
        }

        public PromptHelper()
        {
            _timer = new Timer(DisabledShow);
        }

        private void DisabledShow(object state)
        {
            _prompt = string.Empty;
            Show = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
