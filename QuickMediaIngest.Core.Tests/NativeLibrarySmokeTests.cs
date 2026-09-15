#nullable enable
using System;
using ImageMagick;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class NativeLibrarySmokeTests
{
    [Fact]
    public void MagickNet_IsAtLeast_14_16_0()
    {
        Version? ver = typeof(MagickImage).Assembly.GetName().Version;
        Assert.NotNull(ver);
        Assert.True(ver >= new Version(14, 16, 0), $"Magick.NET {ver} is below 14.16.0 (GHSA floor).");
    }

    [Fact]
    public void TryHandle_IgnoresUnrelatedArgs()
    {
        Assert.False(NativeLibrarySmoke.TryHandle(new[] { "--help" }, out int code));
        Assert.Equal(0, code);
    }

    [Fact]
    public void TryHandle_SmokeNative_LoadsMagickAndVips()
    {
        Assert.True(NativeLibrarySmoke.TryHandle(new[] { NativeLibrarySmoke.Flag }, out int code));
        Assert.Equal(0, code);
    }

    [Fact]
    public void Run_PrintsSqliteSkipWhenUnreferenced()
    {
        Assert.Equal(0, NativeLibrarySmoke.Run());
    }
}
