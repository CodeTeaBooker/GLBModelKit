using System;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    public abstract class ModelConfigBase : ScriptableObject
    {
        public bool Validate(out string error)
        {
            var errors = ValidateConfiguration();
            if (errors != null && errors.Length > 0)
            {
                error = string.Join(", ", errors);
                return false;
            }
            error = null;
            return true;
        }

        protected abstract string[] ValidateConfiguration();

        protected static string ValidateRange<T>(T value, T min, T max, string fieldName) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                return $"{fieldName} must be between {min} and {max}";
            return string.Empty;
        }

        protected static string ValidateRequired(string value, string fieldName)
        {
            return string.IsNullOrEmpty(value) ? $"{fieldName} cannot be empty" : string.Empty;
        }
    }
}
