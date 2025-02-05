using System;
using UnityEngine;

namespace DevToolKit.Models.Events
{
    public class ModelCacheEventArgs : EventArgs
    {
        public string ModelPath { get; }
        public GameObject CachedModel { get; }

        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }

        public long ModelSizeInMB { get; }

        public int CachedCount { get; }
        public int MaxCacheSize { get; }
        public long TotalCacheSize { get; }

        public CacheOperationType OperationType { get; }

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
            CachedModel = templateObject;
            ModelSizeInMB = templateFileSizeInMB;
            CachedCount = cachedCount;
            MaxCacheSize = maxCacheSize;
            TotalCacheSize = totalCacheSize;
        }

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

        public override string ToString()
        {
            return $"[{OperationType}] Path: {ModelPath}, Success: {IsSuccessful}, " +
                   $"Cache: {CachedCount}/{MaxCacheSize}, " +
                   $"Size: {ModelSizeInMB}MB, Total: {TotalCacheSize}MB" +
                   (ErrorMessage != null ? $", Error: {ErrorMessage}" : "");
        }
    }
}


//using System;
//using UnityEngine;

//namespace DevToolKit.Models.Events
//{
//    public class ModelCacheEventArgs : EventArgs
//    {
//        public string ModelPath { get; }
//        public GameObject CachedModel { get; }

//        public bool IsSuccessful { get; }
//        public string ErrorMessage { get; }

//        public long ModelSizeInMB { get; }
//        public int CachedModelCount { get; }
//        public int MaxCacheSize { get; }
//        public long TotalCacheSize { get; }

//        public CacheOperationType OperationType { get; }

//        private ModelCacheEventArgs(
//            string modelPath,
//            CacheOperationType operationType,
//            bool isSuccessful,
//            string errorMessage = null,
//            GameObject templateObject = null,
//            long templateFileSizeInMB = 0,
//            int cachedTemplateCount = 0,
//            int maxTemplateCacheSize = 0,
//            long totalCacheSize = 0)
//        {
//            ModelPath = modelPath;
//            OperationType = operationType;
//            IsSuccessful = isSuccessful;
//            ErrorMessage = errorMessage;
//            CachedModel = templateObject;
//            ModelSizeInMB = templateFileSizeInMB;
//            CachedModelCount = cachedTemplateCount;
//            MaxCacheSize = maxTemplateCacheSize;
//            TotalCacheSize = totalCacheSize;
//        }

//        public static ModelCacheEventArgs CreateAddedEvent(
//            string modelPath,
//            GameObject templateObject,
//            long templateFileSizeInMB,
//            int cachedTemplateCount,
//            long totalCacheSize)
//        {
//            return new ModelCacheEventArgs(
//                modelPath,
//                CacheOperationType.Added,
//                true,
//                templateObject: templateObject,
//                templateFileSizeInMB: templateFileSizeInMB,
//                cachedTemplateCount: cachedTemplateCount,
//                totalCacheSize: totalCacheSize);
//        }

//        public static ModelCacheEventArgs CreateRemovedEvent(
//            string modelPath,
//            GameObject templateObject,
//            int cachedTemplateCount,
//            long totalCacheSize)
//        {
//            return new ModelCacheEventArgs(
//                modelPath,
//                CacheOperationType.Removed,
//                true,
//                templateObject: templateObject,
//                cachedTemplateCount: cachedTemplateCount,
//                totalCacheSize: totalCacheSize);
//        }

//        public static ModelCacheEventArgs CreateClearedEvent(
//            int previousTemplateCount,
//            long previousTotalSize)
//        {
//            return new ModelCacheEventArgs(
//                string.Empty,
//                CacheOperationType.Cleared,
//                true,
//                cachedTemplateCount: 0,
//                maxTemplateCacheSize: previousTemplateCount,
//                totalCacheSize: 0);
//        }

//        public static ModelCacheEventArgs CreateDuplicateEvent(
//            string modelPath,
//            int cachedTemplateCount,
//            long totalCacheSize)
//        {
//            return new ModelCacheEventArgs(
//                modelPath,
//                CacheOperationType.Error,
//                false,
//                errorMessage: "Template already exists in cache",
//                cachedTemplateCount: cachedTemplateCount,
//                totalCacheSize: totalCacheSize);
//        }

//        public static ModelCacheEventArgs CreateLimitEvent(
//            string modelPath,
//            int cachedTemplateCount,
//            int maxTemplateCacheSize,
//            long totalCacheSize)
//        {
//            return new ModelCacheEventArgs(
//                modelPath,
//                CacheOperationType.Error,
//                false,
//                errorMessage: $"Cache limit reached ({cachedTemplateCount}/{maxTemplateCacheSize})",
//                cachedTemplateCount: cachedTemplateCount,
//                maxTemplateCacheSize: maxTemplateCacheSize,
//                totalCacheSize: totalCacheSize);
//        }

//        public static ModelCacheEventArgs CreateSizeLimitEvent(
//            string modelPath,
//            long templateFileSizeInMB,
//            long maxTemplateFileSize,
//            long totalCacheSize)
//        {
//            return new ModelCacheEventArgs(
//                modelPath,
//                CacheOperationType.Error,
//                false,
//                errorMessage: $"File size ({templateFileSizeInMB} MB) exceeds limit ({maxTemplateFileSize} MB)",
//                templateFileSizeInMB: templateFileSizeInMB,
//                totalCacheSize: totalCacheSize);
//        }

//        public static ModelCacheEventArgs CreateErrorEvent(
//            string modelPath,
//            string errorMessage,
//            int cachedTemplateCount,
//            long totalCacheSize)
//        {
//            return new ModelCacheEventArgs(
//                modelPath,
//                CacheOperationType.Error,
//                false,
//                errorMessage: errorMessage,
//                cachedTemplateCount: cachedTemplateCount,
//                totalCacheSize: totalCacheSize);
//        }

//        public override string ToString()
//        {
//            return $"[{OperationType}] Path: {ModelPath}, Success: {IsSuccessful}, " +
//                   $"Cache: {CachedModelCount}/{MaxCacheSize}, " +
//                   $"Size: {ModelSizeInMB}MB, Total: {TotalCacheSize}MB" +
//                   (ErrorMessage != null ? $", Error: {ErrorMessage}" : "");
//        }
//    }
//}
