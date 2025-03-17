using System.Collections.Generic;

namespace DevToolKit.Models.Core
{
    public interface IModelCacheListener
    {
        void OnCacheStateChanged(DevToolKit.Models.Events.ModelCacheEventArgs args);

        void OnCacheStateChangedBatch(IReadOnlyList<DevToolKit.Models.Events.ModelCacheEventArgs> eventsBatch)
        {
            foreach (var args in eventsBatch)
            {
                OnCacheStateChanged(args);
            }
        }
    }
}
