using System;
using UnityEngine;

namespace DevToolKit.Models.Events
{
    public class ModelExportEventArgs : EventArgs
    {
        public string FilePath { get; private set; }
        public GameObject ExportedObject { get; private set; }

        public bool IsSuccessful { get; private set; }
        public string ErrorMessage { get; private set; }

        public float ExportProgress { get; private set; }
        public ModelOperationState ExportState { get; private set; }

        private ModelExportEventArgs(
            string filePath,
            ModelOperationState exportState,
            bool isSuccessful = true,
            string errorMessage = null,
            float exportProgress = 0f,
            GameObject exportedObject = null)
        {
            FilePath = filePath;
            ExportState = exportState;
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
            ExportProgress = exportProgress;
            ExportedObject = exportedObject;
        }

        public static ModelExportEventArgs CreateExportStartEvent(string filePath)
        {
            return new ModelExportEventArgs(filePath, ModelOperationState.Started);
        }

        public static ModelExportEventArgs CreateExportProgressEvent(string filePath, float exportProgress)
        {
            return new ModelExportEventArgs(
                filePath,
                ModelOperationState.InProgress,
                exportProgress: exportProgress);
        }

        public static ModelExportEventArgs CreateExportCompletedEvent(string filePath, GameObject exportedObject)
        {
            return new ModelExportEventArgs(
                filePath,
                ModelOperationState.Completed,
                exportedObject: exportedObject);
        }

        public static ModelExportEventArgs CreateExportFailedEvent(string filePath, string errorMessage)
        {
            return new ModelExportEventArgs(
                filePath,
                ModelOperationState.Failed,
                isSuccessful: false,
                errorMessage: errorMessage);
        }

        public static ModelExportEventArgs CreateExportCancelledEvent(string filePath)
        {
            return new ModelExportEventArgs(
                filePath,
                ModelOperationState.Cancelled,
                isSuccessful: false,
                errorMessage: "Operation cancelled");
        }

        public override string ToString()
        {
            return $"[{ExportState}] Path: {FilePath}, Success: {IsSuccessful}, " +
                   $"Progress: {ExportProgress:P0}" +
                   (ErrorMessage != null ? $", Error: {ErrorMessage}" : "");
        }
    }
}
