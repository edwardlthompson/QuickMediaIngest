#nullable enable
using System;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Overall import meter: files done/total, elapsed, remaining.</summary>
    public static class IngestBenchImportProgress
    {
        public static int Percent(int done, int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            return (int)Math.Clamp(100L * Math.Max(0, done) / total, 0, 100);
        }

        public static string Clock(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                elapsed = TimeSpan.Zero;
            }

            return elapsed.TotalHours >= 1
                ? elapsed.ToString(@"h\:mm\:ss")
                : elapsed.ToString(@"m\:ss");
        }

        public static string Eta(int done, int total, TimeSpan elapsed)
        {
            if (done <= 0 || total <= done || elapsed.TotalSeconds < 1)
            {
                return string.Empty;
            }

            double seconds = (total - done) * (elapsed.TotalSeconds / done);
            return Clock(TimeSpan.FromSeconds(Math.Max(0, seconds)));
        }

        public static string Line(string phase, int done, int total, TimeSpan elapsed)
        {
            string eta = Eta(done, total, elapsed);
            string body = phase + " " + done + "/" + total + "  " + Clock(elapsed) + " elapsed";
            return eta.Length == 0 ? body : body + "  ~" + eta + " left";
        }

        public static string Dual(int copy, int verify, int total, TimeSpan elapsed)
        {
            string eta = Eta(verify, total, elapsed);
            string body = "Copy " + copy + "/" + total + "  Verify " + verify + "/" + total
                + "  " + Clock(elapsed) + " elapsed";
            return eta.Length == 0 ? body : body + "  ~" + eta + " left";
        }
    }
}
