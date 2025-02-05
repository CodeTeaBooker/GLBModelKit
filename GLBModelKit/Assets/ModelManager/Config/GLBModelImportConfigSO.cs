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

        protected override string[] ValidateConfiguration()
        {
            var errors = new List<string>();
            string error;

            error = ValidateRange(MaximumLod, 100, 1000, nameof(MaximumLod));
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            error = ValidateRange(Timeout, 0, 16, nameof(Timeout));
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);

            return errors.ToArray();
        }


        public GLBModelImportConfig CreateImportConfig()
        {
            if (!Validate(out string error))
            {
                Debug.LogError($"[{nameof(GLBModelImportConfigSO)}] Invalid config: {error}");
                return new GLBModelImportConfig();
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
