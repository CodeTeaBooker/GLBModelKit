using DevToolKit.Models.Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityGLTF;

namespace DevToolKit.Models.Config
{
    [CreateAssetMenu(fileName = "GLBModelExporterConfig", menuName = "Configs/GLBModelExporterConfig")]
    public class GLBModelExporterConfigSO : ModelConfigBase
    {
        [Header("Export Path Settings")]
        [Tooltip("Base path for exports")]
        public string ExportBasePath = "Exports";

        [Tooltip("Export file format")]
        public ExportFileFormat FileFormat = ExportFileFormat.GLB;

        [Header("Export Settings")]
        [Tooltip("Export object names")]
        public bool ExportNames = true;

        [Tooltip("Require GLTF extensions")]
        public bool RequireExtensions = false;

        [Tooltip("Export animations")]
        public bool ExportAnimations = true;

        [Header("Export Quality Settings")]
        [Tooltip("Export vertex colors")]
        public bool ExportVertexColors = true;

        [Range(0, 100)]
        [Tooltip("JPEG texture quality")]
        public int TextureQuality = 90;

        [Tooltip("Blend shape export properties")]
        public GLTFSettings.BlendShapeExportPropertyFlags BlendShapeExportProperties = GLTFSettings.BlendShapeExportPropertyFlags.All;

        [Header("Advanced Settings")]
        [Range(1, 10)]
        [Tooltip("Maximum retry attempts")]
        public int MaxRetryCount = 3;

        [Range(0.1f, 5f)]
        [Tooltip("Delay between retries (seconds)")]
        public float RetryDelay = 1.0f;

        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        protected override void ValidateConfigurationInternal(List<ValidationError> errors)
        {
            // Validate required fields
            ValidateRequired(errors, ExportBasePath, nameof(ExportBasePath));

            // Validate numerical ranges
            ValidateRange(errors, TextureQuality, 0, 100, nameof(TextureQuality));
            ValidateRange(errors, MaxRetryCount, 1, 10, nameof(MaxRetryCount));
            ValidateRange(errors, RetryDelay, 0.1f, 5f, nameof(RetryDelay));

            // Validate enum values
            ValidateEnum(errors, FileFormat, nameof(FileFormat));
            ValidateEnum(errors, BlendShapeExportProperties, nameof(BlendShapeExportProperties));
        }

        /// <summary>
        /// Validates default values
        /// </summary>
        protected override void ValidateDefaultValues(List<ValidationError> errors)
        {
            // Check if default values are optimal
            if (TextureQuality < 30)
            {
                errors.Add(new ValidationError(nameof(TextureQuality),
                    $"Default texture quality of {TextureQuality} is very low, consider increasing it for better visuals",
                    ValidationSeverity.Warning));
            }

            if (MaxRetryCount < 2)
            {
                errors.Add(new ValidationError(nameof(MaxRetryCount),
                    "Low retry count may lead to failure in unstable networks",
                    ValidationSeverity.Warning));
            }
        }

        /// <summary>
        /// Validates relationships between properties
        /// </summary>
        protected override void ValidateRelationships(List<ValidationError> errors)
        {
            // Check if the export path exists (warning only)
            if (!string.IsNullOrEmpty(ExportBasePath))
            {
                string fullPath = Path.GetFullPath(ExportBasePath);
                if (!Directory.Exists(fullPath))
                {
                    errors.Add(new ValidationError(nameof(ExportBasePath),
                        $"Export directory does not exist: {fullPath}",
                        ValidationSeverity.Warning));
                }
            }

            // Check property relationships
            if (RequireExtensions && !ExportNames)
            {
                errors.Add(new ValidationError(
                    $"{nameof(RequireExtensions)}/{nameof(ExportNames)}",
                    "When RequireExtensions is true, ExportNames should also be true for better compatibility",
                    ValidationSeverity.Warning));
            }

            // Check performance implications
            if (ExportVertexColors && TextureQuality > 90)
            {
                errors.Add(new ValidationError(
                    $"{nameof(ExportVertexColors)}/{nameof(TextureQuality)}",
                    "High texture quality combined with vertex colors export may impact performance",
                    ValidationSeverity.Warning));
            }

            // Check blend shape export properties
            if (BlendShapeExportProperties == GLTFSettings.BlendShapeExportPropertyFlags.None && ExportAnimations)
            {
                errors.Add(new ValidationError(
                    $"{nameof(BlendShapeExportProperties)}/{nameof(ExportAnimations)}",
                    "BlendShapeExportProperties is set to None but animations are enabled. This may result in limited animation support.",
                    ValidationSeverity.Warning));
            }
        }

        /// <summary>
        /// Creates export settings from the configuration
        /// </summary>
        public GLTFSettings CreateExportSettings()
        {
            var settings = GLTFSettings.GetOrCreateSettings();
            settings.ExportNames = this.ExportNames;
            settings.RequireExtensions = this.RequireExtensions;
            settings.ExportAnimations = this.ExportAnimations;
            settings.ExportVertexColors = this.ExportVertexColors;
            settings.DefaultJpegQuality = this.TextureQuality;
            settings.BlendShapeExportProperties = this.BlendShapeExportProperties;
            return settings;
        }

        public override string ToString()
        {
            return $"GLBModelExporterConfig[Format:{FileFormat}, Quality:{TextureQuality}, Retry:{MaxRetryCount}/{RetryDelay}s]";
        }
    }
}