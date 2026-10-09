using System.Reflection;

namespace HarmonyLib
{
    // Models Harmony's prefix contract; no native detours or Unity runtime are executed.
    public sealed class HarmonyMethod
    {
        public readonly MethodInfo Method;
        public HarmonyMethod(MethodInfo method) { Method = method; }
    }
    public sealed class Harmony
    {
        public static readonly Dictionary<MethodBase, List<MethodInfo>> Prefixes = new();
        public static int PatchCount;
        public Harmony(string id) { }
        public void Patch(MethodBase original, HarmonyMethod prefix)
        {
            if (!Prefixes.TryGetValue(original, out var list)) Prefixes[original] = list = new();
            list.Add(prefix.Method); PatchCount++;
        }
        public static void Invoke(MethodInfo original, object instance, params object[] args)
        {
            if (Prefixes.TryGetValue(original, out var list) && list.Any(prefix => !(bool)prefix.Invoke(null, null)!)) return;
            original.Invoke(instance, args);
        }
    }
}
namespace Project
{
    public sealed class InputService
    {
        public int DispatchCount;
        public void OnUpdate(float dt)
        {
            // Deliberately matches the removed adapter's target. The general-purpose manager
            // must leave this game's callback untouched and preserve its raycast dependency.
            if (UnityEngine.EventSystems.EventSystem.current == null) throw new NullReferenceException("Touch query requires EventSystem.current");
            DispatchCount++;
        }
    }
}
