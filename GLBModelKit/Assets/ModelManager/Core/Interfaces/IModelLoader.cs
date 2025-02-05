using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DevToolKit.Models.Events;

namespace DevToolKit.Models.Core
{
    public interface IModelLoader
    {
        event EventHandler<ModelLoadEventArgs> ModelLoadStateChanged;
        Task<GameObject> LoadModelAsync(string path, Transform parent = null, CancellationToken cancellationToken = default);
        int MaxRetryCount { get; set; }
        float RetryDelay { get; set; }
        bool IsProcessing { get; }
        void Cancel();
    }
}
