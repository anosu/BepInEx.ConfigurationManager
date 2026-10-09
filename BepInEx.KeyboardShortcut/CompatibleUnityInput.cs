using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace BepInEx.Configuration
{
    /// <summary>Input access for IL2CPP runtimes with or without BepInEx.UnityInput.</summary>
    public sealed class CompatibleUnityInput
    {
        /// <summary>The input backend selected for this runtime.</summary>
        public static readonly CompatibleUnityInput Current = new CompatibleUnityInput();

        private readonly Func<KeyCode, bool> _getKey;
        private readonly Func<KeyCode, bool> _getKeyDown;
        private readonly Func<KeyCode, bool> _getKeyUp;
        private readonly Func<Vector3> _mousePosition;
        private readonly Action _resetInputAxes;

        /// <summary>Key codes supported by the selected input backend.</summary>
        public IEnumerable<KeyCode> SupportedKeyCodes { get; }

        private CompatibleUnityInput()
        {
            // Resolve the optional type by name: typeof(UnityInput) would break old runtimes.
            var type = typeof(BasePlugin).Assembly.GetType("BepInEx.UnityInput", false);
            var property = type?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
            var provider = property?.GetValue(null, null);
            if (provider != null)
            {
                // Use the declared interface, including providers with explicit implementations.
                var api = property.PropertyType;
                _getKey = Bind<Func<KeyCode, bool>>(api.GetMethod("GetKey", new[] { typeof(KeyCode) }), provider);
                _getKeyDown = Bind<Func<KeyCode, bool>>(api.GetMethod("GetKeyDown", new[] { typeof(KeyCode) }), provider);
                _getKeyUp = Bind<Func<KeyCode, bool>>(api.GetMethod("GetKeyUp", new[] { typeof(KeyCode) }), provider);
                _mousePosition = Bind<Func<Vector3>>(api.GetProperty("mousePosition").GetGetMethod(), provider);
                _resetInputAxes = Bind<Action>(api.GetMethod("ResetInputAxes", Type.EmptyTypes), provider);
                SupportedKeyCodes = (IEnumerable<KeyCode>)api.GetProperty("SupportedKeyCodes").GetValue(provider, null);
            }
            else
            {
                _getKey = Input.GetKey;
                _getKeyDown = Input.GetKeyDown;
                _getKeyUp = Input.GetKeyUp;
                _mousePosition = () => Input.mousePosition;
                _resetInputAxes = Input.ResetInputAxes;
                SupportedKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
            }
        }

        private static T Bind<T>(MethodInfo method, object provider) where T : class
        {
            return (T)(object)Delegate.CreateDelegate(typeof(T), provider, method);
        }

        /// <summary>Whether a key is currently held.</summary>
        public bool GetKey(KeyCode key) => _getKey(key);
        /// <summary>Whether a key was pressed this frame.</summary>
        public bool GetKeyDown(KeyCode key) => _getKeyDown(key);
        /// <summary>Whether a key was released this frame.</summary>
        public bool GetKeyUp(KeyCode key) => _getKeyUp(key);
        /// <summary>Current mouse position in screen coordinates.</summary>
        public Vector3 mousePosition => _mousePosition();
        /// <summary>Clear input axes after the manager consumes input.</summary>
        public void ResetInputAxes() => _resetInputAxes();
    }
}
