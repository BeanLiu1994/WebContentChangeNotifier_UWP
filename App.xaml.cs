using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TileRefresh;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace WebContentChangeNotify
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// 在应用程序由最终用户正常启动时进行调用。
        /// 将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Frame rootFrame = Window.Current.Content as Frame;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += OnNavigated;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // 当导航堆栈尚未还原时，导航到第一页，
                    // 并通过将所需信息作为导航参数传入来配置
                    // 参数
                    rootFrame.Navigate(typeof(WebContentChangeNotifyView), e.Arguments);
                }
                // 确保当前窗口处于活动状态
                Window.Current.Activate();
                DetectLiveTileTask();
            }
        }
        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
        }

        /// <summary>
        /// 导航到特定页失败时调用
        /// </summary>
        ///<param name="sender">导航失败的框架</param>
        ///<param name="e">有关导航失败的详细信息</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }


        public async static void RegisterWorks(TimerInfo TimerInfoSaved = null)
        {
            var Timer_Condition = new IBackgroundCondition[]{
                new SystemCondition(SystemConditionType.FreeNetworkAvailable)
            };

            if(TimerInfoSaved==null)
                TimerInfoSaved = new TimerInfo();

            if (TimerInfoSaved.Enabled)
                await RegisterLiveTileTask(
                    TimerInfoSaved.Name,
                    typeof(TileRefreshUtils).FullName,
                    new TimeTrigger(TimerInfoSaved.TimerSpan, false),
                    Timer_Condition
                );
            else
                await RegisterLiveTileTask(
                    TimerInfoSaved.Name,
                    typeof(TileRefreshUtils).FullName,
                    null,
                    null
                );
        }
        public static async Task DetectLiveTileTask()
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.Unspecified || status == BackgroundAccessStatus.Denied)
            {
                return;
            }
            var CurrentTimerTask = new TimerInfo();
            foreach (var t in BackgroundTaskRegistration.AllTasks)
            {
                Debug.WriteLine("检测到了名为" + t.Value.Name + "的后台任务");
            }
            if(CurrentTimerTask.Enabled)
            { 
                var TaskMatch = BackgroundTaskRegistration.AllTasks.Where(m => { return m.Value.Name == CurrentTimerTask.Name; });
                if(TaskMatch.Count()==0)
                {
                    Debug.WriteLine("没有检测到名为" + CurrentTimerTask.Name + "的后台任务,更新记录");
                    CurrentTimerTask.Enabled = false;
                }
            }
        }
        //LiveTileSetting
        public static async Task RegisterLiveTileTask(string _Name, string _TaskEntryPoint, IBackgroundTrigger _Trigger, IBackgroundCondition[] _ConditionTable)
        {
            //建立builder
            var taskBuilder = new BackgroundTaskBuilder
            {
                Name = _Name,
                TaskEntryPoint = _TaskEntryPoint
            };
            //清除已有的
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.Unspecified || status == BackgroundAccessStatus.Denied)
            {
                return;
            }
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.Clear();

            foreach (var t in BackgroundTaskRegistration.AllTasks)
            {
                if (t.Value.Name == taskBuilder.Name)
                {
                    t.Value.Unregister(true);
                }
            }
            //如果Trigger为null撤销这个后台任务
            if (_Trigger == null) return;

            //继续构建builder
            taskBuilder.SetTrigger(_Trigger);

            if (_ConditionTable != null)
                foreach (var m in _ConditionTable)
                {
                    taskBuilder.AddCondition(m);
                }

            //注册
            taskBuilder.Register();
            Debug.WriteLine("注册了名为" + _Name + "的后台任务");
        }
    }
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
                Debug.WriteLine("set: Enabled:"+value.ToString());
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
            App.RegisterWorks(this);
        }
    }
}
