using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace DevToolKit.Models.Cache
{
    public class ModelInstanceManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, HashSet<GameObject>> _instancesCache = new ConcurrentDictionary<string, HashSet<GameObject>>();
        private readonly ConcurrentDictionary<GameObject, string> _instancePathMap = new ConcurrentDictionary<GameObject, string>();
        private readonly object _disposeLock = new object();
        private bool _disposed;
        private const string LOG_DOMAIN = "ModelInstanceManager";

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void SafeDestroyGameObject(GameObject obj)
        {
            if (obj != null && !obj.Equals(null))
                UnityEngine.Object.Destroy(obj);
        }

        public bool TrackInstance(string modelPath, GameObject instance)
        {
            ThrowIfDisposed();
            if (instance == null || string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError($"[{LOG_DOMAIN}] Invalid parameters for TrackInstance");
                return false;
            }

            var instances = _instancesCache.GetOrAdd(modelPath, _ => new HashSet<GameObject>());
            lock (instances)
            {
                if (instances.Add(instance))
                {
                    _instancePathMap[instance] = modelPath;
                    return true;
                }
            }
            return false;
        }

        public bool RemoveInstance(GameObject instance)
        {
            ThrowIfDisposed();
            if (instance == null) return false;

            if (_instancePathMap.TryRemove(instance, out string path))
            {
                if (_instancesCache.TryGetValue(path, out var instances))
                {
                    lock (instances)
                    {
                        instances.Remove(instance);
                        if (instances.Count == 0)
                            _instancesCache.TryRemove(path, out _);
                    }
                    SafeDestroyGameObject(instance);
                    return true;
                }
            }
            return false;
        }

        public void RemoveAllInstances(string modelPath)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(modelPath)) return;

            if (_instancesCache.TryGetValue(modelPath, out var instanceSet))
            {
                HashSet<GameObject> copy;
                lock (instanceSet)
                {
                    copy = new HashSet<GameObject>(instanceSet);
                    instanceSet.Clear();
                }

                foreach (var instance in copy)
                {
                    _instancePathMap.TryRemove(instance, out _);
                    SafeDestroyGameObject(instance);
                }
                _instancesCache.TryRemove(modelPath, out _);
                Debug.Log($"[{LOG_DOMAIN}] All instances removed: {modelPath}");
            }
        }

        public IReadOnlyCollection<GameObject> GetModelInstances(string modelPath)
        {
            ThrowIfDisposed();
            if (_instancesCache.TryGetValue(modelPath, out var instances))
            {
                lock (instances)
                {
                    return new HashSet<GameObject>(instances);
                }
            }
            return Array.Empty<GameObject>();
        }

        public void Clear()
        {
            if (_disposed) return;
            var allPaths = new List<string>(_instancesCache.Keys);
            foreach (var path in allPaths)
            {
                RemoveAllInstances(path);
            }
            _instancesCache.Clear();
            _instancePathMap.Clear();
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Clear();
                }
            }
        }
    }
}
