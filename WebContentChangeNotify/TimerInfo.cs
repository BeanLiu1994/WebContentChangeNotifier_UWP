using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using Microsoft.Toolkit.Uwp.Helpers;
using TileRefresh;

namespace WebContentChangeNotify
{
    public class TimerInfo : INotifyPropertyChanged
    {
        const string key = "TimerTask";
        const string key_enabled = "TimerTaskEnabled";

        public string Name { get { return key; } }

        private bool _Enabled;
        public bool Enabled
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key_enabled))
                    _Enabled = (bool)ApplicationData.Current.LocalSettings.Values[key_enabled];
                else
                    _Enabled = false;
                Debug.WriteLine("get: Enabled:" + _Enabled.ToString());
                return _Enabled;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values[key_enabled] = value;
                _Enabled = value;
                Debug.WriteLine("set: Enabled:" + value.ToString());
                PropertyChangeEventHappen(nameof(Enabled));
                UpdateWorkRegister();
            }
        }

        private uint _TimerSpan;
        public uint TimerSpan
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
                    _TimerSpan = (uint)(ApplicationData.Current.LocalSettings.Values[key]);
                else
                    _TimerSpan = 60;
                Debug.WriteLine("get: TimerSpan:" + _TimerSpan.ToString());
                return _TimerSpan;
            }
            set
            {
                _TimerSpan = value;
                ApplicationData.Current.LocalSettings.Values[key] = _TimerSpan;
                Debug.WriteLine("set: TimerSpan:" + value.ToString());
                PropertyChangeEventHappen(nameof(TimerSpan));
                UpdateWorkRegister();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void PropertyChangeEventHappen(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateWorkRegister()
        {
            var Timer_Condition = new IBackgroundCondition[]{
                new SystemCondition(SystemConditionType.FreeNetworkAvailable)
            };

            bool HasRegistered = BackgroundTaskHelper.IsBackgroundTaskRegistered(typeof(TileRefreshUtils));

            if (Enabled)
                BackgroundTaskHelper.Register(typeof(TileRefreshUtils), new TimeTrigger(TimerSpan, false), false, true, Timer_Condition);
            else if (HasRegistered)
                BackgroundTaskHelper.Unregister(typeof(TileRefreshUtils));
        }
    }
}
