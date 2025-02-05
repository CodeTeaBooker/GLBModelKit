using System;
using UnityEngine;

namespace DevToolKit.Models.Events
{
    public class ModelLoadEventArgs : EventArgs
    {
        public string ModelPath { get; }
        public GameObject LoadedModel { get; }

        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }

        public float LoadProgress { get; }
        public ModelOperationState LoadState { get; }

        private ModelLoadEventArgs(
            string modelPath,
            ModelOperationState loadState,
            bool isSuccessful = true,
            string errorMessage = null,
            float loadProgress = 0f,
            GameObject loadedModel = null)
        {
            ModelPath = modelPath;
            LoadState = loadState;
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
            LoadProgress = loadProgress;
            LoadedModel = loadedModel;
        }

        public static ModelLoadEventArgs CreateLoadStartEvent(string modelPath)
            => new ModelLoadEventArgs(modelPath, ModelOperationState.Started);

        public static ModelLoadEventArgs CreateLoadProgressEvent(string modelPath, float loadProgress)
            => new ModelLoadEventArgs(modelPath, ModelOperationState.InProgress, loadProgress: loadProgress);

        public static ModelLoadEventArgs CreateLoadCompletedEvent(string modelPath, GameObject loadedModel)
            => new ModelLoadEventArgs(modelPath, ModelOperationState.Completed, loadedModel: loadedModel);

        public static ModelLoadEventArgs CreateLoadFailedEvent(string modelPath, string errorMessage)
            => new ModelLoadEventArgs(modelPath, ModelOperationState.Failed, false, errorMessage);

        public static ModelLoadEventArgs CreateLoadCancelledEvent(string modelPath)
            => new ModelLoadEventArgs(modelPath, ModelOperationState.Cancelled, false, "Operation cancelled");
    }
}
