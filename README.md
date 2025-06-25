# GLBModelKit

A Unity toolkit for managing GLB/GLTF 3D models with caching, asynchronous loading, and export capabilities.

## Overview

GLBModelKit provides a comprehensive solution for handling GLB and GLTF 3D models in Unity applications. The toolkit includes model caching, instance management, asynchronous loading with progress tracking, and export functionality.

## Features

### Core Functionality
- **Model Loading**: Asynchronous loading with progress tracking and cancellation support
- **Caching System**: Thread-safe model caching with configurable size and memory limits
- **Instance Management**: Automatic tracking and lifecycle management of model instances
- **Export Support**: Export Unity GameObjects to GLB/GLTF format
- **Retry Mechanism**: Configurable retry strategies for failed operations

### Technical Features
- **Event System**: Comprehensive event notifications for all operations
- **Configuration Management**: ScriptableObject-based configuration with validation
- **Editor Integration**: Built-in Unity Editor tools for testing and debugging
- **Memory Management**: Automatic cleanup and resource disposal
- **Thread Safety**: Concurrent access protection across all components

## Requirements

- Unity 2022.3.60f1 or later

## Installation

1. Clone or download this repository
2. Open the GLBModelKit project in Unity
3. All required dependencies (including UnityGLTF) are already included in the project
4. The toolkit is ready to use immediately

## Quick Start

### Basic Setup

1. Open the sample scene: `Assets/Scenes/ModelLoadAndExportExampleScene.unity`
2. The scene includes a pre-configured `GLBModelManager` GameObject
3. Configuration assets are located in the `Assets/ModelManager/` folder:
   - `GLBModelManagerConfig.asset` - Main manager settings
   - `GLBModelImportConfig.asset` - Import parameters
   - `GLBModelExporterConfig.asset` - Export settings

### Loading Models

```csharp
using DevToolKit.Models.Managers;

public class ModelLoader : MonoBehaviour
{
    [SerializeField] private GLBModelManager modelManager;
    
    async void Start()
    {
        // Load a model asynchronously
        GameObject model = await modelManager.CreateModelAsync("Models/example.glb");
        
        if (model != null)
        {
            // Model loaded successfully
            Debug.Log("Model loaded: " + model.name);
        }
    }
}
```

### Managing Model Instances

```csharp
// Create additional instances of cached models
GameObject instance = modelManager.CreateInstance("Models/example.glb", parentTransform);

// Remove specific instances
bool removed = modelManager.RemoveModel(instance);

// Remove all instances of a model
await modelManager.RemoveModelGroupAsync("Models/example.glb");

// Clear entire cache
await modelManager.ClearModelLibraryAsync();
```

### Exporting Models

```csharp
// Export GameObjects to GLB format
Transform[] objectsToExport = { transform };
bool success = modelManager.ExportModel("OutputPath", "filename", objectsToExport);

// Export to byte array
byte[] data = modelManager.ExportModelToStream("filename", objectsToExport);
```

### Event Handling

```csharp
void OnEnable()
{
    modelManager.LoadStateChanged += OnLoadStateChanged;
    modelManager.CacheStateChanged += OnCacheStateChanged;
    modelManager.ExportStateChanged += OnExportStateChanged;
}

void OnLoadStateChanged(object sender, ModelLoadEventArgs e)
{
    Debug.Log($"Load state: {e.LoadState}, Progress: {e.LoadProgress:P0}");
}
```

## Testing in Editor

The GLBModelManager includes built-in editor controls for testing:

1. Select the GLBModelManager GameObject in the scene
2. In the Inspector, find the "Debug Controls" section
3. Use the provided buttons to test operations:
   - **Create Model**: Load the model specified in the configuration
   - **Delete Selected Model**: Remove a specific model instance
   - **Delete Model Group**: Remove all instances of a model type
   - **Clear Model Library**: Clear the entire cache
   - **Export Model**: Export the current root transform

## Architecture

### Core Components

- **GLBModelManager**: Main entry point for all model operations
- **ModelCache**: Thread-safe caching system with size and memory limits
- **ModelLoader Pipeline**: Layered loading system with retry and caching capabilities
- **ModelExporter**: GLB/GLTF export functionality
- **Configuration System**: Validation-enabled ScriptableObject configurations
- **Event System**: Type-safe event notifications

### Configuration

The toolkit uses ScriptableObject-based configuration with built-in validation:

- **GLBModelManagerConfigSO**: Cache settings, retry configuration, and auto-import options
- **GLBModelImportConfigSO**: Import parameters, collider settings, and quality options
- **GLBModelExporterConfigSO**: Export format, texture quality, and output settings

Each configuration includes real-time validation with error reporting and warnings in the Unity Inspector.

## File Structure

```
Assets/ModelManager/
├── Cache/              # Caching system implementation
├── Config/             # Configuration ScriptableObjects and editor tools
├── Core/               # Base classes, interfaces, and retry strategies
├── Events/             # Event system and type-safe event arguments
├── Exporters/          # Model export functionality
├── Loaders/            # Asynchronous loading pipeline
├── Managers/           # Main GLBModelManager component
└── Utils/              # Utility classes and helpers

Assets/Scenes/          # Example scene with pre-configured setup
Assets/Resources/       # UnityGLTF settings and shader variants
```

## Performance Considerations

- Models are cached to reduce loading times for repeated access
- Asynchronous operations prevent UI blocking during model loading
- Automatic memory management prevents resource leaks
- Configurable cache limits help control memory usage
- Thread-safe design enables concurrent operations
- Event batching reduces performance overhead

## Dependencies

- **UnityGLTF** - Included in the project for GLB/GLTF file handling
- **Unity TextMeshPro** - For UI text rendering
- **Unity Timeline** - For animation support

## Contributing

This is a reference implementation demonstrating best practices for Unity model management systems. For production use, consider the specific requirements of your application and modify accordingly.


## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.