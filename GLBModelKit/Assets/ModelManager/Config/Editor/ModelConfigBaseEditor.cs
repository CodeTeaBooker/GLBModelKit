#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using DevToolKit.Models.Core;

namespace DevToolKit.Models.Editor
{
    /// <summary>
    /// Custom editor for ModelConfigBase and derived classes that provides rich validation feedback
    /// </summary>
    [CustomEditor(typeof(ModelConfigBase), true)]
    public class ModelConfigBaseEditor : UnityEditor.Editor
    {
        // Validation results cache
        private List<ModelConfigBase.ValidationError> _lastValidationErrors;
        private bool _hasValidation = false;

        // UI state
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _autoValidate = true;
        private bool _showValidationPanel = true;

        // Styling
        private GUIStyle _headerStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _successStyle;

        /// <summary>
        /// Initialize styles
        /// </summary>
        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel);
                _headerStyle.fontSize = 12;
                _headerStyle.margin = new RectOffset(0, 0, 10, 10);
            }

            if (_errorStyle == null)
            {
                _errorStyle = new GUIStyle(EditorStyles.helpBox);
                _errorStyle.normal.textColor = new Color(0.9f, 0.2f, 0.2f);
                _errorStyle.fontSize = 11;
            }

            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(EditorStyles.helpBox);
                _warningStyle.normal.textColor = new Color(0.9f, 0.7f, 0.1f);
                _warningStyle.fontSize = 11;
            }

            if (_successStyle == null)
            {
                _successStyle = new GUIStyle(EditorStyles.helpBox);
                _successStyle.normal.textColor = new Color(0.1f, 0.7f, 0.1f);
                _successStyle.fontSize = 11;
            }
        }

        public override void OnInspectorGUI()
        {
            InitializeStyles();

            // Update serialized object
            serializedObject.Update();

            // Draw default inspector
            DrawDefaultInspector();

            // Add space before validation panel
            EditorGUILayout.Space();

            // Get target config
            var config = (ModelConfigBase)target;

            // Draw validation panel
            DrawValidationPanel(config);

            // Apply changes
            serializedObject.ApplyModifiedProperties();

            // Auto validate if needed
            if (_autoValidate && GUI.changed)
            {
                ValidateConfig(config);
                Repaint();
            }
        }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        private void ValidateConfig(ModelConfigBase config)
        {
            // Execute validation
            config.Validate(out IReadOnlyList<ModelConfigBase.ValidationError> errors);
            _lastValidationErrors = errors.ToList();
            _hasValidation = true;
        }

        /// <summary>
        /// Draws the validation panel
        /// </summary>
        private void DrawValidationPanel(ModelConfigBase config)
        {
            // Validation panel foldout
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showValidationPanel = EditorGUILayout.Foldout(_showValidationPanel, "Validation", true, EditorStyles.foldoutHeader);

            // Auto-validate toggle
            GUI.enabled = true;
            bool newAutoValidate = EditorGUILayout.ToggleLeft("Auto", _autoValidate, GUILayout.Width(60));
            if (newAutoValidate != _autoValidate)
            {
                _autoValidate = newAutoValidate;
                if (_autoValidate)
                {
                    ValidateConfig(config);
                }
            }

            // Validate button
            if (GUILayout.Button("Validate Now", GUILayout.Width(100)))
            {
                ValidateConfig(config);
            }

            EditorGUILayout.EndHorizontal();

            // Only show validation results if panel is expanded
            if (_showValidationPanel)
            {
                DrawValidationResults();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the validation results
        /// </summary>
        private void DrawValidationResults()
        {
            if (!_hasValidation)
            {
                EditorGUILayout.HelpBox("Click 'Validate Now' to validate the configuration.", MessageType.Info);
                return;
            }

            if (_lastValidationErrors == null || _lastValidationErrors.Count == 0)
            {
                // No errors - show success message
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("✓ Configuration is valid!", _successStyle);
                EditorGUILayout.EndVertical();
                return;
            }

            // Get errors and warnings
            var errors = _lastValidationErrors
                .Where(e => e.Severity >= ModelConfigBase.ValidationSeverity.Error)
                .ToList();

            var warnings = _lastValidationErrors
                .Where(e => e.Severity == ModelConfigBase.ValidationSeverity.Warning)
                .ToList();

            // Draw errors section
            if (errors.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                _showErrors = EditorGUILayout.Foldout(_showErrors, $"Errors ({errors.Count})", true);
                EditorGUILayout.EndHorizontal();

                if (_showErrors)
                {
                    foreach (var error in errors)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"❌ {error.PropertyName}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(error.Message, _errorStyle);
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            // Draw warnings section
            if (warnings.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                _showWarnings = EditorGUILayout.Foldout(_showWarnings, $"Warnings ({warnings.Count})", true);
                EditorGUILayout.EndHorizontal();

                if (_showWarnings)
                {
                    foreach (var warning in warnings)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"⚠ {warning.PropertyName}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(warning.Message, _warningStyle);
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }
    }
}
#endif