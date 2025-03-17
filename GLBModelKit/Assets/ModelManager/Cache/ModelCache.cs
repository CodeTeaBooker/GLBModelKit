using DevToolKit.Models.Core;
using DevToolKit.Models.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;

namespace DevToolKit.Models.Cache
{
    public class ModelCache : DisposableBase, IModelCache
    {
        private readonly ConcurrentDictionary<string, CachedModelInfo> _modelCache = new ConcurrentDictionary<string, CachedModelInfo>();
        private readonly ModelInstanceManager _instanceManager;
        private readonly EventDispatcher _eventDispatcher;
        private readonly object _clearLock = new object();
        private readonly object _operationLock = new object();

        private const int CLEARING_STATE_NO = 0;
        private const int CLEARING_STATE_YES = 1;

        private volatile int _isClearing = CLEARING_STATE_NO;

        private GameObject _templateContainer;
        private const string LOG_DOMAIN = nameof(ModelCache);

        public int MaxCacheSize { get; }
        public long MaxFileSizeInMB { get; }
        public int CachedCount => _modelCache.Count;

        public ModelCache(int maxCacheCount, long maxFileSizeInMB)
        {
            if (maxCacheCount <= 0)
                throw new ArgumentException("Max cache count must be > 0", nameof(maxCacheCount));
            if (maxFileSizeInMB <= 0)
                throw new ArgumentException("Max file size must be > 0", nameof(maxFileSizeInMB));

            MaxCacheSize = maxCacheCount;
            MaxFileSizeInMB = maxFileSizeInMB;

            _instanceManager = new ModelInstanceManager();
            _eventDispatcher = new EventDispatcher();
        }

