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
using Windows.Storage;

namespace WebContentChangeCheckerUtil
{
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

        public ObservableCollection<DateTime> _TimeStamp;
        public ObservableCollection<DateTime> TimeStamp
        {
            get { return _TimeStamp; }
            set
            {
                _TimeStamp = value;
                PropertyChangeEventHappen(nameof(TimeStamp));
            }
        }

        public string ContentFile { get; protected set; }
        public string Content { get; set; }

        public UrlContentSnap()
        {
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

    public class UrlContentChangeChecker
    {
        public string id { get; protected set; }

        public Uri webURL { get; protected set; }

        public ObservableCollection<UrlContentSnap> UrlContentSnapList { get; protected set; }

        protected StorageFolder localStorageFolder;

        public UrlContentChangeChecker(string _id, Uri _webURL)
        {
            UrlContentSnapList = new ObservableCollection<UrlContentSnap>();
            id = _id;
            webURL = _webURL;
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

            var FileList = await localStorageFolder.GetFilesAsync();
            var FileList_Snaps = FileList.Where(a => { if (a.Name.EndsWith(".html")) return false; else return true; });
            foreach (var file in FileList_Snaps)
            {
                var Snap = new UrlContentSnap();
                Snap.path = file.Path;
                await Snap.LoadFromFile();
                UrlContentSnapList.Add(Snap);
            }
            // 没写完 如果文件夹存在时读取所有snap到UrlContentSnapList内

            return true;

        }
        public async void CheckNow()
        {
            if (localStorageFolder == null) return;

            string Content = await GetFromUrl(webURL);

            UrlContentSnap newOne = new UrlContentSnap();
            newOne.Content = Content;
            newOne.TimeStamp.Add(DateTime.Now);
            newOne.Url = webURL.OriginalString;
            newOne.path = localStorageFolder.Path + "\\Snap_" + newOne.TimeStamp[0].ToString("yyyyMMddHHmmss");
            if (UrlContentSnapList.Count > 0 && CompareToRecentRecord(UrlContentSnapList.Last(), newOne))
            {//same
                UrlContentSnapList.Last().TimeStamp.Insert(0,DateTime.Now);
                await UrlContentSnapList.Last().SaveToFile(SaveMode.Update);
            }
            else
            {
                await newOne.SaveToFile();
                UrlContentSnapList.Add(newOne);
                sendNotification(newOne);
            }
        }
        protected async Task<string> GetFromUrl(Uri url)
        {
            string result = "";
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(4));//设置延时时间4s
            try
            {
                HttpClient myHC = new HttpClient();
                HttpResponseMessage response = await myHC.GetAsync(url, cts.Token);
                result = await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException e)
            {
                Debug.WriteLine("连接超时");
                Debug.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Debug.WriteLine("exception!!");
                Debug.WriteLine(e.Message);
            }
            //返回结果网页（html）代码
            return result;
        }
        bool CompareToRecentRecord(UrlContentSnap RecentSnap, UrlContentSnap newOne)
        {
            return (RecentSnap.Content == newOne.Content) && (RecentSnap.Url == newOne.Url);
        }
        void sendNotification(UrlContentSnap newOne)
        {
            // 发送通知
        }
    }
}
