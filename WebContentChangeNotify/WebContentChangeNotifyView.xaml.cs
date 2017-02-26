using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WebContentChangeCheckerUtil;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.Storage;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Diagnostics;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace WebContentChangeNotify
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WebContentChangeNotifyView : Page
    {
        UrlContentChangeCheckerManager CurrentManager;
        public TimerInfo CurrentTimerInfo { get; private set; }
        public string ApplicationName => $"{Windows.ApplicationModel.Package.Current.DisplayName}";
        public string ApplicationVersion => $"版本:        {SystemInformation.ApplicationVersion.Major}.{SystemInformation.ApplicationVersion.Minor}.{SystemInformation.ApplicationVersion.Build} [{SystemInformation.OperatingSystemArchitecture}]";
        public string InstallDatetime =>    $"安装日期: { Windows.ApplicationModel.Package.Current.InstalledDate.DateTime}";

        public WebContentChangeNotifyView()
        {
            this.InitializeComponent();
            var view = ApplicationView.GetForCurrentView();
            view.Title = "网页内容变化检测";
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Init();
        }

        private Task Init()
        { 
            CurrentManager = new UrlContentChangeCheckerManager();
            MainList.ItemsSource = CurrentManager.UrlContentCheckerList;
            var task = CurrentManager.Init();
            return task;
        }

        private async void UpdateButtonClicked(object sender, TappedRoutedEventArgs e)
        {
            //await CurrentManager.CheckAll();
            foreach(var t in CurrentManager.CheckAll_NoWait())
                await t;
        }

        private void CheckerItemClicked(object sender, ItemClickEventArgs e)
        {
            (Window.Current.Content as Frame).Navigate(typeof(WebContentChangeNotifyItem), CurrentManager.UrlContentCheckerList.ElementAt((sender as GridView).Items.IndexOf(e.ClickedItem)));
        }

        private async void AddButtonClicked(object sender, TappedRoutedEventArgs e)
        {
            if (!await CurrentManager.AddItem(UI_id.Text, UI_url.Text))
            {
                var dialog = new MessageDialog("Id已存在或者url格式错误", "消息提示");
                dialog.ShowAsync();
            }
            else
            {
                UI_id.Text = "";
                UI_url.Text = "";
            }

        }
        private async void ReloadButtonClicked(object sender, TappedRoutedEventArgs e)
        {
            await CurrentManager.Init();
            PageLoaded(null, null);
        }

        private async void ItemRefreshClicked(object sender, TappedRoutedEventArgs e)
        {
            var DataContext = (sender as AppBarButton).DataContext as UrlContentChangeChecker;
            await DataContext.CheckNow();
        }

        private void ItemDeleteClicked(object sender, TappedRoutedEventArgs e)
        {
            var DataContext = (sender as AppBarButton).DataContext as UrlContentChangeChecker;
            CurrentManager.DeleteItem(DataContext.id);
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            CurrentTimerInfo = new TimerInfo();
            TimerSelectorEnable.SetBinding(ToggleSwitch.IsOnProperty, new Binding() { Mode = BindingMode.TwoWay, Source = CurrentTimerInfo, Path=new PropertyPath("Enabled") });

            var CurrentTimerSpan = (CurrentTimerInfo.TimerSpan);
            var SelectedNow = TimerSpanSelector.Items.Where(m=> { return double.Parse((m as ComboBoxItem).Tag as string) == CurrentTimerSpan; });
            if (SelectedNow.Count() == 0)
                TimerSpanSelector.SelectedIndex = 3;
            else
                TimerSpanSelector.SelectedItem = SelectedNow.First();

            TimerSpanSelector.SelectionChanged += TimerSpanChanged;
        }

        private void TimerSpanChanged(object sender, SelectionChangedEventArgs e)
        {
            var Selector = sender as ComboBox;
            CurrentTimerInfo.TimerSpan = uint.Parse((Selector.SelectedItem as ComboBoxItem).Tag as string);
        }



        private async void OpenStore(object sender, TappedRoutedEventArgs e)
        {
            var uri = new Uri("ms-windows-store://pdp/?ProductId=9NBLGGH5JDWG");
            await Windows.System.Launcher.LaunchUriAsync(uri);
            Debug.WriteLine("打开了商店页面");
        }

        private async void OpenReview(object sender, TappedRoutedEventArgs e)
        {
            var uri = new Uri("ms-windows-store://review/?ProductId=9NBLGGH5JDWG");
            await Windows.System.Launcher.LaunchUriAsync(uri);
            Debug.WriteLine("打开了商店页面");
        }

    }
}
