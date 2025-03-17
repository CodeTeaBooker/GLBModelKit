using DevToolKit.Models.Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DevToolKit.Models.Config
{
    [CreateAssetMenu(fileName = "GLBModelManagerConfig", menuName = "Configs/GLBModelManagerConfig")]
    public class GLBModelManagerConfigSO : ModelConfigBase
    {
        [Header("Cache Settings")]
        [Range(1, 10)]
        [Tooltip("Maximum number of cached models")]
        public int MaxCacheSize = 5;

        [Range(1, 100)]
        [Tooltip("Maximum size of cached models in MB")]
        public int MaxFileSizeInMB = 100;

        [Header("Model Settings")]
        [Tooltip("Path to the GLB model file (for quick test)")]
        public string ModelPath = "Models/MyModel.glb";

        [Tooltip("Load model automatically on start")]
        public bool ImportOnStart = false;

        [Header("Retry Settings")]
        [Range(1, 10)]
        [Tooltip("Maximum number of retry attempts")]
        public int MaxRetryCount = 3;

        [Range(0.1f, 5f)]
        [Tooltip("Delay between retry attempts in seconds")]
        public float RetryDelay = 1.0f;

        [Header("Debug Settings")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        [Header("Import Configuration")]
        [SerializeField]
        [Tooltip("GLB model import configuration")]
        private GLBModelImportConfigSO _importConfig;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        protected override void ValidateConfigurationInternal(List<ValidationError> errors)
        {
            // Validate numerical ranges
            ValidateRange(errors, MaxCacheSize, 1, 10, nameof(MaxCacheSize));
            ValidateRange(errors, MaxFileSizeInMB, 1, 100, nameof(MaxFileSizeInMB));
            ValidateRange(errors, MaxRetryCount, 1, 10, nameof(MaxRetryCount));
            ValidateRange(errors, RetryDelay, 0.1f, 5f, nameof(RetryDelay));

            // Validate required values
            if (ImportOnStart)
            {
                ValidateRequired(errors, ModelPath, nameof(ModelPath));
            }
        }

        /// <summary>
        /// Validates default values
        /// </summary>
        protected override void ValidateDefaultValues(List<ValidationError> errors)
        {
            // Check if cache size is reasonable
            if (MaxCacheSize < 3 && ImportOnStart)
            {
                errors.Add(new ValidationError(nameof(MaxCacheSize),
                    "Small cache size with auto-import enabled may lead to frequent cache misses",
                    ValidationSeverity.Warning));
            }

            // Check file size limit
            if (MaxFileSizeInMB < 10)
            {
                errors.Add(new ValidationError(nameof(MaxFileSizeInMB),
                    "Very small file size limit may prevent loading detailed models",
                    ValidationSeverity.Warning));
            }

            // Check retry settings
            if (MaxRetryCount < 2 && Application.internetReachability != NetworkReachability.NotReachable)
            {
                errors.Add(new ValidationError(nameof(MaxRetryCount),
                    "Low retry count may lead to failures in unstable networks",
                    ValidationSeverity.Warning));
            }
        }

        /// <summary>
        /// Validates relationships between properties
        /// </summary>
        protected override void ValidateRelationships(List<ValidationError> errors)
        {
            // Validate import config is assigned when auto-import is enabled
            if (ImportOnStart && _importConfig == null)
            {
                errors.Add(new ValidationError(nameof(_importConfig),
                    "Import configuration is missing but ImportOnStart is enabled",
                    ValidationSeverity.Error));
            }

            // Check if ModelPath exists
            if (ImportOnStart && !string.IsNullOrEmpty(ModelPath))
            {
                string testPath = ModelPath;
                if (_importConfig != null && _importConfig.AppendStreamingAssets)
                {
                    testPath = Path.Combine(Application.streamingAssetsPath, ModelPath.TrimStart('/', '\\'));
                }

                if (!File.Exists(testPath))
                {
                    errors.Add(new ValidationError(nameof(ModelPath),
                        $"Model file not found at path: {testPath}",
                        ValidationSeverity.Warning));
                }
            }

            // Check nested configuration
            if (_importConfig != null)
            {
                // Create validation context for nested validation
                var context = new ValidationContext(this);
                var importContext = context.CreateChildContext(_importConfig, nameof(_importConfig));

                // Validate import config
                if (!_importConfig.ValidateWithContext(importContext))
                {
                    // Merge errors from child context
                    context.MergeChildContext(importContext);
                    errors.AddRange(context.Errors);
                }

                // Check specific relationship between manager and import config
                if (_importConfig.Timeout > 0 && _importConfig.Timeout * MaxRetryCount > 30)
                {
                    errors.Add(new ValidationError(
                        $"{nameof(_importConfig)}.{nameof(_importConfig.Timeout)}/{nameof(MaxRetryCount)}",
                        "Total potential timeout (Timeout * MaxRetryCount) exceeds 30 seconds which may cause poor user experience",
                        ValidationSeverity.Warning));
                }
            }

            // Memory considerations
            if (MaxCacheSize > 5 && MaxFileSizeInMB > 50)
            {
                errors.Add(new ValidationError(
                    $"{nameof(MaxCacheSize)}/{nameof(MaxFileSizeInMB)}",
                    $"Large cache size ({MaxCacheSize}) combined with large file size limit ({MaxFileSizeInMB}MB) may consume excessive memory",
                    ValidationSeverity.Warning));
            }

            // Platform specific checks
            if (Application.isMobilePlatform)
            {
                if (MaxFileSizeInMB > 50)
                {
                    errors.Add(new ValidationError(nameof(MaxFileSizeInMB),
                        "Large file size limit may cause memory issues on mobile platforms",
                        ValidationSeverity.Warning));
                }

                if (MaxCacheSize > 3)
                {
                    errors.Add(new ValidationError(nameof(MaxCacheSize),
                        "Large cache size may cause memory issues on mobile platforms",
                        ValidationSeverity.Warning));
                }
            }
        }

        /// <summary>
        /// Validate with context (for nested validation)
        /// </summary>
        protected override void ValidateConfigurationInternalWithContext(ValidationContext context)
        {
            var errors = new List<ValidationError>();
            ValidateConfigurationInternal(errors);
            context.AddErrors(errors);

            ValidateDefaultValues(errors);
            context.AddErrors(errors);

            // Don't call ValidateRelationships here to avoid infinite recursion
            // with nested configs
        }

        /// <summary>
        /// Creates an import configuration from the scriptable object
        /// </summary>
        public GLBModelImportConfig CreateImportConfig()
        {
            if (_importConfig == null)
            {
                Debug.LogWarning($"[{nameof(GLBModelManagerConfigSO)}] Import config is not assigned, using default settings");
                return new GLBModelImportConfig();
            }
            return _importConfig.CreateImportConfig();
        }

        public override string ToString()
        {
            return $"GLBModelManagerConfig[Cache:{MaxCacheSize}/{MaxFileSizeInMB}MB, Retry:{MaxRetryCount}/{RetryDelay}s, AutoImport:{ImportOnStart}]";
        }
    }
}