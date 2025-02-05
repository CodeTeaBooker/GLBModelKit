using DevToolKit.Models.Config;
using DevToolKit.Models.Core;
using DevToolKit.Models.Events;
using DevToolKit.Models.Loaders;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using DevToolKit.Models.Cache;
using DevToolKit.Models.Exporters;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevToolKit.Models.Managers
{
    public class GLBModelManager : MonoBehaviour, IModelCacheListener
    {
        private enum ManagerState
        {
            None,
            Initializing,
            Ready,
            Loading,
            Removing,
            Error
        }

        private const string LOG_DOMAIN = nameof(GLBModelManager);

        [Header("Configuration")]
        [SerializeField] private GLBModelManagerConfigSO _config;
        [SerializeField] private GLBModelExporterConfigSO _exporterConfig;
        [SerializeField] private Transform _rootTransform;

        [Header("Debug")]
        [SerializeField] private GameObject ToRemoveGameObject;

        private IModelCache _cache;
        private IModelLoader _loader;
        private IModelExporter _exporter;
        private AsyncCoroutineHelper _asyncHelper;
        private ManagerState _state = ManagerState.None;
        private CancellationTokenSource _cts;

        public GLBModelManagerConfigSO CurrentConfig => _config;
        public Transform RootTransform => _rootTransform;

        public event EventHandler<ModelLoadEventArgs> LoadStateChanged;
        public event EventHandler<ModelExportEventArgs> ExportStateChanged;
        public event EventHandler<ModelCacheEventArgs> CacheStateChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private async void Start()
        {
            if (_state == ManagerState.Ready && _config != null &&
                _config.ImportOnStart && !string.IsNullOrEmpty(_config.ModelPath))
            {
                await CreateModelAsync(_config.ModelPath, _rootTransform);
            }
        }

        private void OnEnable()
        {
            if (_cache is ModelCache modelCache)
            {
                modelCache.AddListener(this);
                if (_config?.DebugMode ?? false)
                {
                    Debug.Log($"[{LOG_DOMAIN}] Cache listener registered");
                }
            }
        }

        private void OnDisable()
        {
            if (_cache is ModelCache modelCache)
            {
                modelCache.RemoveListener(this);
                if (_config?.DebugMode ?? false)
                {
                    Debug.Log($"[{LOG_DOMAIN}] Cache listener unregistered");
                }
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            UpdateState(ManagerState.Initializing);
            if (!ValidateConfiguration(out var error))
            {
                Debug.LogError($"[{LOG_DOMAIN}] {error}");
                UpdateState(ManagerState.Error);
                return;
            }

            InitializeHelpers();
            InitializeCache();
            InitializeLoader();
            InitializeExporter();

            ResetCancellationToken();
            UpdateState(ManagerState.Ready);
        }

        private void InitializeHelpers()
        {
            _asyncHelper = GetComponent<AsyncCoroutineHelper>();
            if (!_asyncHelper)
                _asyncHelper = gameObject.AddComponent<AsyncCoroutineHelper>();
        }

        private void InitializeCache()
        {
            _cache = new ModelCache(_config.MaxCacheSize, _config.MaxFileSizeInMB);
        }

        private void InitializeLoader()
        {
            var importConfig = _config.CreateImportConfig();
            var baseLoader = new GLBModelLoader(_asyncHelper, importConfig);
            var retryStrategy = new DefaultRetryStrategy(_config.MaxRetryCount, _config.RetryDelay);
            var retryableLoader = new RetryableModelLoader(baseLoader, retryStrategy);
            var cachedLoader = new GLBCachedModelLoader(retryableLoader, _cache);

            _loader = cachedLoader;
            _loader.ModelLoadStateChanged += OnModelLoadStateChanged;
        }

        private void InitializeExporter()
        {
            if (_exporterConfig == null)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] No exporter config assigned!");
                return;
            }

            try
            {
                if (!_exporterConfig.Validate(out string error))
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Invalid exporter configuration: {error}");
                    return;
                }

                _exporter = new GLBModelExporter(_exporterConfig);
                _exporter.ExportStateChanged += OnExportStateChanged;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Failed to initialize exporter: {ex.Message}");
            }
        }

        #endregion

        #region Model Operations

        public async Task<GameObject> CreateModelAsync(string path, Transform parent = null)
        {
            if (!EnsureReadyState(nameof(CreateModelAsync), out var error))
            {
                Debug.LogError(error);
                return null;
            }

            UpdateState(ManagerState.Loading);
            try
            {
                ResetCancellationToken();
                return await _loader.LoadModelAsync(path, parent, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] Load cancelled: {path}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Load error: {ex.Message}");
                throw;
            }
            finally
            {
                if (_state == ManagerState.Loading)
                    UpdateState(ManagerState.Ready);
            }
        }

        public async Task RemoveModelGroupAsync(string path)
        {
            if (!EnsureReadyState(nameof(RemoveModelGroupAsync), out var error))
            {
                Debug.LogError(error);
                return;
            }

            UpdateState(ManagerState.Removing);
            try
            {
                await Task.Yield();
                _cache.RemoveModel(path, true);
            }
            finally
            {
                if (_state == ManagerState.Removing)
                    UpdateState(ManagerState.Ready);
            }
        }

        public bool RemoveModel(GameObject instance)
        {
            if (!EnsureReadyState(nameof(RemoveModel), out var error))
            {
                Debug.LogError(error);
                return false;
            }

            if (instance == null)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] RemoveModel called with null instance");
                return false;
            }

            return _cache.RemoveInstance(instance);
        }

        public void ClearModelLibrary()
        {
            if (!EnsureReadyState(nameof(ClearModelLibrary), out var error))
            {
                Debug.LogError(error);
                return;
            }

            UpdateState(ManagerState.Removing);
            try
            {
                _cache.Clear();
            }
            finally
            {
                UpdateState(ManagerState.Ready);
            }
        }

        public async Task ClearModelLibraryAsync()
        {
            if (!EnsureReadyState(nameof(ClearModelLibraryAsync), out var error))
            {
                Debug.LogError(error);
                return;
            }

            UpdateState(ManagerState.Removing);
            try
            {
                await (_cache as ModelCache)?.ClearAsync();
            }
            finally
            {
                UpdateState(ManagerState.Ready);
            }
        }

        public bool ExportModel(string path, string fileName, Transform[] roots)
        {
            if (!EnsureReadyState(nameof(ExportModel), out var error))
            {
                Debug.LogError(error);
                return false;
            }

            if (_exporter == null)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Exporter not initialized! Please ensure ExporterConfig is properly assigned and configured.");
                return false;
            }

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(fileName))
            {
                Debug.LogError($"[{LOG_DOMAIN}] Invalid export parameters: path and fileName must not be empty");
                return false;
            }

            if (roots == null || roots.Length == 0)
            {
                Debug.LogError($"[{LOG_DOMAIN}] No transforms provided for export");
                return false;
            }

            return _exporter.ExportToFile(path, fileName, roots);
        }

        public byte[] ExportModelToStream(string fileName, Transform[] roots)
        {
            if (!EnsureReadyState(nameof(ExportModelToStream), out var error))
            {
                Debug.LogError(error);
                return null;
            }

            if (_exporter == null)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Exporter not initialized!");
                return null;
            }

            return _exporter.ExportToStream(fileName, roots);
        }

        #endregion

        #region Event Handlers

        public void OnCacheStateChanged(ModelCacheEventArgs args)
        {
            if (args == null) return;

            if (_config?.DebugMode ?? false)
            {
                Debug.Log($"[{LOG_DOMAIN}] Cache state changed: Operation={args.OperationType}, " +
                         $"Path={args.ModelPath}, Success={args.IsSuccessful}");
            }

            switch (args.OperationType)
            {
                case CacheOperationType.Added:
                    HandleModelAdded(args);
                    break;
                case CacheOperationType.Removed:
                    HandleModelRemoved(args);
                    break;
                case CacheOperationType.Cleared:
                    HandleCacheCleared(args);
                    break;
                case CacheOperationType.Error:
                    HandleCacheError(args);
                    break;
            }

            CacheStateChanged?.Invoke(this, args);
        }

        private void OnModelLoadStateChanged(object sender, ModelLoadEventArgs args)
        {
            if (_config?.DebugMode ?? false)
                Debug.Log($"[{LOG_DOMAIN}] Load state changed: {args.LoadState}");

            LoadStateChanged?.Invoke(this, args);
        }

        private void OnExportStateChanged(object sender, ModelExportEventArgs args)
        {
            if (_config?.DebugMode ?? false)
                Debug.Log($"[{LOG_DOMAIN}] Export state changed: {args.ExportState}");

            ExportStateChanged?.Invoke(this, args);
        }

        #endregion

        #region Cache Event Handlers

        private void HandleModelAdded(ModelCacheEventArgs args)
        {
            if (args.IsSuccessful)
            {
                if (_config?.DebugMode ?? false)
                {
                    Debug.Log($"[{LOG_DOMAIN}] Model added successfully: {args.ModelPath} " +
                             $"(Size: {args.ModelSizeInMB}MB, Total Cache: {args.CachedCount}/{args.MaxCacheSize})");
                }
            }
        }

        private void HandleModelRemoved(ModelCacheEventArgs args)
        {
            if (args.IsSuccessful)
            {
                if (_config?.DebugMode ?? false)
                {
                    Debug.Log($"[{LOG_DOMAIN}] Model removed successfully: {args.ModelPath} " +
                             $"(Remaining Cache: {args.CachedCount})");
                }
            }
        }

        private void HandleCacheCleared(ModelCacheEventArgs args)
        {
            if (_config?.DebugMode ?? false)
            {
                Debug.Log($"[{LOG_DOMAIN}] Cache cleared: Previously had {args.MaxCacheSize} models");
            }
        }

        private void HandleCacheError(ModelCacheEventArgs args)
        {
            Debug.LogError($"[{LOG_DOMAIN}] Cache error occurred: {args.ErrorMessage} " +
                          $"(Path: {args.ModelPath})");

            if (args.ModelPath != null && args.ErrorMessage?.Contains("limit reached") == true)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] Cache limit reached, consider implementing cleanup strategy");
            }
        }

        #endregion

        #region Utility Methods

        private bool ValidateConfiguration(out string error)
        {
            if (_config == null)
            {
                error = "Missing manager configuration!";
                return false;
            }
            if (!_config.Validate(out string configError))
            {
                error = $"Invalid manager configuration: {configError}";
                return false;
            }
            error = null;
            return true;
        }

        private void UpdateState(ManagerState newState)
        {
            _state = newState;
            if (_config?.DebugMode ?? false)
                Debug.Log($"[{LOG_DOMAIN}] State changed to: {newState}");
        }

        private bool EnsureReadyState(string operation, out string error)
        {
            if (_state != ManagerState.Ready)
            {
                error = $"[{LOG_DOMAIN}] {operation} failed: Not in Ready state, current: {_state}";
                return false;
            }
            error = null;
            return true;
        }

        private void ResetCancellationToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void Cleanup()
        {
            try
            {
                if (_loader != null)
                {
                    _loader.ModelLoadStateChanged -= OnModelLoadStateChanged;
                    if (_loader is IDisposable disposableLoader)
                    {
                        disposableLoader.Dispose();
                    }
                }
                _loader = null;

                if (_exporter != null)
                {
                    _exporter.ExportStateChanged -= OnExportStateChanged;
                    if (_exporter is IDisposable disposableExporter)
                    {
                        disposableExporter.Dispose();
                    }
                }
                _exporter = null;

                if (_cache is IDisposable disposableCache)
                {
                    disposableCache.Dispose();
                }
                _cache = null;

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                UpdateState(ManagerState.None);

                if (_config?.DebugMode ?? false)
                    Debug.Log($"[{LOG_DOMAIN}] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Editor
#if UNITY_EDITOR
        [CustomEditor(typeof(GLBModelManager))]
        public class GLBModelManagerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                GLBModelManager manager = (GLBModelManager)target;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Create Model (from config)"))
                    {
                        if (manager.CurrentConfig != null)
                            _ = manager.CreateModelAsync(manager.CurrentConfig.ModelPath, manager.RootTransform);
                        else
                            Debug.LogWarning("No manager config assigned!");
                    }

                    if (GUILayout.Button("Delete Selected Model"))
                    {
                        if (manager.ToRemoveGameObject != null)
                        {
                            bool success = manager.RemoveModel(manager.ToRemoveGameObject);
                            Debug.Log($"RemoveModel success: {success}");
                        }
                        else
                            Debug.LogWarning("Please assign ToRemoveGameObject in Inspector first!");
                    }

                    if (GUILayout.Button("Delete Model Group (from config)"))
                    {
                        if (manager.CurrentConfig != null)
                            _ = manager.RemoveModelGroupAsync(manager.CurrentConfig.ModelPath);
                        else
                            Debug.LogWarning("No manager config assigned!");
                    }

                    if (GUILayout.Button("Clear Model Library"))
                    {
                        manager.ClearModelLibrary();
                    }

                    if (GUILayout.Button("Clear Model Library (Async)"))
                    {
                        _ = manager.ClearModelLibraryAsync();
                    }

                    if (GUILayout.Button("Export Model"))
                    {
                        if (manager.RootTransform != null)
                        {
                            var success = manager.ExportModel("Exports", "ExportedModel",
                                new Transform[] { manager.RootTransform });
                            Debug.Log($"Export success: {success}");
                        }
                        else


                            Debug.LogWarning("Please assign RootTransform first!");
                    }

                    if (GUILayout.Button("Export Model To Stream"))
                    {
                        if (manager.RootTransform != null)
                        {
                            var data = manager.ExportModelToStream("ExportedModel",
                                new Transform[] { manager.RootTransform });
                            Debug.Log($"Export to stream result: {(data != null ? "Success" : "Failed")}");
                        }
                        else
                            Debug.LogWarning("Please assign RootTransform first!");
                    }
                }
            }
        }
#endif
    }
}
#endregion


