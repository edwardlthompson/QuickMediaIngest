#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using ImageMagick;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Headless Magick + libvips (+ optional SQLite) probe for trimmed linux-x64 publish.
    /// SQLite is Windows WPF-only; Desktop prints SKIP when the assembly is absent.
    /// </summary>
    public static class NativeLibrarySmoke
    {
        public const string Flag = "--smoke-native";

        public static bool TryHandle(string[] args, out int exitCode)
        {
            exitCode = 0;
            foreach (string arg in args)
            {
                if (string.Equals(arg, Flag, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "--smoke-libvips", StringComparison.OrdinalIgnoreCase))
                {
                    exitCode = Run();
                    return true;
                }
            }

            return false;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(MagickImage))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(NetVips.NetVips))]
        public static int Run()
        {
            int failures = 0;
            failures += Probe("magick", ProbeMagick);
            failures += Probe("libvips", ProbeVips);
            failures += ProbeSqlite();
            return failures == 0 ? 0 : 1;
        }

        private static int Probe(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine($"OK {name}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
                return 1;
            }
        }

        private static void ProbeMagick()
        {
            using var image = new MagickImage(MagickColors.Black, 1, 1);
            if (image.Width != 1 || image.Height != 1)
            {
                throw new InvalidOperationException($"unexpected size {image.Width}x{image.Height}");
            }
        }

        private static void ProbeVips() => _ = NetVips.NetVips.Version(0);

        private static int ProbeSqlite()
        {
            Type? sqlite = Type.GetType("System.Data.SQLite.SQLiteConnection, System.Data.SQLite");
            if (sqlite is null)
            {
                Console.WriteLine("SKIP sqlite (not referenced)");
                return 0;
            }

            try
            {
                object? connection = Activator.CreateInstance(sqlite, "Data Source=:memory:;Version=3;");
                if (connection is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                Console.WriteLine("OK sqlite");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL sqlite: {ex.Message}");
                return 1;
            }
        }
    }
}
