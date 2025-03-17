using System;
using UnityEngine;

namespace DevToolKit.Models.Events
{
    /// <summary>
    /// Event arguments for model cache state changes with optimized memory usage.
    /// </summary>
    public class ModelCacheEventArgs : EventArgs
    {
        // Core properties - always initialized
        public string ModelPath { get; }
        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }
        public CacheOperationType OperationType { get; }

        // Additional data encapsulated in a struct to reduce memory overhead
        private readonly CacheStats _stats;

        // Cached string representation for better performance
        private string _cachedToString;

        /// <summary>
        /// Statistics about the cache state.
        /// </summary>
        public struct CacheStats
        {
            public GameObject CachedModel;
            public long ModelSizeInMB;
            public int CachedCount;
            public int MaxCacheSize;
            public long TotalCacheSize;

            public CacheStats(
                GameObject model = null,
                long modelSize = 0,
                int cachedCount = 0,
                int maxCacheSize = 0,
                long totalCacheSize = 0)
            {
                CachedModel = model;
                ModelSizeInMB = modelSize;
                CachedCount = cachedCount;
                MaxCacheSize = maxCacheSize;
                TotalCacheSize = totalCacheSize;
            }
        }

        /// <summary>
        /// Gets the cached model associated with this event.
        /// </summary>
        public GameObject CachedModel => _stats.CachedModel;

        /// <summary>
        /// Gets the size of the model in MB.
        /// </summary>
        public long ModelSizeInMB => _stats.ModelSizeInMB;

        /// <summary>
        /// Gets the current count of cached models.
        /// </summary>
        public int CachedCount => _stats.CachedCount;

        /// <summary>
        /// Gets the maximum allowed cache size.
        /// </summary>
        public int MaxCacheSize => _stats.MaxCacheSize;

        /// <summary>
        /// Gets the total size of all cached models in MB.
        /// </summary>
        public long TotalCacheSize => _stats.TotalCacheSize;

        /// <summary>
        /// Private constructor for creating cache event args.
        /// </summary>
        private ModelCacheEventArgs(
            string modelPath,
            CacheOperationType operationType,
            bool isSuccessful,
            string errorMessage = null,
            GameObject templateObject = null,
            long templateFileSizeInMB = 0,
            int cachedCount = 0,
            int maxCacheSize = 0,
            long totalCacheSize = 0)
        {
            ModelPath = modelPath;
            OperationType = operationType;
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;

            _stats = new CacheStats(
                templateObject,
                templateFileSizeInMB,
                cachedCount,
                maxCacheSize,
                totalCacheSize
            );
        }

        /// <summary>
        /// Creates an event for when a model is added to the cache.
        /// </summary>
        public static ModelCacheEventArgs CreateAddedEvent(
            string modelPath,
            GameObject templateObject,
            long templateFileSizeInMB,
            int cachedTemplateCount,
            long totalCacheSize)
        {
            return new ModelCacheEventArgs(
                modelPath,
                CacheOperationType.Added,
                true,
                templateObject: templateObject,
                templateFileSizeInMB: templateFileSizeInMB,
                cachedCount: cachedTemplateCount,
                totalCacheSize: totalCacheSize);
        }

        /// <summary>
        /// Creates an event for when a model is removed from the cache.
        /// </summary>
        public static ModelCacheEventArgs CreateRemovedEvent(
            string modelPath,
            GameObject templateObject,
            int cachedTemplateCount,
            long totalCacheSize)
        {
            return new ModelCacheEventArgs(
                modelPath,
                CacheOperationType.Removed,
                true,
                templateObject: templateObject,
                cachedCount: cachedTemplateCount,
                totalCacheSize: totalCacheSize);
        }

        /// <summary>
        /// Creates an event for when the cache is cleared.
        /// </summary>
        public static ModelCacheEventArgs CreateClearedEvent(
            int previousTemplateCount,
            int maxTemplateCacheSize,
            long previousSize)
        {
            return new ModelCacheEventArgs(
                string.Empty,
                CacheOperationType.Cleared,
                true,
                cachedCount: 0,
                maxCacheSize: maxTemplateCacheSize,
                totalCacheSize: 0);
        }

        /// <summary>
        /// Creates an event for when a duplicate model is detected.
        /// </summary>
        public static ModelCacheEventArgs CreateDuplicateEvent(
            string modelPath,
            int cachedTemplateCount,
            long totalCacheSize)
        {
            return new ModelCacheEventArgs(
                modelPath,
                CacheOperationType.Error,
                false,
                errorMessage: "Template already exists in cache",
                cachedCount: cachedTemplateCount,
                totalCacheSize: totalCacheSize);
        }

        /// <summary>
        /// Creates an event for when the cache limit is reached.
        /// </summary>
        public static ModelCacheEventArgs CreateLimitEvent(
            string modelPath,
            int cachedCount,
            int maxCacheSize,
            long totalCacheSize)
        {
            return new ModelCacheEventArgs(
                modelPath,
                CacheOperationType.Error,
                false,
                errorMessage: $"Cache limit reached ({cachedCount}/{maxCacheSize})",
                cachedCount: cachedCount,
                maxCacheSize: maxCacheSize,
                totalCacheSize: totalCacheSize);
        }

        /// <summary>
        /// Creates an event for when the file size limit is exceeded.
        /// </summary>
        public static ModelCacheEventArgs CreateSizeLimitEvent(
            string modelPath,
            long templateFileSizeInMB,
            long maxTemplateFileSize,
            long totalCacheSize)
        {
            return new ModelCacheEventArgs(
                modelPath,
                CacheOperationType.Error,
                false,
                errorMessage: $"File size ({templateFileSizeInMB} MB) exceeds limit ({maxTemplateFileSize} MB)",
                templateFileSizeInMB: templateFileSizeInMB,
                totalCacheSize: totalCacheSize);
        }

        /// <summary>
        /// Creates an event for general cache errors.
        /// </summary>
        public static ModelCacheEventArgs CreateErrorEvent(
            string modelPath,
            string errorMessage,
            int cachedTemplateCount,
            long totalCacheSize)
        {
            return new ModelCacheEventArgs(
                modelPath,
                CacheOperationType.Error,
                false,
                errorMessage: errorMessage,
                cachedCount: cachedTemplateCount,
                totalCacheSize: totalCacheSize);
        }

        /// <summary>
        /// Returns a string representation of the event arguments.
        /// Uses lazy initialization for better performance.
        /// </summary>
        public override string ToString()
        {
            if (_cachedToString == null)
            {
                _cachedToString = $"[{OperationType}] Path: {ModelPath}, Success: {IsSuccessful}, " +
                   $"Cache: {CachedCount}/{MaxCacheSize}, " +
                   $"Size: {ModelSizeInMB}MB, Total: {TotalCacheSize}MB" +
                   (ErrorMessage != null ? $", Error: {ErrorMessage}" : "");
            }
            return _cachedToString;
        }
    }
}