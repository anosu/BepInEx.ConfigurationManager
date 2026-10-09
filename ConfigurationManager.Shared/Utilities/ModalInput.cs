#if IL2CPP
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine.EventSystems;

namespace ConfigurationManager.Utilities
{
    internal static class ModalInput
    {
        private static readonly HashSet<MethodBase> Patched = new HashSet<MethodBase>();
        private static Harmony _harmony;
        private static bool _active;

        internal static void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;
            if (!active) return;

            // Keep EventSystem.current and its input module alive for game-side raycasts.
            // Suppress dispatch instead of disabling the component and removing it from current.
            try
            {
                if (_harmony == null) _harmony = new Harmony("com.bepis.bepinex.configurationmanager.modal-input");
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                TryPatch(typeof(EventSystem).GetMethod("Update", flags), "EventSystem.Update");

            }
            catch (Exception ex)
            {
                ConfigurationManager.Logger.LogWarning("Failed to initialize modal input hooks: " + ex);
            }
        }

        private static void TryPatch(MethodInfo method, string name)
        {
            if (method != null && Patched.Contains(method)) return;
            try
            {
                if (method == null) throw new MissingMethodException(name);
                var prefix = typeof(ModalInput).GetMethod(nameof(AllowGameInput), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                Patched.Add(method);
                ConfigurationManager.Logger.LogInfo("Modal input dispatch guard installed: " + name);
            }
            catch (Exception ex) { ConfigurationManager.Logger.LogWarning("Failed to guard " + name + ": " + ex); }
        }

        internal static bool AllowGameInput() => !_active;
    }
}
#endif
