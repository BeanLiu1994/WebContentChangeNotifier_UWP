using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Toasts;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace WebContentChangeCheckerUtil
{
    public class Nullable_boolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as bool?).GetValueOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value as bool?);
        }
    }
    public class Nullable_boolRevertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(value as bool?);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(value as bool?);
        }
    }

    public enum SaveMode {
        Update,Normal
    };
    public class UrlContentSnap : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void PropertyChangeEventHappen(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _path;
        public string path
        {
            get { return _path; }
            set
            {
                _path = value;
                ContentFile = Path.GetFileName(path) + "_web.html";
                PropertyChangeEventHappen(nameof(path));
            }
        }

        private string _url;
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                PropertyChangeEventHappen(nameof(Url));
            }
        }
        
        private ObservableCollection<DateTime> _TimeStamp;
        public ObservableCollection<DateTime> TimeStamp
        {
            get { return _TimeStamp; }
            set
            {
                _TimeStamp = value;
                PropertyChangeEventHappen(nameof(TimeStamp));
            }
        }

        private bool _ShrinkUI;
        public bool ShrinkUI
        {
            get { return _ShrinkUI; }
            set
            {
                _ShrinkUI = value;
                PropertyChangeEventHappen(nameof(ShrinkUI));
                PropertyChangeEventHappen(nameof(TimeStampShowOut));
            }
        }

        
        public ObservableCollection<DateTime> TimeStampShowOut
        {
            get {
                if (ShrinkUI && TimeStamp.Count > 1)
                    return new ObservableCollection<DateTime>() { TimeStamp.First(), TimeStamp.Last() };
                else
                    return TimeStamp;
            }
        }

        public string ContentFile { get; protected set; }
        public string Content { get; set; }

        public UrlContentSnap()
        {
            ShrinkUI = true;
            TimeStamp = new ObservableCollection<DateTime>();
        }

        // 仅根据 path 和 ContentFile 读取文件
        public async Task<bool> LoadFromFile()
        {
            var Folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path));
            if (Folder == null) return false;
            var File = await Folder.GetFileAsync(Path.GetFileName(path));
            if (File == null) return false;
            var _ContentFile = await Folder.GetFileAsync(ContentFile);
            if (_ContentFile == null) return false;
            TimeStamp.Clear();
            string FileContent;
            using (Stream file = await File.OpenStreamForReadAsync())
            {
                using (StreamReader read = new StreamReader(file))
                {
                    FileContent = read.ReadToEnd();
                }
            }

            using (StringReader strRdr = new StringReader(FileContent))
            {
                //通过XmlReader.Create静态方法创建XmlReader实例
                using (XmlReader rdr = XmlReader.Create(strRdr))
                {
                    //循环Read方法直到文档结束
                    while (rdr.Read())
                    {
                        //如果是开始节点
                        if (rdr.NodeType == XmlNodeType.Element)
                        {
                            //通过rdr.Name得到节点名
                            string elementName = rdr.Name;
                            if (elementName == "Url")
                            {
                                //读取到节点内文本内容
                                if (rdr.Read())
                                {
                                    //通过rdr.Value获得文本内容
                                    Url = rdr.Value;
                                }
                            }
                            if (elementName == "ContentFile")
                            {
                                if (rdr.Read())
                                {
                                    if (string.Compare(ContentFile, rdr.Value) != 0)
                                        Debug.WriteLine("读取的内容和保存的内容不符合");
                                }
                            }
                            if (elementName == "TimeStampItem")
                            {
                                if (rdr.Read())
                                {                                    
                                    TimeStamp.Add(DateTime.ParseExact(rdr.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                                }
                            }
                        }
                    }
                }
            }

            using (Stream file = await _ContentFile.OpenStreamForReadAsync())
            {
                using (StreamReader read = new StreamReader(file))
                {
                    Content = read.ReadToEnd();
                }
            }
            return true;
        }
        public async Task<bool> SaveToFile(SaveMode mode = SaveMode.Normal)
        {
            var Folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(path));
            if (Folder == null) return false;
            StorageFile File, _ContentFile;
            try
            {
                File = await Folder.GetFileAsync(Path.GetFileName(path));
            }
            catch
            {
                File = await Folder.CreateFileAsync(Path.GetFileName(path));
            }
            try
            {
                _ContentFile = await Folder.GetFileAsync(ContentFile);
            }
            catch
            {
                _ContentFile = await Folder.CreateFileAsync(ContentFile);
            }

            string FileContent;
            using (MemoryStream ms = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                //要求缩进
                settings.Indent = true;
                //注意如果不设置encoding默认将输出utf-16
                //注意这儿不能直接用Encoding.UTF8如果用Encoding.UTF8将在输出文本的最前面添加4个字节的非xml内容
                settings.Encoding = new UTF8Encoding(false);

                //设置换行符
                settings.NewLineChars = Environment.NewLine;

                using (XmlWriter xmlWriter = XmlWriter.Create(ms, settings))
                {

                    //写xml文件开始<?xml version="1.0" encoding="utf-8" ?>
                    xmlWriter.WriteStartDocument(false);
                    //写根节点
                    xmlWriter.WriteStartElement("root");

                    //通过WriteElementString可以添加一个节点同时添加节点内容
                    xmlWriter.WriteElementString("Url", Url);
                    xmlWriter.WriteElementString("ContentFile", ContentFile);

                    xmlWriter.WriteStartElement("TimeStamp");
                    foreach (var item in TimeStamp)
                    {
                        xmlWriter.WriteStartElement("TimeStampItem");
                        xmlWriter.WriteString(item.ToString("yyyy-MM-dd HH:mm:ss"));
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                }

                //将xml内容输出到控制台中
                FileContent = Encoding.UTF8.GetString(ms.ToArray());
            }

            using (Stream file = await File.OpenStreamForWriteAsync())
            {
                using (StreamWriter write = new StreamWriter(file))
                {
                    write.Write(FileContent);
                }
            }

            if (mode == SaveMode.Normal)
                using (Stream file = await _ContentFile.OpenStreamForWriteAsync())
                {
                    using (StreamWriter write = new StreamWriter(file))
                    {
                        write.Write(Content);
                    }
                }

            return true;
        }
    }

    public class UrlContentChangeChecker:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void PropertyChangeEventHappen(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private bool _Updating;
        public bool Updating { get { return _Updating; } protected set { _Updating = value; PropertyChangeEventHappen(nameof(Updating)); } }

        private string _id;
        public string id { get { return _id; } protected set { _id = value;PropertyChangeEventHappen(nameof(id)); } }

        private Uri _webURL;
        public Uri webURL { get { return _webURL; } protected set { _webURL = value; PropertyChangeEventHappen(nameof(webURL)); } }

        private string _recentStamp;
        public string recentStamp { get { return _recentStamp; } protected set { _recentStamp = value; PropertyChangeEventHappen(nameof(recentStamp)); } }

        private bool? _IsActivated;
        public bool? IsActivated
        {
            get { return _IsActivated; }
            set { _IsActivated = value; PropertyChangeEventHappen(nameof(IsActivated)); SaveUrlInfo(); }
        }

        public ObservableCollection<UrlContentSnap> UrlContentSnapList { get; protected set; }

        protected StorageFolder localStorageFolder;

        public UrlContentChangeChecker(string _id)
        {
            init();
            id = _id;
        }
        public UrlContentChangeChecker(string _id, Uri _webURL)
        {
            init();
            id = _id;
            webURL = _webURL;
        }
        protected void init()
        {
            UrlContentSnapList = new ObservableCollection<UrlContentSnap>();
            _IsActivated = true;
            _Updating = false;
            _webURL = null;
            _id = null;
        }


        public async Task<StorageFile> LoadUrlInfo()
        {
            StorageFile UrlInfo;
            Updating = true;
            try
            {
                UrlInfo = await localStorageFolder.GetFileAsync("UrlInfo");
                using (Stream file = await UrlInfo.OpenStreamForReadAsync())
                {
                    using (StreamReader read = new StreamReader(file))
                    {
                        webURL = new Uri(read.ReadLine());
                        IsActivated = bool.Parse(read.ReadLine());
                    }
                }
            }
            catch
            {
                UrlInfo = await SaveUrlInfo();
            }
            Updating = false;
            return UrlInfo;
        }
        public async Task<StorageFile> SaveUrlInfo()
        {
            StorageFile UrlInfo;
            Updating = true;

            if (webURL != null)
            {
                try
                {
                    UrlInfo = await localStorageFolder.GetFileAsync("UrlInfo");
                    await UrlInfo.DeleteAsync();
                    UrlInfo = await localStorageFolder.CreateFileAsync("UrlInfo");
                }
                catch
                {
                    UrlInfo = await localStorageFolder.CreateFileAsync("UrlInfo");
                }
                using (Stream file = await UrlInfo.OpenStreamForWriteAsync())
                {
                    using (StreamWriter write = new StreamWriter(file))
                    {
                        write.WriteLine(webURL.OriginalString);
                        write.WriteLine(IsActivated.ToString());
                    }
                }
            }
            else
                UrlInfo = null;

            Updating = false;
            return UrlInfo;
        }
        public async Task<bool> CheckExistance()
        {
            var local = ApplicationData.Current.LocalFolder;
            try
            {
                localStorageFolder = await local.GetFolderAsync(id);
            }
            catch
            {
                localStorageFolder = await local.CreateFolderAsync(id);
            }
            if (localStorageFolder == null) return false;

            var UrlInfo = await LoadUrlInfo();
            if (UrlInfo == null) return false;

            Updating = true;

            var FileList = await localStorageFolder.GetFilesAsync();
            var FileList_Snaps = FileList.Where(a => { if (a.Name.EndsWith(".html")||(!a.Name.StartsWith("Snap"))) return false; else return true; });
            var Tasks = new List<Task<bool>>();
            foreach (var file in FileList_Snaps)
            {
                var Snap = new UrlContentSnap();
                Snap.path = file.Path;
                Tasks.Add(Snap.LoadFromFile());
                UrlContentSnapList.Insert(0,Snap);
            }
            foreach (var m in Tasks)
                await m;
            if (UrlContentSnapList.Count > 0)
                recentStamp = UrlContentSnapList[0].TimeStamp[0].ToString();
            Updating = false;
            return true;
        }
        private WebContentFetcher Fetcher => new WebContentFetcher();
        public async Task CheckNow()
        {
            if (localStorageFolder == null || webURL == null || IsActivated == false) return;
            Updating = true;
            //string Content = await GetFromUrl(webURL);
            string Content = await Fetcher.FetchContent(webURL);

            if (Content.Count() == 0)
            { Updating = false; return; }

            UrlContentSnap newOne = new UrlContentSnap();
            newOne.Content = Content;
            newOne.TimeStamp.Add(DateTime.Now);
            newOne.Url = webURL.OriginalString;
            newOne.path = localStorageFolder.Path + "\\Snap_" + newOne.TimeStamp.Last().ToString("yyyyMMddHHmmss");
            if (UrlContentSnapList.Count > 0 && CompareToRecentRecord(UrlContentSnapList.First(), newOne))
            {//same
                UrlContentSnapList.First().TimeStamp.Insert(0,DateTime.Now);
                UrlContentSnapList.First().PropertyChangeEventHappen("TimeStampShowOut");
                await UrlContentSnapList.First().SaveToFile(SaveMode.Update);
            }
            else
            {
                await newOne.SaveToFile();
                UrlContentSnapList.Insert(0,newOne);
                sendNotification(newOne);
            }
            recentStamp = newOne.TimeStamp[0].ToString();
            Updating = false;
        }
        //protected async Task<string> GetFromUrl(Uri url)
        //{
        //    string result = "";
        //    var cts = new CancellationTokenSource();
        //    cts.CancelAfter(TimeSpan.FromSeconds(4));//设置延时时间4s
        //    try
        //    {
        //        HttpClient myHC = new HttpClient();
        //        HttpResponseMessage response = await myHC.GetAsync(url, cts.Token);
        //        result = await response.Content.ReadAsStringAsync();
        //    }
        //    catch (TaskCanceledException e)
        //    {
        //        Debug.WriteLine("连接超时");
        //        Debug.WriteLine(e.Message);
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.WriteLine("exception!!");
        //        Debug.WriteLine(e.Message);
        //    }
        //    //返回结果网页（html）代码
        //    return result;
        //}
        bool CompareToRecentRecord(UrlContentSnap RecentSnap, UrlContentSnap newOne)
        {
            return (RecentSnap.Content == newOne.Content) && (RecentSnap.Url == newOne.Url);
        }
        void sendNotification(UrlContentSnap newOne)
        {
            // 有新的出现后发送通知
            if (UrlContentSnapList.Count > 1)
                ToastsDef.SendNotification_TwoString("关注的网站 [" + id + "] 内容更新了", newOne.Url);
        }
        public async void Delete()
        {
            Updating = true;
            await localStorageFolder.DeleteAsync();
            Updating = false;
        }
    }

    public class UrlContentChangeCheckerManager
    {
        public ObservableCollection<UrlContentChangeChecker> UrlContentCheckerList { get; protected set; }
        public UrlContentChangeCheckerManager()
        {
            UrlContentCheckerList = new ObservableCollection<UrlContentChangeChecker>();
        }
        public async Task Init()
        {
            UrlContentCheckerList.Clear();
            var local = ApplicationData.Current.LocalFolder;
            var Folders = await local.GetFoldersAsync();
            var Tasks = new List<Task<bool>>();
            foreach (var m in Folders)
            {
                var uccc = new UrlContentChangeChecker(m.Name);
                Tasks.Add(uccc.CheckExistance());
                UrlContentCheckerList.Add(uccc);
            }
            foreach(var m in Tasks)
            {
                await m;
            }
        }
        public async Task CheckAll()
        {
            var Tasks = CheckAll_NoWait();
            foreach (var m in Tasks)
            {
                await m;
            }
        }
        public List<Task> CheckAll_NoWait()
        {
            var Tasks = new List<Task>();
            foreach (var m in UrlContentCheckerList)
            {
                Tasks.Add(m.CheckNow());
            }
            return Tasks;
        }
        public async Task<bool> AddItem(string id, string url)
        {
            if (UrlContentCheckerList.Where(m => { if (m.id == id) return true; else return false; }).Count() > 0)
                return false;
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch
            {
                return false;
            }
            var newOne = new UrlContentChangeChecker(id, uri);
            if (!await newOne.CheckExistance())
                await newOne.CheckNow();
            UrlContentCheckerList.Insert(0, newOne);

            return true;
        }
        public bool DeleteItem(string id)
        {
            var DeleteItems = UrlContentCheckerList.Where(m =>
              {
                  if (m.id == id) return true;
                  else return false;
              });
            //应该只有一个
            if (DeleteItems.Count() == 0) return false;
            DeleteItems.First().Delete();
            UrlContentCheckerList.Remove(DeleteItems.First());
            return true;
        }
    }
}
