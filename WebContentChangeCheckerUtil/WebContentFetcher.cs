using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebContentChangeCheckerUtil
{
    class WebContentFetcher
    {
        Windows.UI.Xaml.Controls.WebView WebViewInstance;
        public string HtmlContent { get; protected set; }
        public bool Fetching { get; protected set; }
        public bool Navigating { get; protected set; }
        public WebContentFetcher()
        {
            WebViewInstance = new Windows.UI.Xaml.Controls.WebView();
            //WebViewInstance.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            WebViewInstance.NavigationStarting += 
                async (sender, e) =>
                {
                    Debug.WriteLine("Starting NavigationStarting");
                    Navigating = true;
                    await Task.Delay(4000);
                    if (Navigating)
                    {
                        Debug.WriteLine("Time exceeds");
                        e.Cancel = true;
                        Navigating = false;
                        Fetching = false;
                        HtmlContent = "";
                    }
                    Debug.WriteLine("End NavigationStarting");
                };
            WebViewInstance.NavigationCompleted += 
                async (sender, e) =>
                {
                    Debug.WriteLine("Starting NavigationCompleted");
                    Navigating = false;
                    HtmlContent = await WebViewInstance.InvokeScriptAsync("eval", new string[1] { "document.doctype" });
                    HtmlContent += await WebViewInstance.InvokeScriptAsync("eval", new string[1] { "document.documentElement.outerHTML" });
                    if (!e.IsSuccess)
                        await Task.Delay(500);
                    Fetching = false;
                    Debug.WriteLine("End NavigationCompleted");
                };
        }
        public async Task<string> FetchContent(Uri url)
        {
            //webURL = url;
            //ShowWebView();
            Fetching = true;
            WebViewInstance.Navigate(url);
            while (Fetching)
                await Task.Delay(50);
            return HtmlContent;
        }
        //Uri webURL;
        //public async void ShowWebView()
        //{
        //    var nowView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
        //    var newView = Windows.ApplicationModel.Core.CoreApplication.CreateNewView();
        //    await newView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
        //    {
        //        var newWindow = Windows.UI.Xaml.Window.Current;
        //        var newAppView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
        //        var frame = new Windows.UI.Xaml.Controls.Frame();
        //        frame.Navigate(typeof(WebContentChangeNotify.MainPage));
        //        var MainGrid = (frame.Content as Windows.UI.Xaml.Controls.Page).Content as Windows.UI.Xaml.Controls.Grid;
        //        MainGrid.Children.Add(new Windows.UI.Xaml.Controls.AppBar());
        //        var wb = new Windows.UI.Xaml.Controls.WebView(); wb.Navigate(webURL);
        //        MainGrid.Children.Add(wb);

        //        newWindow.Content = frame;
        //        newWindow.Activate();
        //        await Windows.UI.ViewManagement.ApplicationViewSwitcher.TryShowAsStandaloneAsync(
        //            newAppView.Id,
        //            Windows.UI.ViewManagement.ViewSizePreference.UseMinimum,
        //            nowView.Id,
        //            Windows.UI.ViewManagement.ViewSizePreference.UseMinimum
        //            );
        //    });
        //}
    }
}
