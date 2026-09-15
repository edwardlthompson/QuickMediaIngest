#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.X11;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (!UidGuard.TryRefuseRoot(out string message))
        {
            Console.Error.WriteLine(message);
            return 1;
        }

        if (NativeLibrarySmoke.TryHandle(args, out int smokeCode))
        {
            return smokeCode;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions { WmClass = "quick-media-ingest" })
            .LogToTrace();
}
