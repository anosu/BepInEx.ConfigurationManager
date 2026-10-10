using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Reflection.Emit;

if (args.Length == 1 && args[0] == "--self-test")
{
    var roots = GetBodies(typeof(ConstructorOnlyFailure)).Concat(GetBodies(typeof(InitializerOnlyFailure)));
    var detected = ScanUnavailable(roots, new[] { typeof(ConstructorOnlyFailure).Assembly }, out _);
    if (!detected.Contains(typeof(ConstructorOnlyFailure).FullName + "..ctor") ||
        !detected.Contains(typeof(InitializerOnlyFailure).FullName + "..cctor"))
        throw new Exception("Constructor-only or static-initializer-only failure stub escaped startup scanning.");
    Console.WriteLine("PASS: constructor-only and static-initializer-only failure stubs are detected.");
    return;
}

var interop = Path.GetFullPath(args[0]);
var core = Path.GetFullPath(Path.Combine(interop, "../core"));
var pluginDirectory = Path.GetFullPath("bin/IL2CPP");
var context = new AssemblyLoadContext("GameInterop", true);
context.Resolving += (loader, name) =>
{
    foreach (var directory in new[] { interop, core, pluginDirectory })
    {
        var file = Path.Combine(directory, name.Name + ".dll");
        if (File.Exists(file))
            return loader.LoadFromAssemblyPath(file);
    }
    return null;
};
var imgui = context.LoadFromAssemblyPath(Path.Combine(interop, "UnityEngine.IMGUIModule.dll"));
var api = args.Length > 1 ? args[1] : "UnityEngine.GUI";
var windowMethod = imgui.GetType(api, true)!.GetMethods(BindingFlags.Static | BindingFlags.Public)
    .Single(m => api == "UnityEngine.GUI" ? m.Name == "ModalWindow" && m.GetParameters().Length == 4 &&
        m.GetParameters()[3].ParameterType == typeof(string) : m.Name == "DoWindow");
RuntimeHelpers.PrepareMethod(windowMethod.MethodHandle);
Console.WriteLine("PASS: " + api + "." + windowMethod.Name + " JIT resolves against the game's actual interop assemblies.");
const BindingFlags inputFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
var ui = context.LoadFromAssemblyPath(Path.Combine(interop, "UnityEngine.UI.dll"));
var eventUpdate = ui.GetType("UnityEngine.EventSystems.EventSystem", true)!.GetMethod("Update", inputFlags)
    ?? throw new MissingMethodException("EventSystem.Update modal hook target is absent.");
var inputTargets = new List<MethodBase> { eventUpdate };
foreach (var target in inputTargets) RuntimeHelpers.PrepareMethod(target.MethodHandle);
Console.WriteLine($"PASS: {inputTargets.Count} modal input hook targets JIT resolve against the game's actual assemblies.");
// Reflection hides this factory from the ordinary reachable-call scan; validate its target explicitly.
var fontType = context.LoadFromAssemblyPath(Path.Combine(interop, "UnityEngine.TextRenderingModule.dll")).GetType("UnityEngine.Font", true)!;
var fontFactory = fontType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
    .Single(m => m.Name == "Internal_CreateDynamicFont" && m.GetParameters().Length == 3);
