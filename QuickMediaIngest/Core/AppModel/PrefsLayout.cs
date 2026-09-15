#nullable enable
using System;
using System.Text.Json.Nodes;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Window bounds and preview-pane width in <c>prefs.json</c>.</summary>
    public static class PrefsLayout
    {
        public const double DefaultPane = 360;
        public const double MinPane = 200;
        public const double MaxPane = 4000;

        public static bool HasSavedSize(double width, double height) =>
            width >= 400 && height >= 300;

        public static double ClampPane(double width)
        {
            if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
            {
                return DefaultPane;
            }

            return Math.Clamp(width, MinPane, MaxPane);
        }

        public static double CoalescePane(double saved, double measured) =>
            measured >= MinPane ? ClampPane(measured) : ClampPane(saved);

        public static void Write(JsonObject obj, PrefsStore.FileDto dto)
        {
            obj["WindowWidth"] = dto.WindowWidth;
            obj["WindowHeight"] = dto.WindowHeight;
            obj["WindowLeft"] = dto.WindowLeft;
            obj["WindowTop"] = dto.WindowTop;
            obj["WindowMaximized"] = dto.WindowMaximized;
            obj["WindowPositionSet"] = dto.WindowPositionSet;
            obj["PreviewPaneWidth"] = dto.PreviewPaneWidth;
        }

        public static void Read(JsonObject obj, PrefsStore.FileDto dto)
        {
            dto.WindowWidth = Num(obj, "WindowWidth");
            dto.WindowHeight = Num(obj, "WindowHeight");
            dto.WindowLeft = Num(obj, "WindowLeft");
            dto.WindowTop = Num(obj, "WindowTop");
            dto.WindowMaximized = Flag(obj, "WindowMaximized");
            dto.WindowPositionSet = Flag(obj, "WindowPositionSet");
            dto.PreviewPaneWidth = Num(obj, "PreviewPaneWidth");
        }

        private static bool Flag(JsonObject obj, string name) =>
            obj[name] is JsonValue value && value.TryGetValue(out bool flag) && flag;

        private static double Num(JsonObject obj, string name)
        {
            if (obj[name] is not JsonValue value)
            {
                return 0;
            }

            if (value.TryGetValue(out double d))
            {
                return d;
            }

            return value.TryGetValue(out int i) ? i : 0;
        }
    }
}
