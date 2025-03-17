using DevToolKit.Models.Config;
using DevToolKit.Models.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityGLTF;

namespace DevToolKit.Models.Exporters
{
    public class GLBModelExporter : BaseModelExporter
    {
        private readonly GLBModelExporterConfigSO _config;

        public GLBModelExporter(GLBModelExporterConfigSO config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            if (!_config.Validate(out IReadOnlyList<ModelConfigBase.ValidationError> errors))
            {
                var errorMessages = string.Join(
                    Environment.NewLine,
                    errors.Where(e => e.Severity >= ModelConfigBase.ValidationSeverity.Error)
                          .Select(e => $"{e.PropertyName}: {e.Message}")
                );

                throw new ArgumentException($"Exporter config invalid: {errorMessages}");
            }
        }

        protected override bool ExportToFileImplementation(string path, string fileName, Transform[] roots)
        {
            try
            {
                string directory = Path.Combine(_config.ExportBasePath, path ?? "");
                Directory.CreateDirectory(directory);

                var exporter = new GLTFSceneExporter(roots, new ExportContext(_config.CreateExportSettings()));
                string extension = _config.FileFormat == ExportFileFormat.GLB ? ".glb" : ".gltf";
                string fullPath = Path.Combine(directory, fileName + extension);

                if (_config.FileFormat == ExportFileFormat.GLB)
                {
                    exporter.SaveGLB(directory, fileName);
                }
                else
                {
                    exporter.SaveGLTFandBin(directory, fileName);
                }

                return File.Exists(fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GLBModelExporter] ExportToFile error: {ex.Message}");
                return false;
            }
        }

        protected override byte[] ExportToStreamImplementation(string fileName, Transform[] roots)
        {
            try
            {
                var exporter = new GLTFSceneExporter(roots, new ExportContext(_config.CreateExportSettings()));
                return exporter.SaveGLBToByteArray(fileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GLBModelExporter] ExportToStream error: {ex.Message}");
                return null;
            }
        }
    }
}