RuntimeHelpers.PrepareMethod(fontFactory.MethodHandle);
Console.WriteLine("PASS: reflected Font.Internal_CreateDynamicFont target JIT resolves against the game's actual assemblies.");
var plugin = context.LoadFromAssemblyPath(Path.Combine(pluginDirectory, "ConfigurationManager.dll"));
var assemblies = new[] { plugin, context.LoadFromAssemblyPath(Path.Combine(pluginDirectory, "BepInEx.KeyboardShortcut.dll")) };
var count = 0;
var constructors = 0;
var initializers = 0;
var jitFailures = new List<string>();
var pending = new Queue<MethodBase>(inputTargets);
pending.Enqueue(fontFactory);
foreach (var assembly in assemblies)
{
    foreach (var type in assembly.GetTypes().Where(type => !type.ContainsGenericParameters))
    {
        foreach (var method in GetBodies(type))
        {
            if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() == null)
                continue;
            try { RuntimeHelpers.PrepareMethod(method.MethodHandle); }
            catch (Exception ex) { jitFailures.Add(type.FullName + "." + method.Name + ": " + ex.Message); }
            count++;
            if (method is ConstructorInfo) { if (method.IsStatic) initializers++; else constructors++; }
            pending.Enqueue(method);
        }
    }
}
if (constructors == 0 || initializers == 0) throw new Exception("Startup constructors/initializers were not checked.");
if (jitFailures.Count == 0)
    Console.WriteLine($"PASS: {count} plugin/shortcut bodies, including {constructors} constructors and {initializers} static initializers, JIT resolve against the game's actual assemblies.");

var failures = ScanUnavailable(pending, assemblies, out var reachable);
if (failures.Count > 0 || jitFailures.Count > 0)
    throw new Exception("JIT compatibility failures:\n" + string.Join("\n", jitFailures) +
        "\nReachable unavailable IMGUI methods:\n" + string.Join("\n", failures.Order()));
Console.WriteLine($"PASS: {reachable} reachable methods contain no unstripping-failure stubs, missing references or invalid GUILayout CLR boxing.");

IEnumerable<MethodBase> GetBodies(Type type)
{
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                               BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    var bodies = type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags));
    if (type.TypeInitializer != null) bodies = bodies.Append(type.TypeInitializer);
    return bodies.Distinct();
}

HashSet<string> ScanUnavailable(IEnumerable<MethodBase> roots, Assembly[] allowedAssemblies, out int visitedCount)
{
    var work = new Queue<MethodBase>(roots);
    var opcodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!).ToDictionary(opcode => (ushort)opcode.Value);
    var visited = new HashSet<MethodBase>();
    var errors = new HashSet<string>();
    while (work.Count > 0)
    {
        var method = work.Dequeue();
        if (!visited.Add(method))
            continue;
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null)
            continue;
        for (var offset = 0; offset < il.Length;)
        {
            ushort code = il[offset++];
            if (code == 0xfe) code = (ushort)(0xfe00 | il[offset++]);
            var opcode = opcodes[code];
            var size = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, offset),
                _ => 4
            };
            if (opcode.OperandType == OperandType.InlineString &&
                method.Module.ResolveString(BitConverter.ToInt32(il, offset)) == "Method unstripping failed")
                errors.Add(method.DeclaringType!.FullName + "." + method.Name);
            if (opcode == OpCodes.Box && method.DeclaringType?.FullName == "UnityEngine.GUILayout")
                errors.Add(method.DeclaringType.FullName + "." + method.Name + ": CLR boxing in an IL2CPP layout-option factory.");
            if (opcode.OperandType == OperandType.InlineMethod)
            {
                try
                {
                    var called = method.Module.ResolveMethod(BitConverter.ToInt32(il, offset),
                        method.DeclaringType?.GetGenericArguments(), method.IsGenericMethod ? method.GetGenericArguments() : null)!;
                    if (called.DeclaringType!.Assembly.GetName().Name!.StartsWith("UnityEngine") || allowedAssemblies.Contains(called.DeclaringType.Assembly))
                        work.Enqueue(called);
                }
                catch (MissingMethodException ex) { errors.Add(method.DeclaringType!.FullName + "." + method.Name + ": " + ex.Message); }
            }
            offset += size;
        }
    }
    visitedCount = visited.Count;
    return errors;
}

class ConstructorOnlyFailure
{
    public ConstructorOnlyFailure() { throw new NotSupportedException("Method unstripping failed"); }
}
class InitializerOnlyFailure
{
    static InitializerOnlyFailure() { throw new NotSupportedException("Method unstripping failed"); }
}
