using System;
using System.IO;
using System.Threading.Tasks;
using UnityGLTF.Loader;

namespace DevToolKit.Models.Loaders
{
    /// <summary>
    /// A disposable wrapper for UnityWebRequestLoader
    /// </summary>
    public class DisposableWebRequestLoader : IDataLoader, IDisposable
    {
        private UnityWebRequestLoader _innerLoader;
        private bool _disposed;

        public DisposableWebRequestLoader(string directory)
        {
            _innerLoader = new UnityWebRequestLoader(directory);
        }

        public async Task<Stream> LoadStreamAsync(string relativeFilePath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DisposableWebRequestLoader));

            return await _innerLoader.LoadStreamAsync(relativeFilePath);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _innerLoader = null;
            }
        }
    }
}