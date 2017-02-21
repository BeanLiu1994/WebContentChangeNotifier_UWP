using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebContentChangeCheckerUtil;
using Windows.ApplicationModel.Background;

namespace TileRefresh
{
    public sealed class TileRefreshUtils : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            var TaskNow = new UrlContentChangeCheckerManager();
            await TaskNow.Init();
            await TaskNow.CheckAll();
            deferral.Complete();
        }
    }
}
