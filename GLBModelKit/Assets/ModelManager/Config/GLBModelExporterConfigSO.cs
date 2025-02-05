using DevToolKit.Models.Core;
using System.Collections.Generic;
using System.Linq;
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

        protected override string[] ValidateConfiguration()
        {
            var errors = new List<string>();

           
            if (string.IsNullOrEmpty(ExportBasePath))
                errors.Add($"{nameof(ExportBasePath)} cannot be empty");

          
            if (TextureQuality < 0 || TextureQuality > 100)
                errors.Add($"{nameof(TextureQuality)} must be between 0 and 100");

            if (MaxRetryCount < 1 || MaxRetryCount > 10)
                errors.Add($"{nameof(MaxRetryCount)} must be between 1 and 10");

            if (RetryDelay < 0.1f || RetryDelay > 5f)
                errors.Add($"{nameof(RetryDelay)} must be between 0.1 and 5");

            return errors.Where(e => !string.IsNullOrEmpty(e)).ToArray();
        }

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
