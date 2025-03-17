using DevToolKit.Models.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityGLTF;

namespace DevToolKit.Models.Config
{
    [CreateAssetMenu(fileName = "GLBModelImportConfig", menuName = "Configs/GLBModelImportConfig")]
    public class GLBModelImportConfigSO : ModelConfigBase
    {
        [Header("Path Settings")]
        [Tooltip("Append StreamingAssets path to model path")]
        public bool AppendStreamingAssets = true;

        [Header("Model Settings")]
        [Tooltip("Type of collider to generate")]
        public GLTFSceneImporter.ColliderType ColliderType = GLTFSceneImporter.ColliderType.None;

        [Range(100, 1000)]
        [Tooltip("Maximum LOD level for imported models")]
        public int MaximumLod = 300;

        [Header("Import Settings")]
        [Range(0, 16)]
        [Tooltip("Import operation timeout in seconds")]
        public int Timeout = 8;

        [Tooltip("Enable multithreaded import")]
        public bool Multithreaded = true;

        [Tooltip("Hide scene object during loading process")]
        public bool HideSceneObjDuringLoad = false;

        [Tooltip("Hide model after loading completes")]
        public bool HideModelAfterLoad = false;

        [Header("Advanced Import Settings")]
        [Tooltip("Normal import mode")]
        public GLTFImporterNormals ImportNormals = GLTFImporterNormals.Import;

        [Tooltip("Tangent import mode")]
        public GLTFImporterNormals ImportTangents = GLTFImporterNormals.Import;

        [Tooltip("Swap UV coordinates during import")]
        public bool SwapUVs = false;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        protected override void ValidateConfigurationInternal(List<ValidationError> errors)
        {
            // Validate numerical ranges
            ValidateRange(errors, MaximumLod, 100, 1000, nameof(MaximumLod));
            ValidateRange(errors, Timeout, 0, 16, nameof(Timeout));

            // Validate enum values
            ValidateEnum(errors, ColliderType, nameof(ColliderType));
            ValidateEnum(errors, ImportNormals, nameof(ImportNormals));
            ValidateEnum(errors, ImportTangents, nameof(ImportTangents));
        }

        /// <summary>
        /// Validates default values
        /// </summary>
        protected override void ValidateDefaultValues(List<ValidationError> errors)
        {
            // Check if timeout is reasonable
            if (Timeout < 4 && Multithreaded)
            {
                errors.Add(new ValidationError(nameof(Timeout),
                    "Timeout value may be too low for multithreaded imports. Consider increasing it to at least 4 seconds.",
                    ValidationSeverity.Warning));
            }

            // Check LOD level
            if (MaximumLod > 500)
            {
                errors.Add(new ValidationError(nameof(MaximumLod),
                    "High LOD values may impact performance without significant visual improvement.",
                    ValidationSeverity.Warning));
            }
        }

        /// <summary>
        /// Validates relationships between properties
        /// </summary>
        protected override void ValidateRelationships(List<ValidationError> errors)
        {
            // Check collider type against platform constraints
            if (ColliderType != GLTFSceneImporter.ColliderType.None &&
                Application.platform == RuntimePlatform.WebGLPlayer)
            {
                errors.Add(new ValidationError(nameof(ColliderType),
                    "Collider generation may impact performance on WebGL platform.",
                    ValidationSeverity.Warning));
            }

            // Check normals and tangents consistency
            if (ImportNormals == GLTFImporterNormals.Calculate &&
                ImportTangents == GLTFImporterNormals.Import)
            {
                errors.Add(new ValidationError(
                    $"{nameof(ImportNormals)}/{nameof(ImportTangents)}",
                    "When calculating normals, it's usually better to calculate tangents as well.",
                    ValidationSeverity.Warning));
            }

            // Check performance implications
            if (Multithreaded && SystemInfo.processorCount <= 2)
            {
                errors.Add(new ValidationError(nameof(Multithreaded),
                    "Multithreaded import may not be efficient on systems with few CPU cores.",
                    ValidationSeverity.Warning));
            }

            // Mobile platform specific warnings
            if (Application.isMobilePlatform)
            {
                if (MaximumLod > 300)
                {
                    errors.Add(new ValidationError(nameof(MaximumLod),
                        "High LOD values may cause performance issues on mobile platforms.",
                        ValidationSeverity.Warning));
                }

                if (ColliderType == GLTFSceneImporter.ColliderType.Mesh)
                {
                    errors.Add(new ValidationError(nameof(ColliderType),
                        "Mesh colliders can be performance intensive on mobile platforms.",
                        ValidationSeverity.Warning));
                }
            }
        }

        /// <summary>
        /// Creates an import configuration from the scriptable object
        /// </summary>
        public GLBModelImportConfig CreateImportConfig()
        {
            // Validate before creating, but only log warnings
            Validate(out IReadOnlyList<ValidationError> errors);
            foreach (var error in errors)
            {
                if (error.Severity >= ValidationSeverity.Error)
                {
                    Debug.LogError($"[{nameof(GLBModelImportConfigSO)}] Config error: {error}");
                }
                else
                {
                    Debug.LogWarning($"[{nameof(GLBModelImportConfigSO)}] Config warning: {error}");
                }
            }

            return new GLBModelImportConfig
            {
                AppendStreamingAssets = this.AppendStreamingAssets,
                ColliderType = this.ColliderType,
                MaximumLod = this.MaximumLod,
                Timeout = this.Timeout,
                Multithreaded = this.Multithreaded,
                HideSceneObjDuringLoad = this.HideSceneObjDuringLoad,
                HideModelAfterLoad = this.HideModelAfterLoad,
                ImportNormals = this.ImportNormals,
                ImportTangents = this.ImportTangents,
                SwapUVs = this.SwapUVs
            };
        }
    }

    /// <summary>
    /// Runtime import configuration 
    /// </summary>
    public class GLBModelImportConfig
    {
        public bool AppendStreamingAssets { get; set; } = true;
        public GLTFSceneImporter.ColliderType ColliderType { get; set; } = GLTFSceneImporter.ColliderType.None;
        public int MaximumLod { get; set; } = 300;
        public int Timeout { get; set; } = 8;
        public bool Multithreaded { get; set; } = true;
        public bool HideSceneObjDuringLoad { get; set; } = false;
        public bool HideModelAfterLoad { get; set; } = false;
        public GLTFImporterNormals ImportNormals { get; set; } = GLTFImporterNormals.Import;
        public GLTFImporterNormals ImportTangents { get; set; } = GLTFImporterNormals.Import;
        public bool SwapUVs { get; set; } = false;

        /// <summary>
        /// Creates a deep copy of the configuration
        /// </summary>
        public GLBModelImportConfig Clone()
        {
            return (GLBModelImportConfig)this.MemberwiseClone();
        }

        public override string ToString()
        {
            return $"GLBModelImportConfig[LOD:{MaximumLod}, Timeout:{Timeout}s, Multithreaded:{Multithreaded}, AppendStreamingAssets:{AppendStreamingAssets}]";
        }
    }
}