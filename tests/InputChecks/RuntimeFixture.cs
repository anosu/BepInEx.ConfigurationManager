namespace UnityEngine
{
    public enum KeyCode { None, A, F1, Mouse0 }
    public struct Vector3
    {
        public float x;
        public float y;
    }
    public static class Input
    {
        public static int ResetCount;
        public static bool GetKey(KeyCode key) => key == KeyCode.A;
        public static bool GetKeyDown(KeyCode key) => key == KeyCode.F1;
        public static bool GetKeyUp(KeyCode key) => key == KeyCode.A;
        public static Vector3 mousePosition => new Vector3 { x = 123, y = 456 };
        public static void ResetInputAxes() => ResetCount++;
    }
}

namespace BepInEx.Unity.IL2CPP
{
    public class BasePlugin { }
}

#if MODERN
namespace BepInEx
{
    public static class UnityInput
    {
        public static IInputSystem Current { get; } = new ExplicitProvider();
    }
    public interface IInputSystem
    {
        bool GetKey(UnityEngine.KeyCode key);
        bool GetKeyDown(UnityEngine.KeyCode key);
        bool GetKeyUp(UnityEngine.KeyCode key);
        UnityEngine.Vector3 mousePosition { get; }
        IEnumerable<UnityEngine.KeyCode> SupportedKeyCodes { get; }
        void ResetInputAxes();
    }
    internal sealed class ExplicitProvider : IInputSystem
    {
        bool IInputSystem.GetKey(UnityEngine.KeyCode key) => key == UnityEngine.KeyCode.F1;
        bool IInputSystem.GetKeyDown(UnityEngine.KeyCode key) => key == UnityEngine.KeyCode.A;
        bool IInputSystem.GetKeyUp(UnityEngine.KeyCode key) => key == UnityEngine.KeyCode.F1;
        UnityEngine.Vector3 IInputSystem.mousePosition => new UnityEngine.Vector3 { x = 789, y = 321 };
        IEnumerable<UnityEngine.KeyCode> IInputSystem.SupportedKeyCodes => new[] { UnityEngine.KeyCode.F1, UnityEngine.KeyCode.A };
        void IInputSystem.ResetInputAxes() => UnityEngine.Input.ResetCount += 10;
    }
}
#endif
