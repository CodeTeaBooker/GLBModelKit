namespace DevToolKit.Models.Core
{
    public interface IModelCacheListener
    {
        void OnCacheStateChanged(DevToolKit.Models.Events.ModelCacheEventArgs args);
    }
}
