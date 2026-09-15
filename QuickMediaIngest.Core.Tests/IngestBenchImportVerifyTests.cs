#nullable enable
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchImportVerifyTests
{
    [Fact]
    public async Task CompleteThenDrain_EmptyChannel_Returns()
    {
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new VerifyPaths(), SilentUserPrompt.Instance);
        var verify = IngestBenchImportVerify.Start(host, 1, Stopwatch.StartNew(), CancellationToken.None);
        verify.Complete();
        await verify.Drain();
    }

    [Fact]
    public async Task NoteCopy_MissingDest_MarksFailedAndDoesNotDrop()
    {
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new VerifyPaths(), SilentUserPrompt.Instance);
        var item = new QuickMediaIngest.Core.Models.ImportItem
        {
            FileName = "gone.jpg",
            SourcePath = "/tmp/qmi-gone.jpg",
            IsSelected = true,
        };
        var group = new QuickMediaIngest.Core.Models.ItemGroup { Title = "Shoot" };
        group.Items.Add(item);
        host.Groups.Add(group);
        host.Model.Shoots.Add(group);
        var verify = IngestBenchImportVerify.Start(host, 1, Stopwatch.StartNew(), CancellationToken.None);
        verify.NoteCopy(new IngestProgressInfo
        {
            Success = true,
            SourcePath = item.SourcePath,
            DestinationPath = Path.Combine(Path.GetTempPath(), "qmi-missing-dest-" + Path.GetRandomFileName()),
        });
        verify.Complete();
        await verify.Drain();
        Assert.Contains(item.SourcePath, host.FailedPaths);
        Assert.Single(group.Items);
    }
}

file sealed class VerifyPaths : IAppPaths
{
    public VerifyPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-ver-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
