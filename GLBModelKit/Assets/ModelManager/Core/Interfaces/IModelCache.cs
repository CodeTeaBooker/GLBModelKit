using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    public interface IModelCache : IDisposable
    {
        bool HasModel(string modelPath);
        GameObject GetModel(string modelPath);
        bool AddModel(string modelPath, GameObject templateObject, long templateSizeInMB = 0);
        bool RemoveModel(string modelPath, bool removeInstances = true);
        void Clear();
        Task<bool> ClearAsync();
        GameObject CreateInstance(string modelPath, Transform parent = null);
        bool RemoveInstance(GameObject instanceObject);
        void RemoveAllInstances(string modelPath);
        IReadOnlyCollection<GameObject> GetModelInstances(string modelPath);
        int MaxCacheSize { get; }
        long MaxFileSizeInMB { get; }
        int CachedCount { get; }
        long GetTotalCacheSize();
        void AddListener(IModelCacheListener listener);
        void RemoveListener(IModelCacheListener listener);
    }
}
