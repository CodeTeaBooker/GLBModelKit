//using DevToolKit.Models.Core;
//using System;
//using System.Collections.Generic;
//using UnityEngine;

//namespace DevToolKit.Models.Events
//{
//    public class EventDispatcher : IDisposable
//    {
//        private readonly List<IModelCacheListener> _listeners = new List<IModelCacheListener>();
//        private bool _disposed;
//        public void AddListener(IModelCacheListener listener)
//        {
//            if (_disposed)
//                throw new ObjectDisposedException(nameof(EventDispatcher));
//            if (listener == null)
//                return;
//            if (!_listeners.Contains(listener))
//                _listeners.Add(listener);
//        }
//        public void RemoveListener(IModelCacheListener listener)
//        {
//            if (!_disposed && listener != null)
//                _listeners.Remove(listener);
//        }
//        public void NotifyStateChanged(ModelCacheEventArgs args)
//        {
//            if (_disposed)
//                return;
//            foreach (var listener in _listeners.ToArray())
//            {
//                try { listener.OnCacheStateChanged(args); }
//                catch (Exception ex) { Debug.LogError($"[EventDispatcher] Error notifying listener: {ex.Message}"); }
//            }
//        }
//        public void Dispose()
//        {
//            _disposed = true;
//            _listeners.Clear();
//        }
//    }
//}


using DevToolKit.Models.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevToolKit.Models.Events
{
    /// <summary>
    /// A thread-safe event dispatcher for model cache events with performance optimizations
    /// </summary>
    public class EventDispatcher : IDisposable
    {
        private readonly object _lock = new object();
        private readonly List<IModelCacheListener> _eventListeners = new List<IModelCacheListener>();
        private volatile IModelCacheListener[] _cachedEventListeners;
        private bool _disposed;
        private const string LOG_DOMAIN = nameof(EventDispatcher);

        /// <summary>
        /// Adds a new event listener if it's not already registered
        /// </summary>
        /// <param name="listener">The listener to add</param>
        /// <exception cref="ObjectDisposedException">Thrown if the dispatcher is disposed</exception>
        public void AddListener(IModelCacheListener listener)
        {
            ThrowIfDisposed();
            if (listener == null) return;

            lock (_lock)
            {
                if (!_eventListeners.Contains(listener))
                {
                    _eventListeners.Add(listener);
                    _cachedEventListeners = null; // Invalidate cache
                }
            }
        }

        /// <summary>
        /// Removes an event listener if it exists
        /// </summary>
        /// <param name="listener">The listener to remove</param>
        public void RemoveListener(IModelCacheListener listener)
        {
            if (_disposed) return;
            if (listener == null) return;

            lock (_lock)
            {
                if (_eventListeners.Remove(listener))
                {
                    _cachedEventListeners = null; // Invalidate cache
                }
            }
        }

        /// <summary>
        /// Notifies all registered listeners of a state change
        /// </summary>
        /// <param name="args">Event arguments containing the state change information</param>
        public void NotifyStateChanged(ModelCacheEventArgs args)
        {
            ThrowIfDisposed();

            var listeners = _cachedEventListeners;
            if (listeners == null)
            {
                lock (_lock)
                {
                    if (_disposed) return;
                    _cachedEventListeners = _eventListeners.ToArray();
                    listeners = _cachedEventListeners;
                }
            }

            foreach (var listener in listeners)
            {
                try
                {
                    listener?.OnCacheStateChanged(args);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{LOG_DOMAIN}] Error notifying listener: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Removes all registered listeners
        /// </summary>
        public void Clear()
        {
            if (_disposed) return;

            lock (_lock)
            {
                _eventListeners.Clear();
                _cachedEventListeners = null;
            }
        }

        /// <summary>
        /// Checks if the dispatcher is disposed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the dispatcher is disposed</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Gets the current count of registered listeners
        /// </summary>
        public int ListenerCount
        {
            get
            {
                lock (_lock)
                {
                    return _eventListeners.Count;
                }
            }
        }

        /// <summary>
        /// Disposes the dispatcher and clears all listeners
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
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
