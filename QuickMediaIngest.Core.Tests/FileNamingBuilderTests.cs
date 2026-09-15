#nullable enable
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class FileNamingBuilderTests
{
    [Fact]
    public void RecommendedPreset_DateShootOriginal()
    {
        string template = FileNamingBuilder.BuildTemplate(
            FileNamingBuilder.PresetParts(FileNamingBuilder.PresetRecommended, FileNamingBuilder.DefaultParts));
        Assert.Equal("[Date]_[ShootName]_[Original]", template);
    }

    [Fact]
    public void DateTimePreset_IncludesTime()
    {
        string template = FileNamingBuilder.BuildTemplate(
            FileNamingBuilder.PresetParts(FileNamingBuilder.PresetDateTime, FileNamingBuilder.DefaultParts));
        Assert.Equal("[Date]_[Time]_[ShootName]_[Original]", template);
    }

    [Fact]
    public void InsertToken_SkipsDuplicateBracketToken()
    {
        Assert.Equal("[Date]_[Original]", FileNamingBuilder.InsertToken("[Date]_[Original]", "[Date]"));
        Assert.Equal("[Date]_[Original]_[ShootName]", FileNamingBuilder.InsertToken("[Date]_[Original]", "[ShootName]"));
    }

    [Fact]
    public void RemoveToken_FallsBackToOriginal()
    {
        Assert.Equal("[Original]", FileNamingBuilder.RemoveToken("[Date]", "[Date]"));
    }

    [Fact]
    public void CoercePreset_DivergedTemplateBecomesCustom()
    {
        NamingParts parts = FileNamingBuilder.DefaultParts;
        Assert.Equal(
            FileNamingBuilder.PresetCustom,
            FileNamingBuilder.CoercePreset(FileNamingBuilder.PresetRecommended, "[Original]", parts));
        Assert.Equal(
            FileNamingBuilder.PresetRecommended,
            FileNamingBuilder.CoercePreset(
                FileNamingBuilder.PresetRecommended,
                "[Date]_[ShootName]_[Original]",
                parts with { Time = false, Shoot = true }));
    }

    [Fact]
    public void Preview_AppliesLowercaseAndSequence()
    {
        IReadOnlyList<string> examples = FileNamingPreview.Examples(
            "[ShootName]_[Sequence]_[Original]",
            FileNamingBuilder.DefaultParts with { Separator = "_" },
            "MyShoot",
            lowercase: true);
        Assert.Equal(3, examples.Count);
        Assert.Equal("myshoot_0001_img_0001.jpg", examples[0]);
        Assert.Contains("[Time]", FileNamingPreview.UnusedFileTokens("[Date]_[Original]"));
        Assert.Contains("[Date]", FileNamingPreview.UsedFileTokens("[Date]_[Original]"));
    }
}

public sealed class IngestBenchNamingTests
{
    [Fact]
    public void ChipsAndPreset_PersistInDestJson()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-naming-" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(root);
        var paths = new NamingPaths(root);
        using (IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance))
        {
            IngestBenchNaming.ApplyPreset(host, FileNamingBuilder.PresetRecommended);
            Assert.Equal("[Date]_[ShootName]_[Original]", host.Model.NamingTemplate);
            Assert.Contains("[Date]", host.Model.Naming.SelectedTokens);
            Assert.DoesNotContain("[Date]", host.Model.Naming.AvailableTokens);
            IngestBenchNaming.InsertFileToken(host, "[Sequence]");
            Assert.Contains("[Sequence]", host.Model.NamingTemplate);
            IngestBenchNaming.InsertFolderToken(host, "[ShootTitle]");
            Assert.Contains("[ShootTitle]", host.Model.DestFolderTemplate);
            Assert.NotEmpty(host.Model.Naming.PreviewExamples);
        }

        using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Contains("[Sequence]", again.Model.NamingTemplate);
        Assert.Contains("[ShootTitle]", again.Model.DestFolderTemplate);
        Assert.Contains("[Sequence]", again.Model.Naming.SelectedTokens);
    }

    [Fact]
    public void Checkboxes_AppendInTheOrderTheyAreTicked()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-order-" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(root);
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new NamingPaths(root), SilentUserPrompt.Instance);
        host.Model.NamingTemplate = string.Empty;
        IngestBenchNaming.Hydrate(host.Model);
        IngestBenchNaming.ToggleFile(host, "Original", include: true);
        IngestBenchNaming.ToggleFile(host, "Date", include: true);
        Assert.Equal("[Original]_[Date]", host.Model.NamingTemplate);
        host.Model.Naming.DateFormat = "yyyyMMdd";
        IngestBenchNaming.ApplyOptions(host);
        Assert.Equal("[Original]_[YYYY][MM][DD]", host.Model.NamingTemplate);

        IngestBenchNaming.ToggleFolder(host, "[Camera]", include: true);
        IngestBenchNaming.ToggleFolder(host, "[ShootTitle]", include: true);
        Assert.Equal("[Camera]_[ShootTitle]", host.Model.DestFolderTemplate);
        IngestBenchNaming.ToggleFolder(host, "[Camera]", include: false);
        Assert.Equal("[ShootTitle]", host.Model.DestFolderTemplate);
        Assert.False(host.Model.Naming.IncludeFolderCamera);
        Assert.True(host.Model.Naming.IncludeFolderShoot);
    }

    [Fact]
    public void FolderField_AddKeepsFullChoiceList_AndPreview()
    {
        Assert.True(FileNamingFields.SyncFromFolder("[ShootTitle]_[Date]").ShootTitle);
        IReadOnlyList<string> empty = FileNamingPreview.FolderExamples("");
        Assert.Contains("save location", empty[0], System.StringComparison.OrdinalIgnoreCase);

        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-folder-" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(root);
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new NamingPaths(root), SilentUserPrompt.Instance);
        IngestBenchNaming.ToggleFolder(host, "[ShootTitle]", include: true);
        Assert.Contains("[ShootTitle]", host.Model.DestFolderTemplate);
        Assert.True(host.Model.Naming.IncludeFolderShoot);
        Assert.Contains("studio-day", host.Model.Naming.FolderPreview[0]);
        IngestBenchNaming.ToggleFolder(host, "[Date]", include: true);
        Assert.Equal("[ShootTitle]_[Date]", host.Model.DestFolderTemplate);
    }

    private sealed class NamingPaths : IAppPaths
    {
        public NamingPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
