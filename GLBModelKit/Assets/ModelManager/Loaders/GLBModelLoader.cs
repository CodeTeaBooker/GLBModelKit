using DevToolKit.Models.Config;
using DevToolKit.Models.Core;
using DevToolKit.Models.Events;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;

namespace DevToolKit.Models.Loaders
{
    public class GLBModelLoader : BaseModelLoader
    {
        private readonly AsyncCoroutineHelper _asyncHelper;
        private readonly GLBModelImportConfig _importSettings;
        private readonly DefaultImporterFactory _importerFactory;
        private const string LOG_DOMAIN = nameof(GLBModelLoader);

        public GLBModelLoader(AsyncCoroutineHelper asyncHelper, GLBModelImportConfig settings)
        {
            _asyncHelper = asyncHelper ?? throw new ArgumentNullException(nameof(asyncHelper));
            _importSettings = settings ?? new GLBModelImportConfig();
            _importerFactory = ScriptableObject.CreateInstance<DefaultImporterFactory>();
        }

        protected override async Task<GameObject> ExecuteLoad(string path, Transform parent, CancellationToken token)
        {
            string fullPath = GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            string filename = Path.GetFileName(fullPath);

            using var dataLoader = new DisposableWebRequestLoader(directory);

            var importOptions = new ImportOptions
            {
                AsyncCoroutineHelper = _asyncHelper,
                DataLoader = dataLoader,
                ImportNormals = _importSettings.ImportNormals,
                ImportTangents = _importSettings.ImportTangents,
                SwapUVs = _importSettings.SwapUVs
            };

            using var sceneImporter = _importerFactory.CreateSceneImporter(filename, importOptions);

            try
            {
                return await LoadSceneWithProgress(sceneImporter, path, parent, token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] Load cancelled: {path}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{LOG_DOMAIN}] Load failed: {path}, Error: {ex.Message}");
                throw;
            }
        }

        private async Task<GameObject> LoadSceneWithProgress(
            GLTFSceneImporter sceneImporter,
            string path,
            Transform parent,
            CancellationToken token)
        {
            ConfigureSceneImporter(sceneImporter, parent);

            var tcs = new TaskCompletionSource<GameObject>();
            var progress = new Progress<ImportProgress>(p =>
                NotifyStateChanged(ModelLoadEventArgs.CreateLoadProgressEvent(path, p.Progress)));

            token.Register(() => tcs.TrySetCanceled());

            await sceneImporter.LoadSceneAsync(
                showSceneObj: !_importSettings.HideSceneObjDuringLoad,
                onLoadComplete: (go, exInfo) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }

                    if (exInfo != null)
                    {
                        tcs.TrySetException(exInfo.SourceException);
                        return;
                    }

                    if (go != null)
                    {
                        go.SetActive(!_importSettings.HideModelAfterLoad);
                        tcs.TrySetResult(go);
                    }
                    else
                    {
                        tcs.TrySetException(new InvalidOperationException("Load completed but no GameObject was created"));
                    }
                },
                progress: progress
            );

            return await tcs.Task;
        }

        private void ConfigureSceneImporter(GLTFSceneImporter importer, Transform parent)
        {
            importer.SceneParent = parent;
            importer.Collider = _importSettings.ColliderType;
            importer.MaximumLod = _importSettings.MaximumLod;
            importer.Timeout = _importSettings.Timeout;
            importer.IsMultithreaded = _importSettings.Multithreaded;
        }

        private string GetFullPath(string path)
        {
            if (_importSettings.AppendStreamingAssets)
            {
                return Path.Combine(Application.streamingAssetsPath, path.TrimStart('/', '\\'));
            }
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                if (_importerFactory)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(_importerFactory);
                    }
                    catch (Exception) { }
                }
            }

            base.Dispose(disposing);
        }
    }
}