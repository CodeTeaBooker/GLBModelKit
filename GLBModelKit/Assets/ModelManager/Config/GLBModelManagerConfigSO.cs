using DevToolKit.Models.Core;
using System.Collections.Generic;
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

        //[Tooltip("Parent transform for the loaded model")]
        //public Transform RootTransform;

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

        protected override string[] ValidateConfiguration()
        {
            var errors = new List<string>();
            string error;

            error = ValidateRange(MaxCacheSize, 1, 10, nameof(MaxCacheSize));
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            error = ValidateRange(MaxFileSizeInMB, 1, 100, nameof(MaxFileSizeInMB));
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            error = ValidateRange(MaxRetryCount, 1, 10, nameof(MaxRetryCount));
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            error = ValidateRange(RetryDelay, 0.1f, 5f, nameof(RetryDelay));
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            if (_importConfig != null)
            {
                if (!_importConfig.Validate(out string configError))
                {
                    if (!string.IsNullOrEmpty(configError))
                        errors.Add($"Import config validation failed: {configError}");
                }
            }

            return errors.ToArray();
        }



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
