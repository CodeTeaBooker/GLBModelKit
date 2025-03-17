using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    /// <summary>
    /// Base class for all model configuration scriptable objects with strong validation support
    /// </summary>
    public abstract class ModelConfigBase : ScriptableObject
    {
        /// <summary>
        /// Represents a validation error or warning detected during configuration validation
        /// </summary>
        public class ValidationError
        {
            /// <summary>
            /// Name of the property that has the error
            /// </summary>
            public string PropertyName { get; }

            /// <summary>
            /// Error message
            /// </summary>
            public string Message { get; }

            /// <summary>
            /// Severity level of the error
            /// </summary>
            public ValidationSeverity Severity { get; }

            public ValidationError(string propertyName, string message, ValidationSeverity severity = ValidationSeverity.Error)
            {
                PropertyName = propertyName;
                Message = message;
                Severity = severity;
            }

            public override string ToString() => $"[{Severity}] {PropertyName}: {Message}";
        }

        /// <summary>
        /// Severity levels for validation errors
        /// </summary>
        public enum ValidationSeverity
        {
            /// <summary>
            /// Warning - configuration can be used but may have issues
            /// </summary>
            Warning,

            /// <summary>
            /// Error - configuration should not be used as it may cause problems
            /// </summary>
            Error,

            /// <summary>
            /// Critical error - configuration will cause system failure
            /// </summary>
            Critical
        }

        /// <summary>
        /// Validates the configuration and returns any validation errors
        /// </summary>
        /// <param name="errors">List of validation errors found</param>
        /// <returns>True if configuration is valid (no errors or critical errors), false otherwise</returns>
        public bool Validate(out IReadOnlyList<ValidationError> errors)
        {
            var errorList = new List<ValidationError>();

            ValidateBasicProperties(errorList);
            ValidateConfigurationInternal(errorList);
            ValidateDefaultValues(errorList);
            ValidateRelationships(errorList);

            errors = errorList;
            return !errorList.Any(e => e.Severity >= ValidationSeverity.Error);
        }

        /// <summary>
        /// Validates the configuration using the provided validation context
        /// </summary>
        /// <param name="context">Validation context to use</param>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool ValidateWithContext(ValidationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ValidateBasicPropertiesWithContext(context);
            ValidateConfigurationInternalWithContext(context);
            ValidateDefaultValuesWithContext(context);
            ValidateRelationshipsWithContext(context);

            return !context.Errors.Any(e => e.Severity >= ValidationSeverity.Error);
        }

        /// <summary>
        /// Validates basic properties (called first in validation sequence)
        /// </summary>
        protected virtual void ValidateBasicProperties(List<ValidationError> errors)
        {
            // Base implementation is empty, subclasses can override
        }

        /// <summary>
        /// Validates basic properties using context
        /// </summary>
        protected virtual void ValidateBasicPropertiesWithContext(ValidationContext context)
        {
            // Default implementation calls the list-based version
            var errors = new List<ValidationError>();
            ValidateBasicProperties(errors);
            context.AddErrors(errors);
        }

        /// <summary>
        /// Main validation method to be implemented by subclasses
        /// </summary>
        protected abstract void ValidateConfigurationInternal(List<ValidationError> errors);

        /// <summary>
        /// Main validation method using context to be implemented by subclasses
        /// </summary>
        protected virtual void ValidateConfigurationInternalWithContext(ValidationContext context)
        {
            // Default implementation calls the list-based version
            var errors = new List<ValidationError>();
            ValidateConfigurationInternal(errors);
            context.AddErrors(errors);
        }

        /// <summary>
        /// Validates default values (called after main validation)
        /// </summary>
        protected virtual void ValidateDefaultValues(List<ValidationError> errors)
        {
            // Base implementation is empty, subclasses can override
        }

        /// <summary>
        /// Validates default values using context
        /// </summary>
        protected virtual void ValidateDefaultValuesWithContext(ValidationContext context)
        {
            // Default implementation calls the list-based version
            var errors = new List<ValidationError>();
            ValidateDefaultValues(errors);
            context.AddErrors(errors);
        }

        /// <summary>
        /// Validates relationships between properties (called last in validation sequence)
        /// </summary>
        protected virtual void ValidateRelationships(List<ValidationError> errors)
        {
            // Base implementation is empty, subclasses can override
        }

        /// <summary>
        /// Validates relationships between properties using context
        /// </summary>
        protected virtual void ValidateRelationshipsWithContext(ValidationContext context)
        {
            // Default implementation calls the list-based version
            var errors = new List<ValidationError>();
            ValidateRelationships(errors);
            context.AddErrors(errors);
        }

        #region Validation Helper Methods

        /// <summary>
        /// Validates that a value is within a specified range
        /// </summary>
        protected void ValidateRange<T>(List<ValidationError> errors, T value, T min, T max, string propertyName)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                errors.Add(new ValidationError(propertyName,
                    $"Must be between {min} and {max}", ValidationSeverity.Error));
            }
        }

        /// <summary>
        /// Validates that a string is not null or empty
        /// </summary>
        protected void ValidateRequired(List<ValidationError> errors, string value, string propertyName)
        {
            if (string.IsNullOrEmpty(value))
            {
                errors.Add(new ValidationError(propertyName,
                    "Cannot be empty", ValidationSeverity.Error));
            }
        }

        /// <summary>
        /// Validates that a directory exists
        /// </summary>
        protected void ValidateDirectoryExists(List<ValidationError> errors, string path, string propertyName,
            ValidationSeverity severity = ValidationSeverity.Warning)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                errors.Add(new ValidationError(propertyName,
                    $"Directory does not exist: {path}", severity));
            }
        }

        /// <summary>
        /// Validates that a file exists
        /// </summary>
        protected void ValidateFileExists(List<ValidationError> errors, string path, string propertyName,
            ValidationSeverity severity = ValidationSeverity.Warning)
        {
            if (!string.IsNullOrEmpty(path) && !File.Exists(path))
            {
                errors.Add(new ValidationError(propertyName,
                    $"File does not exist: {path}", severity));
            }
        }

        /// <summary>
        /// Validates that an enum value is defined
        /// </summary>
        protected void ValidateEnum<T>(List<ValidationError> errors, T value, string propertyName)
            where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                errors.Add(new ValidationError(propertyName,
                    $"Invalid enum value: {value}", ValidationSeverity.Error));
            }
        }

        /// <summary>
        /// Validates that a reference is not null
        /// </summary>
        protected void ValidateNotNull(List<ValidationError> errors, UnityEngine.Object value, string propertyName,
            ValidationSeverity severity = ValidationSeverity.Error)
        {
            if (value == null)
            {
                errors.Add(new ValidationError(propertyName,
                    "Reference cannot be null", severity));
            }
        }

        /// <summary>
        /// Validates a condition with a custom message
        /// </summary>
        protected void ValidateCondition(List<ValidationError> errors, bool condition, string propertyName,
            string message, ValidationSeverity severity = ValidationSeverity.Error)
        {
            if (!condition)
            {
                errors.Add(new ValidationError(propertyName, message, severity));
            }
        }

        #endregion
    }

    /// <summary>
    /// Context for validation that enables hierarchical validation and state sharing
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// The object being validated
        /// </summary>
        public UnityEngine.Object Owner { get; }

        /// <summary>
        /// Validation errors collected during validation
        /// </summary>
        public List<ModelConfigBase.ValidationError> Errors { get; }

        /// <summary>
        /// Shared properties for validation
        /// </summary>
        public Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Parent context (if this is a child context)
        /// </summary>
        public ValidationContext Parent { get; }

        /// <summary>
        /// Current property path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a new validation context
        /// </summary>
        public ValidationContext(UnityEngine.Object owner, ValidationContext parent = null, string path = null)
        {
            Owner = owner;
            Parent = parent;
            Path = path ?? string.Empty;
            Errors = new List<ModelConfigBase.ValidationError>();
            Properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Adds an error to the context
        /// </summary>
        public void AddError(string propertyName, string message,
            ModelConfigBase.ValidationSeverity severity = ModelConfigBase.ValidationSeverity.Error)
        {
            string fullPath = string.IsNullOrEmpty(Path)
                ? propertyName
                : $"{Path}.{propertyName}";

            Errors.Add(new ModelConfigBase.ValidationError(fullPath, message, severity));
        }

        /// <summary>
        /// Adds multiple errors to the context
        /// </summary>
        public void AddErrors(IEnumerable<ModelConfigBase.ValidationError> errors)
        {
            foreach (var error in errors)
            {
                string fullPath = string.IsNullOrEmpty(Path)
                    ? error.PropertyName
                    : $"{Path}.{error.PropertyName}";

                Errors.Add(new ModelConfigBase.ValidationError(fullPath, error.Message, error.Severity));
            }
        }

        /// <summary>
        /// Creates a child validation context
        /// </summary>
        public ValidationContext CreateChildContext(UnityEngine.Object childOwner, string childPath)
        {
            string fullPath = string.IsNullOrEmpty(Path)
                ? childPath
                : $"{Path}.{childPath}";

            return new ValidationContext(childOwner, this, fullPath);
        }

        /// <summary>
        /// Merges errors from a child context
        /// </summary>
        public void MergeChildContext(ValidationContext childContext)
        {
            if (childContext == null) return;

            Errors.AddRange(childContext.Errors);

            // Merge selected properties if needed
            foreach (var prop in childContext.Properties)
            {
                if (!Properties.ContainsKey(prop.Key))
                {
                    Properties[prop.Key] = prop.Value;
                }
            }
        }

        /// <summary>
        /// Sets a property in the context
        /// </summary>
        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }

        /// <summary>
        /// Gets a property from the context
        /// </summary>
        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            if (Parent != null)
            {
                return Parent.GetProperty(key, defaultValue);
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if a property exists in the context
        /// </summary>
        public bool HasProperty(string key)
        {
            return Properties.ContainsKey(key) || (Parent != null && Parent.HasProperty(key));
        }
    }
}