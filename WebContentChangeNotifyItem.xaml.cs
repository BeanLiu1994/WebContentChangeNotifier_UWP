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
using System.Collections.ObjectModel;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace WebContentChangeNotify
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    /// 
    public sealed partial class WebContentChangeNotifyItem : Page
    {
        public UrlContentChangeChecker CurrentChecker { get; protected set; }
        public WebContentChangeNotifyItem()
        {
            this.InitializeComponent();
            SelectChecker();
            BindItemsSource();
        }
        public void SelectChecker()
        {
            UrlContentChangeChecker test = new UrlContentChangeChecker("test", new Uri("https://ibug.doc.ic.ac.uk/resources/lsfm/"));
            CurrentChecker = test;
        }

        public async void BindItemsSource()
        {
            await CurrentChecker.CheckExistance();
            TimeLineContainer.ItemsSource = CurrentChecker.UrlContentSnapList;
            if(TimeLineContainer.Items.Count>0)
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

        private void GetSnap(object sender, TappedRoutedEventArgs e)
        {
            CurrentChecker.CheckNow();
        }
    }
}
