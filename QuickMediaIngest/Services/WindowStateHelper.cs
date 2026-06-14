using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace QuickMediaIngest.Services
{
    public static class WindowStateHelper
    {
        public const double MinWidth = 400;
        public const double MinHeight = 300;
        public const double MinVisibleOverlap = 100;

        public static (double Width, double Height, double Left, double Top) ClampToVisibleBounds(
            double width,
            double height,
            double left,
            double top,
            IEnumerable<Rect> workingAreas)
        {
            width = Math.Max(MinWidth, width);
            height = Math.Max(MinHeight, height);

            var areas = workingAreas.ToList();
            if (areas.Count == 0)
            {
                areas.Add(GetFallbackWorkingArea());
            }

            if (!HasSufficientVisibleOverlap(left, top, width, height, areas))
            {
                var primary = areas[0];
                left = primary.Left + Math.Max(0, (primary.Width - width) / 2);
                top = primary.Top + Math.Max(0, (primary.Height - height) / 2);
            }

            left = ClampAxis(left, width, areas, horizontal: true);
            top = ClampAxis(top, height, areas, horizontal: false);

            return (width, height, left, top);
        }

        public static (double Width, double Height, double Left, double Top, bool Maximized) GetBoundsToPersist(Window window)
        {
            bool maximized = window.WindowState == WindowState.Maximized;
            Rect bounds = maximized
                ? window.RestoreBounds
                : new Rect(window.Left, window.Top, window.Width, window.Height);

            return (bounds.Width, bounds.Height, bounds.Left, bounds.Top, maximized);
        }

        public static IEnumerable<Rect> GetAllWorkingAreas()
        {
            var areas = new List<Rect>();
            EnumDisplayMonitors(
                IntPtr.Zero,
                IntPtr.Zero,
                (IntPtr hMonitor, IntPtr _, ref NativeRect rect, IntPtr __) =>
                {
                    var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
                    if (GetMonitorInfo(hMonitor, ref monitorInfo))
                    {
                        areas.Add(ToRect(monitorInfo.rcWork));
                    }
                    else
                    {
                        areas.Add(ToRect(rect));
                    }

                    return true;
                },
                IntPtr.Zero);

            return areas;
        }

        private static Rect GetFallbackWorkingArea()
        {
            var workArea = SystemParameters.WorkArea;
            return new Rect(workArea.Left, workArea.Top, workArea.Width, workArea.Height);
        }

        private static bool HasSufficientVisibleOverlap(
            double left,
            double top,
            double width,
            double height,
            IReadOnlyList<Rect> workingAreas)
        {
            var windowRect = new Rect(left, top, width, height);
            foreach (var area in workingAreas)
            {
                var intersection = Rect.Intersect(windowRect, area);
                if (intersection.Width >= MinVisibleOverlap && intersection.Height >= MinVisibleOverlap)
                {
                    return true;
                }
            }

            return false;
        }

        private static double ClampAxis(double position, double size, IReadOnlyList<Rect> workingAreas, bool horizontal)
        {
            double minPosition = workingAreas.Min(area => horizontal ? area.Left : area.Top);
            double maxPosition = workingAreas.Max(area => horizontal ? area.Right : area.Bottom) - size;
            if (maxPosition < minPosition)
            {
                return minPosition;
            }

            return Math.Clamp(position, minPosition, maxPosition);
        }

        private static Rect ToRect(NativeRect rect) =>
            new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int cbSize;
            public NativeRect rcMonitor;
            public NativeRect rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(
            IntPtr hdc,
            IntPtr lprcClip,
            MonitorEnumProc lpfnEnum,
            IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
    }
}