        public bool HasModel(string modelPath)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(modelPath)) return false;
            return _modelCache.ContainsKey(modelPath);
        }

        public GameObject GetModel(string modelPath)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(modelPath)) return null;
            return _modelCache.TryGetValue(modelPath, out var info) ? info.ModelObject : null;
        }

        public bool AddModel(string modelPath, GameObject templateObject, long templateSizeInMB = 0)
        {
            ThrowIfDisposed();

            if (_isClearing == CLEARING_STATE_YES)
            {
                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Cache is being cleared", CachedCount, GetTotalCacheSize()));
                return false;
            }

            lock (_operationLock)
            {
                if (string.IsNullOrEmpty(modelPath) || templateObject == null)
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Invalid parameters", CachedCount, GetTotalCacheSize()));
                    return false;
                }

                if (_modelCache.ContainsKey(modelPath))
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateDuplicateEvent(modelPath, CachedCount, GetTotalCacheSize()));
                    return false;
                }

                if (CachedCount >= MaxCacheSize)
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateLimitEvent(modelPath, CachedCount, MaxCacheSize, GetTotalCacheSize()));
                    return false;
                }

                if (templateSizeInMB > MaxFileSizeInMB)
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateSizeLimitEvent(modelPath, templateSizeInMB, MaxFileSizeInMB, GetTotalCacheSize()));
                    return false;
                }

                var gltfComponent = templateObject.GetComponent<UnityGLTF.InstantiatedGLTFObject>();
                if (gltfComponent == null)
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Template missing InstantiatedGLTFObject", CachedCount, GetTotalCacheSize()));
                    return false;
                }

                EnsureTemplateContainer();
                templateObject.transform.SetParent(_templateContainer.transform, false);

                var templateInfo = new CachedModelInfo(templateObject, templateSizeInMB);
                if (_modelCache.TryAdd(modelPath, templateInfo))
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateAddedEvent(modelPath, templateObject, templateSizeInMB, CachedCount, GetTotalCacheSize()));
                    return true;
                }

                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Failed to add to cache", CachedCount, GetTotalCacheSize()));
                return false;
            }
        }

        public int AddModels(IEnumerable<KeyValuePair<string, GameObject>> models, Dictionary<string, long> templateSizes = null)
        {
            ThrowIfDisposed();
            if (models == null) return 0;

            if (_isClearing == CLEARING_STATE_YES)
            {
                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(null, "Cache is being cleared", CachedCount, GetTotalCacheSize()));
                return 0;
            }

            lock (_operationLock)
            {
                var successCount = 0;
                var events = new List<ModelCacheEventArgs>();

                foreach (var pair in models)
                {
                    string modelPath = pair.Key;
                    GameObject templateObject = pair.Value;

                    if (string.IsNullOrEmpty(modelPath) || templateObject == null)
                    {
                        events.Add(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Invalid parameters", CachedCount, GetTotalCacheSize()));
                        continue;
                    }

                    if (_modelCache.ContainsKey(modelPath))
                    {
                        events.Add(ModelCacheEventArgs.CreateDuplicateEvent(modelPath, CachedCount, GetTotalCacheSize()));
                        continue;
                    }

                    if (CachedCount >= MaxCacheSize)
                    {
                        events.Add(ModelCacheEventArgs.CreateLimitEvent(modelPath, CachedCount, MaxCacheSize, GetTotalCacheSize()));
                        continue;
                    }

                    long templateSizeInMB = 0;
                    if (templateSizes != null && templateSizes.TryGetValue(modelPath, out long size))
                    {
                        templateSizeInMB = size;
                    }

                    if (templateSizeInMB > MaxFileSizeInMB)
                    {
                        events.Add(ModelCacheEventArgs.CreateSizeLimitEvent(modelPath, templateSizeInMB, MaxFileSizeInMB, GetTotalCacheSize()));
                        continue;
                    }

                    var gltfComponent = templateObject.GetComponent<UnityGLTF.InstantiatedGLTFObject>();
                    if (gltfComponent == null)
                    {
                        events.Add(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Template missing InstantiatedGLTFObject", CachedCount, GetTotalCacheSize()));
                        continue;
                    }

                    EnsureTemplateContainer();
                    templateObject.transform.SetParent(_templateContainer.transform, false);

                    var templateInfo = new CachedModelInfo(templateObject, templateSizeInMB);
                    if (_modelCache.TryAdd(modelPath, templateInfo))
                    {
                        successCount++;
                        events.Add(ModelCacheEventArgs.CreateAddedEvent(modelPath, templateObject, templateSizeInMB, CachedCount, GetTotalCacheSize()));
                    }
                    else
                    {
                        events.Add(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Failed to add to cache", CachedCount, GetTotalCacheSize()));
                    }
                }

                if (events.Count > 0)
                {
                    _eventDispatcher.NotifyStateChangedBatch(events);
                }

                return successCount;
            }
        }

        public bool RemoveModel(string modelPath, bool removeInstances = true)
        {
            ThrowIfDisposed();

            if (_isClearing == CLEARING_STATE_YES)
            {
                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Cache is being cleared", CachedCount, GetTotalCacheSize()));
                return false;
            }

            lock (_operationLock)
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Invalid path", CachedCount, GetTotalCacheSize()));
                    return false;
                }

                if (_modelCache.TryRemove(modelPath, out var templateInfo))
                {
                    if (removeInstances)
                    {
                        _instanceManager.RemoveAllInstances(modelPath);
                    }

                    if (templateInfo.ModelObject != null)
                    {
                        SafeDestroyGameObject(templateInfo.ModelObject);
                    }

                    NotifyStateChanged(ModelCacheEventArgs.CreateRemovedEvent(modelPath, templateInfo.ModelObject, CachedCount, GetTotalCacheSize()));
                    return true;
                }

                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Model not found in cache", CachedCount, GetTotalCacheSize()));
                return false;
            }
        }

        public int RemoveModels(IEnumerable<string> modelPaths, bool removeInstances = true)
        {
            ThrowIfDisposed();
            if (modelPaths == null) return 0;

            if (_isClearing == CLEARING_STATE_YES)
            {
                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(null, "Cache is being cleared", CachedCount, GetTotalCacheSize()));
                return 0;
            }

            lock (_operationLock)
            {
                var successCount = 0;
                var events = new List<ModelCacheEventArgs>();

                foreach (var path in modelPaths)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        events.Add(ModelCacheEventArgs.CreateErrorEvent(path, "Invalid path", CachedCount, GetTotalCacheSize()));
                        continue;
                    }

                    if (_modelCache.TryRemove(path, out var templateInfo))
                    {
                        if (removeInstances)
                        {
                            _instanceManager.RemoveAllInstances(path);
                        }

                        if (templateInfo.ModelObject != null)
                        {
                            SafeDestroyGameObject(templateInfo.ModelObject);
                        }

                        successCount++;
                        events.Add(ModelCacheEventArgs.CreateRemovedEvent(path, templateInfo.ModelObject, CachedCount, GetTotalCacheSize()));
                    }
                    else
                    {
                        events.Add(ModelCacheEventArgs.CreateErrorEvent(path, "Model not found in cache", CachedCount, GetTotalCacheSize()));
                    }
                }

                if (events.Count > 0)
                {
                    _eventDispatcher.NotifyStateChangedBatch(events);
                }

                return successCount;
            }
        }

        public async Task<bool> ClearAsync()
        {
            ThrowIfDisposed();

            if (Interlocked.Exchange(ref _isClearing, CLEARING_STATE_YES) == CLEARING_STATE_YES)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] Clear operation already in progress");
                return false;
            }

            try
            {
                var previousCount = CachedCount;
                var previousSize = GetTotalCacheSize();

                _instanceManager.Clear();

                List<KeyValuePair<string, CachedModelInfo>> modelsToDestroy;
                lock (_clearLock)
                {
                    modelsToDestroy = _modelCache.ToList();
                    _modelCache.Clear();
                }

                foreach (var kvp in modelsToDestroy)
                {
                    SafeDestroyGameObject(kvp.Value.ModelObject);
                    await Task.Yield();
                }

                if (_templateContainer != null)
                {
                    SafeDestroyGameObject(_templateContainer);
                    _templateContainer = null;
                    await Task.Yield();
                }

                NotifyStateChanged(ModelCacheEventArgs.CreateClearedEvent(previousCount, MaxCacheSize, previousSize));
                return true;
            }
            finally
            {
                _isClearing = CLEARING_STATE_NO;
            }
        }

        public void Clear()
        {
            ThrowIfDisposed();

            if (Interlocked.Exchange(ref _isClearing, CLEARING_STATE_YES) == CLEARING_STATE_YES)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] Clear operation already in progress");
                return;
            }

            try
            {
                lock (_clearLock)
                {
                    var previousCount = CachedCount;
                    var previousSize = GetTotalCacheSize();

                    _instanceManager.Clear();

                    var modelsToDestroy = _modelCache.ToList();
                    _modelCache.Clear();

                    foreach (var kvp in modelsToDestroy)
                    {
                        SafeDestroyGameObject(kvp.Value.ModelObject);
                    }

                    if (_templateContainer != null)
                    {
                        SafeDestroyGameObject(_templateContainer);
                        _templateContainer = null;
                    }

                    NotifyStateChanged(ModelCacheEventArgs.CreateClearedEvent(previousCount, MaxCacheSize, previousSize));
                }
            }
            finally
            {
                _isClearing = CLEARING_STATE_NO;
            }
        }

        public GameObject CreateInstance(string modelPath, Transform parent = null)
        {
            ThrowIfDisposed();

            if (_isClearing == CLEARING_STATE_YES)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Cannot create instance while cache is being cleared");
                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Cache is being cleared", CachedCount, GetTotalCacheSize()));
                return null;
            }

            lock (_operationLock)
            {
                if (string.IsNullOrEmpty(modelPath))
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Invalid model path for CreateInstance");
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Invalid path for instance creation", CachedCount, GetTotalCacheSize()));
                    return null;
                }

                var template = GetModel(modelPath);
                if (template == null)
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Template not found for path: {modelPath}");
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Template not found", CachedCount, GetTotalCacheSize()));
                    return null;
                }

                var gltfComponent = template.GetComponent<UnityGLTF.InstantiatedGLTFObject>();
                if (gltfComponent == null)
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Template missing InstantiatedGLTFObject: {modelPath}");
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Invalid template format", CachedCount, GetTotalCacheSize()));
                    return null;
                }

                var instanceGltfComponent = gltfComponent.Duplicate();
                if (instanceGltfComponent == null)
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Failed to duplicate GLTF object: {modelPath}");
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(modelPath, "Failed to create instance", CachedCount, GetTotalCacheSize()));
                    return null;
                }

                var instance = instanceGltfComponent.gameObject;
                if (parent != null)
                {
                    instance.transform.SetParent(parent, false);
                }

                instance.SetActive(true);
                _instanceManager.TrackInstance(modelPath, instance);
                return instance;
            }
        }

        public bool RemoveInstance(GameObject instanceObject)
        {
            ThrowIfDisposed();
            return _instanceManager.RemoveInstance(instanceObject);
        }

        public void RemoveAllInstances(string modelPath)
        {
            ThrowIfDisposed();
            _instanceManager.RemoveAllInstances(modelPath);
        }

        public IReadOnlyCollection<GameObject> GetModelInstances(string modelPath)
        {
            ThrowIfDisposed();
            return _instanceManager.GetModelInstances(modelPath);
        }

        public long GetTotalCacheSize()
        {
            ThrowIfDisposed();
            return _modelCache.Values.Sum(info => info.ModelSizeInMB);
        }

        public void AddListener(IModelCacheListener listener)
        {
            ThrowIfDisposed();
            _eventDispatcher.AddListener(listener);
        }

        public void RemoveListener(IModelCacheListener listener)
        {
            if (IsDisposed) return;
            _eventDispatcher.RemoveListener(listener);
        }

        protected virtual void NotifyStateChanged(ModelCacheEventArgs args)
        {
            if (!IsDisposed)
            {
                Debug.Log($"[{LOG_DOMAIN}] Cache state change: {args}");
                _eventDispatcher.NotifyStateChanged(args);
            }
        }

        private void EnsureTemplateContainer()
        {
            if (_templateContainer == null)
            {
                _templateContainer = new GameObject("__GLBModelTemplates__");
                _templateContainer.SetActive(true);
            }
        }

        private void SafeDestroyGameObject(GameObject obj)
        {
            if (obj == null || !obj) return;

            try
            {
                var gltfObj = obj.GetComponent<InstantiatedGLTFObject>();
                if (gltfObj)
                {
                    gltfObj.CachedData = null;
                }

                UnityEngine.Object.Destroy(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Error destroying GameObject: {ex.Message}");
                NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(null, $"Error destroying object: {ex.Message}", CachedCount, GetTotalCacheSize()));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                try
                {
                    Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Error during Clear in Dispose: {ex.Message}");
                    NotifyStateChanged(ModelCacheEventArgs.CreateErrorEvent(null, $"Error during dispose: {ex.Message}", CachedCount, GetTotalCacheSize()));
                }

                _instanceManager.Dispose();
                _eventDispatcher.Dispose();
            }
            base.Dispose(disposing);
        }

        private class CachedModelInfo
        {
            public GameObject ModelObject { get; }
            public long ModelSizeInMB { get; }
            public DateTime CacheTime { get; }

            public CachedModelInfo(GameObject modelObject, long modelSizeInMB)
            {
                ModelObject = modelObject;
                ModelSizeInMB = modelSizeInMB;
                CacheTime = DateTime.UtcNow;
            }
        }
    }
}
