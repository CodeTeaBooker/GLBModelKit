namespace DevToolKit.Models.Events
{
    public enum ModelOperationState
    {
        None = 0,
        Started,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}
