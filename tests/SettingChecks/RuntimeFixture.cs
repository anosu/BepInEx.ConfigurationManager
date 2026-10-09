using BepInEx.Configuration;
using UnityEngine;

namespace BepInEx
{
    public class BepInPlugin
    {
        public string GUID, Name, Version;
        public BepInPlugin(string guid, string name, string version) { GUID = guid; Name = name; Version = version; }
    }
    public class PluginInfo { public object Instance; public BepInPlugin Metadata; public string Location; }
    public static class Paths { public static Version BepInExVersion = new Version(6, 0); }
}
namespace BepInEx.Logging { public enum LogLevel { Message = 1, Warning = 2 } }
namespace BepInEx.Unity.IL2CPP
{
    public class BasePlugin { public ConfigFile Config = new ConfigFile(); }
    public class IL2CPPChainloader
    {
        public static IL2CPPChainloader Instance = new IL2CPPChainloader();
        public Dictionary<string, BepInEx.PluginInfo> Plugins = new Dictionary<string, BepInEx.PluginInfo>();
    }
}
namespace BepInEx.Configuration
{
    public class ConfigDefinition { public string Key, Section; }
    public class ConfigDescription { public string Description; public object[] Tags; public AcceptableValueBase AcceptableValues; }
    public class ConfigEntryBase
    {
        public ConfigDefinition Definition = new ConfigDefinition();
        public ConfigDescription Description = new ConfigDescription();
        public Type SettingType; public object DefaultValue, BoxedValue;
    }
    public abstract class AcceptableValueBase { }
    public class AcceptableValueList<T> : AcceptableValueBase { public T[] AcceptableValues { get; set; } }
    public class AcceptableValueRange<T> : AcceptableValueBase { public T MinValue { get; set; } public T MaxValue { get; set; } }
    public class ConfigFile : Dictionary<ConfigDefinition, ConfigEntryBase> { public static ConfigFile CoreConfig = new ConfigFile(); }
    public static class TomlTypeConverter { public static TypeConverter GetConverter(Type type) => null; }
    public class TypeConverter { public Func<object, Type, string> ConvertToString; public Func<string, Type, object> ConvertToObject; }
    public struct KeyboardShortcut { }
    public class CompatibleUnityInput
    {
        public static CompatibleUnityInput Current = new CompatibleUnityInput();
        public IEnumerable<KeyCode> SupportedKeyCodes => Enum.GetValues<KeyCode>();
        public bool GetKey(KeyCode key) => false;
        public bool GetKeyUp(KeyCode key) => false;
    }
}
namespace UnityEngine
{
    public struct Vector3 { public float x, y, z; }
    public struct Vector4 { public float x, y, z, w; }
    public struct Quaternion { public float x, y, z, w; }
    public class MonoBehaviour : Object { public bool enabled { get; set; } }
    public static class ColorUtility { public static bool TryParseHtmlString(string text, out Color value) { value = default; return false; } }
    public static class Application { public static string dataPath = "", persistentDataPath = ""; }
}
namespace ConfigurationManager
{
    internal class ConfigurationManager
    {
        public const string GUID = "com.bepis.bepinex.configurationmanager";
        internal int RightColumnWidth => 300;
        internal Rect SettingWindowRect => new Rect(0, 0, 700, 450);
        internal static FixtureLog Logger = new FixtureLog();
        internal class FixtureLog
        {
            internal readonly List<string> Errors = new List<string>();
            internal void LogError(object value) => Errors.Add(value.ToString());
            internal void LogWarning(object value) { }
            internal void LogInfo(object value) { }
            internal void Log(BepInEx.Logging.LogLevel level, object value) { }
        }
    }
}
