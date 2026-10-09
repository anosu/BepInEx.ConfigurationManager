using System.Collections.Generic;

namespace ConfigurationManager
{
    internal static class Localization
    {
        internal const string English = "English";
        internal const string SimplifiedChinese = "简体中文";
        internal static string Language = English;

        private static readonly Dictionary<string, string> Chinese = new Dictionary<string, string>
        {
            { "Enabled", "已启用" },
            { "Disabled", "已禁用" },
            { "Press any key", "请按任意键" },
            { "Cancel", "取消" },
            { "Set...", "设置…" },
            { "Set the key by pressing any key on your keyboard.", "按键盘上的任意键设置按键。" },
            { "Press any key combination", "请按快捷键组合" },
            { "Clear", "清除" },
            { "Hex", "十六进制" },
            { "Plugin / mod settings", "插件 / 模组设置" },
            { "Plugins with no options available: ", "没有可配置选项的插件：" },
            { "Tip: Click a plugin to expand; hover over names for details.", "提示：点击插件名称展开，将鼠标移到设置和分组名称上查看说明。" },
            { "Tip: Drag the title to move; press the shortcut to close.", "提示：拖动标题可移动窗口，再按一次快捷键可关闭。" },
            { "Normal settings", "普通设置" },
            { "Normal", "普通" },
            { "Keys", "快捷键" },
            { "Advanced", "高级" },
            { "Debug", "调试" },
            { "Log", "日志" },
            { "Keyboard shortcuts", "快捷键" },
            { "Advanced settings", "高级设置" },
            { "Debug info", "调试信息" },
            { "Open Log", "打开日志" },
            { "Close", "关闭" },
            { "Search: ", "搜索：" },
            { "Expand All", "全部展开" },
            { "Collapse All", "全部折叠" },
            { "Failed to draw this field, check log for details.", "无法显示此设置，请查看日志。" },
            { "Reset", "重置" },
            { "Language", "语言" },
            { "Not set", "未设置" },
            { "No settings to display. Check the filters above.", "没有可显示的设置，请检查上方筛选条件。" },
            { "No matching settings. Try another search or clear it.", "没有匹配的设置，请更换关键词或清除搜索。" },
        };

        internal static string Text(string english)
        {
            return Language == SimplifiedChinese && Chinese.TryGetValue(english, out var translated) ? translated : english;
        }
    }
}
