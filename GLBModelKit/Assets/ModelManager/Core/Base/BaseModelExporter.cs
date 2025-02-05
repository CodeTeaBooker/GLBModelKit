using DevToolKit.Models.Events;
using System;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    public abstract class BaseModelExporter : IModelExporter, IDisposable
    {
        public event EventHandler<ModelExportEventArgs> ExportStateChanged;

        private volatile bool _disposed;
        private volatile bool _isProcessing;
        private readonly object _lock = new object();

        public bool IsProcessing
        {
            get { lock (_lock) { return _isProcessing; } }
            protected set { lock (_lock) { _isProcessing = value; } }
        }

        public bool ExportToFile(string path, string fileName, Transform[] roots)
        {
            ThrowIfDisposed();

            if (IsProcessing)
            {
                Debug.LogWarning("[BaseModelExporter] Export in progress");
                NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(path, "Exporter busy"));
                return false;
            }

            IsProcessing = true;

            try
            {
                if (!ValidateExport(path, fileName, roots))
                {
                    NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(path, "Validation failed"));
                    return false;
                }

                NotifyStateChanged(ModelExportEventArgs.CreateExportStartEvent(path));

                bool success = ExportToFileImplementation(path, fileName, roots);
                if (success)
                {
                    NotifyStateChanged(ModelExportEventArgs.CreateExportCompletedEvent(path, null));
                    return true;
                }

                NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(path, "Export failed"));
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaseModelExporter] Export error: {ex.Message}");
                NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(path, $"Export error: {ex.Message}"));
                return false;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        public byte[] ExportToStream(string fileName, Transform[] roots)
        {
            ThrowIfDisposed();

            if (IsProcessing)
            {
                Debug.LogWarning("[BaseModelExporter] Export in progress");
                NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(fileName, "Exporter busy"));
                return null;
            }

            IsProcessing = true;

            try
            {
                if (!ValidateExport(null, fileName, roots))
                {
                    NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(fileName, "Validation failed"));
                    return null;
                }

                NotifyStateChanged(ModelExportEventArgs.CreateExportStartEvent(fileName));

                var data = ExportToStreamImplementation(fileName, roots);
                if (data != null)
                {
                    NotifyStateChanged(ModelExportEventArgs.CreateExportCompletedEvent(fileName, null));
                    return data;
                }

                NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(fileName, "Export failed"));
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BaseModelExporter] Export error: {ex.Message}");
                NotifyStateChanged(ModelExportEventArgs.CreateExportFailedEvent(fileName, $"Export error: {ex.Message}"));
                return null;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        protected abstract bool ExportToFileImplementation(string path, string fileName, Transform[] roots);
        protected abstract byte[] ExportToStreamImplementation(string fileName, Transform[] roots);

        protected virtual bool ValidateExport(string path, string fileName, Transform[] roots)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("[BaseModelExporter] Filename is empty");
                return false;
            }
            if (roots == null || roots.Length == 0)
            {
                Debug.LogError("[BaseModelExporter] No roots provided");
                return false;
            }
            return true;
        }

        protected void NotifyStateChanged(ModelExportEventArgs args)
        {
            if (!_disposed)
            {
                try
                {
                    ExportStateChanged?.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BaseModelExporter] Event handler error: {ex.Message}");
                }
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public virtual void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    ExportStateChanged = null;
                }
            }
        }
    }
}