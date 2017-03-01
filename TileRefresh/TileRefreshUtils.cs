using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebContentChangeCheckerUtil;
using Windows.ApplicationModel.Background;
using Toasts;
using Windows.Storage;
using System.IO;

namespace TileRefresh
{
    public sealed class TileRefreshUtils : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            SaveLog();
            //ToastsDef.SendNotification_TwoString("后台任务执行", "当前时间" + DateTime.Now.ToString());
            var deferral = taskInstance.GetDeferral();
            var TaskNow = new UrlContentChangeCheckerManager();
            await TaskNow.Init();
            var Tasks = TaskNow.CheckAll_NoWait();
            Task.WaitAll(Tasks.ToArray());
            Debug.WriteLine("结束任务");
            //ToastsDef.SendNotification_TwoString("后台任务结束", "当前时间" + DateTime.Now.ToString());

            deferral.Complete();
        }
        public async void SaveLog()
        {
            var local = ApplicationData.Current.LocalFolder;
            StorageFile logfile;
            try
            {
                logfile = await local.GetFileAsync("TimerTaskLog.txt");
            }
            catch
            {
                logfile = await local.CreateFileAsync("TimerTaskLog.txt");
            }
            if (logfile == null) return;

            string contentToWrite = "Task runs at : " + DateTime.Now.ToString();
            using (var stream = await logfile.OpenStreamForWriteAsync())
            {
                ulong t = (await logfile.GetBasicPropertiesAsync()).Size;
                if ((await logfile.GetBasicPropertiesAsync()).Size < (ulong)1e4)
                    stream.Position = stream.Length;
                else
                    stream.SetLength(contentToWrite.Count() + 1);
                using (StreamWriter write = new StreamWriter(stream))
                {
                    write.WriteLine(contentToWrite);
                }
            }
        }
    }
}
