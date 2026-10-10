using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var path = args.Length > 0 ? args[0] : "bin/IL2CPP/ConfigurationManager.dll";
var paths = new[] { path, Path.Combine(Path.GetDirectoryName(path)!, "BepInEx.KeyboardShortcut.dll") };
foreach (var assemblyPath in paths.Where(File.Exists))
{
    using var stream = File.OpenRead(assemblyPath);
    using var pe = new PEReader(stream);
    var metadata = pe.GetMetadataReader();
    foreach (var handle in metadata.TypeReferences)
    {
        var type = metadata.GetTypeReference(handle);
        var ns = metadata.GetString(type.Namespace);
        var name = metadata.GetString(type.Name);
        if (assemblyPath == path && ns == "UnityEngine" && new[] { "GUILayout", "GUILayoutUtility", "GUILayoutOption", "GUILayoutGroup", "GUIScrollGroup" }.Contains(name))
            throw new Exception("Native layout regression: IL2CPP manager must own its geometry and avoid UnityEngine." + name);
        var referencesIl2Cpp = type.ResolutionScope.Kind == HandleKind.AssemblyReference &&
            metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name) == "BepInEx.Unity.IL2CPP";
        if (referencesIl2Cpp && (ns == "BepInEx.Unity.IL2CPP.Configuration" && name == "KeyboardShortcut" ||
                                ns == "BepInEx" && name == "UnityInput"))
            throw new Exception($"TypeLoadException regression: {assemblyPath} still requires optional type {ns}.{name}.");
    }
    var isIl2Cpp = metadata.AssemblyReferences.Any(handle =>
        metadata.GetString(metadata.GetAssemblyReference(handle).Name) == "BepInEx.Unity.IL2CPP");
    if (isIl2Cpp)
    {
        foreach (var handle in metadata.MemberReferences)
        {
            var member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference)
                continue;
            var owner = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            if (metadata.GetString(owner.Namespace) == "UnityEngine" && metadata.GetString(owner.Name) == "GUI" &&
                metadata.GetString(member.Name) == "DrawTexture")
                throw new Exception("Unstripping regression: IL2CPP texture painting must avoid restored GUI.DrawTexture overloads.");
            if (metadata.GetString(owner.Namespace) == "UnityEngine" && metadata.GetString(owner.Name) == "Font" &&
                metadata.GetString(member.Name) == ".ctor")
            {
                var signature = metadata.GetBlobReader(member.Signature);
                signature.ReadSignatureHeader();
                if (signature.ReadCompressedInteger() == 0)
                    throw new Exception("MissingMethod regression: IL2CPP font preparation must not require Font's parameterless constructor.");
            }
            if (metadata.GetString(owner.Namespace) == "UnityEngine" &&
                metadata.GetString(owner.Name) == "GUI" && metadata.GetString(member.Name) == "Window")
                throw new Exception("Modal regression: IL2CPP must use GUI.ModalWindow rather than a modeless GUI.Window.");
            if (metadata.GetString(owner.Namespace) == "UnityEngine" &&
                metadata.GetString(owner.Name) == "GUILayout" && metadata.GetString(member.Name) == "Window")
                throw new Exception("MissingMethodException regression: IL2CPP must not call GUILayout.Window, whose LayoutedWindow constructor may be stripped.");
            if (metadata.GetString(owner.Namespace) == "UnityEngine" && metadata.GetString(owner.Name) == "GUILayout" &&
                new[] { "Width", "Height", "MinWidth", "MaxWidth", "MinHeight", "MaxHeight", "ExpandWidth", "ExpandHeight" }.Contains(metadata.GetString(member.Name)))
                throw new Exception("AccessViolation regression: IL2CPP must use native-boxed layout options instead of restored GUILayout option factories.");
        }
    }
}
Console.WriteLine("PASS: plugin and shortcut DLLs have no hard dependency on optional IL2CPP input/shortcut types.");
Console.WriteLine("PASS: IL2CPP window drawing avoids the stripped GUILayout.LayoutedWindow helper.");
Console.WriteLine("PASS: IL2CPP drawing has no native GUILayout groups, rect allocation or layout options.");
Console.WriteLine("PASS: IL2CPP texture/font preparation avoids stripped DrawTexture and parameterless Font constructor references.");

var translations = (Dictionary<string, string>)typeof(ConfigurationManager.Localization)
    .GetField("Chinese", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
foreach (var pair in translations)
{
    ConfigurationManager.Localization.Language = ConfigurationManager.Localization.English;
    if (ConfigurationManager.Localization.Text(pair.Key) != pair.Key)
        throw new Exception("English text changed: " + pair.Key);
    ConfigurationManager.Localization.Language = ConfigurationManager.Localization.SimplifiedChinese;
    if (string.IsNullOrWhiteSpace(pair.Value) || ConfigurationManager.Localization.Text(pair.Key) != pair.Value)
        throw new Exception("Missing Chinese translation: " + pair.Key);
}
if (ConfigurationManager.Localization.Text("Third-party plugin description") != "Third-party plugin description")
    throw new Exception("Unknown plugin text must remain unchanged.");
Console.WriteLine($"PASS: {translations.Count} English/Chinese translations and fallback.");

var version = System.Xml.Linq.XDocument.Load("Directory.Build.props").Descendants("Version").Single().Value;
foreach (var flavor in new[] { "BepInEx5", "IL2CPP" })
{
    using var archive = System.IO.Compression.ZipFile.OpenRead($"bin/BepInEx.ConfigurationManager_{flavor}_v{version}.zip");
    var shortcut = archive.Entries.Any(entry => entry.Name == "BepInEx.KeyboardShortcut.dll");
    if (shortcut != (flavor == "IL2CPP"))
        throw new Exception("Incorrect compatibility assembly packaging for " + flavor);
    if (!archive.Entries.Any(entry => entry.Name == "ConfigurationManager.dll"))
        throw new Exception("Missing plugin DLL for " + flavor);
}
Console.WriteLine("PASS: Mono and IL2CPP release packages contain the correct assemblies.");
