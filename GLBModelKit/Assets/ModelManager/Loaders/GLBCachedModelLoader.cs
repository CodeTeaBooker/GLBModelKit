using DevToolKit.Models.Core;
using DevToolKit.Models.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DevToolKit.Models.Loaders
{
    public class GLBCachedModelLoader : BaseModelLoader
    {
        private readonly IModelLoader _innerLoader;
        private readonly IModelCache _cache;
        private const string LOG_DOMAIN = nameof(GLBCachedModelLoader);

        public GLBCachedModelLoader(IModelLoader innerLoader, IModelCache cache)
        {
            _innerLoader = innerLoader ?? throw new ArgumentNullException(nameof(innerLoader));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _innerLoader.ModelLoadStateChanged += OnInnerLoaderStateChanged;
        }

        private void OnInnerLoaderStateChanged(object s, ModelLoadEventArgs e)
        {
            if (!IsDisposed)
            {
                NotifyStateChanged(e);
            }
        }

        protected override async Task<GameObject> ExecuteLoad(string path, Transform parent, CancellationToken token)
        {
            if (_cache.HasModel(path))
            {
                Debug.Log($"[{LOG_DOMAIN}] Found template in cache: {path}");
                return _cache.CreateInstance(path, parent);
            }

            if (_cache.CachedCount >= _cache.MaxCacheSize)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Cache is full ({_cache.CachedCount}/{_cache.MaxCacheSize}), cannot load new model: {path}");
                NotifyStateChanged(ModelLoadEventArgs.CreateLoadFailedEvent(path, $"Cache limit reached ({_cache.CachedCount}/{_cache.MaxCacheSize})"));
                return null;
            }

            Debug.Log($"[{LOG_DOMAIN}] No cache found, delegate to inner loader: {path}");
            var template = await _innerLoader.LoadModelAsync(path, null, token);
            if (template != null)
            {
                if (_cache.AddModel(path, template))
                {
                    return _cache.CreateInstance(path, parent);
                }
                else
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Failed to cache template for: {path}");
                    SafeDestroyGameObject(template);
                    NotifyStateChanged(ModelLoadEventArgs.CreateLoadFailedEvent(path, "Failed to cache template"));
                    return null;
                }
            }
            return null;
        }

        private void SafeDestroyGameObject(GameObject obj)
        {
            if (obj != null && !obj.Equals(null))
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        public override void Cancel()
        {
            base.Cancel();
            _innerLoader?.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                if (_innerLoader != null)
                {
                    _innerLoader.ModelLoadStateChanged -= OnInnerLoaderStateChanged;

                    if (_innerLoader is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (ObjectDisposedException) { }
                    }
                }
            }

            base.Dispose(disposing);
        }
    }
}