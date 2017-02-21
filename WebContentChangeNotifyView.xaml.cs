﻿using System;
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

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace WebContentChangeNotify
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WebContentChangeNotifyView : Page
    {
        UrlContentChangeCheckerManager CurrentManager;
        public WebContentChangeNotifyView()
        {
            this.InitializeComponent();
            CurrentManager = new UrlContentChangeCheckerManager();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        private async void Init(object sender, RoutedEventArgs e)
        {
            await CurrentManager.Init();
            await CurrentManager.CheckAll();
            MainList.ItemsSource = CurrentManager.UrlContentCheckerList;
        }

        private async void UpdateButtonClicked(object sender, TappedRoutedEventArgs e)
        {
            await CurrentManager.CheckAll();
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
    }
}
