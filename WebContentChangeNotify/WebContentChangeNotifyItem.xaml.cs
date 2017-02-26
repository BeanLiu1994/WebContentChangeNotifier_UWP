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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Core;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace WebContentChangeNotify
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    /// 
    public sealed partial class WebContentChangeNotifyItem : Page
    {
        public UrlContentChangeChecker CurrentChecker { get; private set; }

        public WebContentChangeNotifyItem()
        {
            this.InitializeComponent();
        }
        public void SelectChecker(string Id, string Url)
        {
            SelectChecker(new UrlContentChangeChecker(Id, new Uri(Url)));
            BindItemsSource();
        }

        public void SelectChecker(UrlContentChangeChecker UrlChecker)
        {
            CurrentChecker = UrlChecker;
            var view = ApplicationView.GetForCurrentView();
            if (UrlChecker.webURL.OriginalString.Count() > 24)
                view.Title = UrlChecker.id + "的快照情况 (" + UrlChecker.webURL.OriginalString.Substring(0, 24) + "...)";
            else
                view.Title = UrlChecker.id + "的快照情况 (" + UrlChecker.webURL.OriginalString + ")";
            BindItemsSource(false);
        }

        public async void BindItemsSource(bool ForceUpdate = true)
        {
            if (ForceUpdate)
                await CurrentChecker.CheckExistance();
            TimeLineContainer.ItemsSource = CurrentChecker.UrlContentSnapList;
            if (TimeLineContainer.Items.Count > 0)
                TimeLineContainer.SelectedIndex = 0;
        }

        private void ItemClicked(object sender, ItemClickEventArgs e)
        {

        }

        private void ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ListViewNow = (sender as ListView);
            var snap = (ListViewNow.ItemsSource as ObservableCollection<UrlContentSnap>).ElementAt(ListViewNow.SelectedIndex);
            WebContentShow.NavigateToString(snap.Content);
        }

        private async void GetSnap(object sender, TappedRoutedEventArgs e)
        {
            await CurrentChecker.CheckNow();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SelectChecker(e.Parameter as UrlContentChangeChecker);
            SystemNavigationManager.GetForCurrentView().BackRequested +=
                (sender, param) =>
                {
                    Frame rootFrame = Window.Current.Content as Frame;

                    if (rootFrame != null && rootFrame.CanGoBack)
                    {
                        param.Handled = true;
                        rootFrame.GoBack();
                        var view = ApplicationView.GetForCurrentView();
                        view.Title = "网页内容变化检测";
                    }
                };
            base.OnNavigatedTo(e);
        }
    }
}
