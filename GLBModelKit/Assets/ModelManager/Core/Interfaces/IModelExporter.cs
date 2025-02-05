using DevToolKit.Models.Events;
using System;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    public interface IModelExporter
    {
        event EventHandler<ModelExportEventArgs> ExportStateChanged;

        bool IsProcessing { get; }

        bool ExportToFile(string path, string fileName, Transform[] roots);
        byte[] ExportToStream(string fileName, Transform[] roots);
    }
}
