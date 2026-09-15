#nullable enable
namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Applies presets, checkboxes, and preview to <see cref="IngestBenchAppModel"/>.</summary>
    public static class IngestBenchNaming
    {
        public static void Hydrate(IngestBenchAppModel model)
        {
            NamingState n = model.Naming;
            n.Suppress = true;
            n.FromParts(FileNamingBuilder.SyncFromTemplate(model.NamingTemplate));
            n.FromFolderParts(FileNamingFields.SyncFromFolder(model.DestFolderTemplate));
            n.Preset = FileNamingBuilder.CoercePreset(n.Preset, model.NamingTemplate, n.ToParts());
            n.RefreshLists(model.NamingTemplate, model.DestFolderTemplate);
            n.Suppress = false;
        }

        public static void ApplyTemplate(IngestBenchHost host, string? template)
        {
            host.Model.NamingTemplate = string.IsNullOrWhiteSpace(template)
                ? DestSettingsStore.DefaultNaming
                : template.Trim();
            Hydrate(host.Model);
            Save(host);
        }

        public static void ApplyPreset(IngestBenchHost host, string? preset)
        {
            NamingState n = host.Model.Naming;
            if (n.Suppress)
            {
                return;
            }

            n.Suppress = true;
            n.Preset = string.IsNullOrWhiteSpace(preset) ? FileNamingBuilder.PresetCustom : preset;
            n.FromParts(FileNamingBuilder.PresetParts(n.Preset, n.ToParts()));
            host.Model.NamingTemplate = FileNamingBuilder.BuildTemplate(n.ToParts());
            n.RefreshLists(host.Model.NamingTemplate, host.Model.DestFolderTemplate);
            n.Suppress = false;
            Save(host);
        }

        public static void ApplyOptions(IngestBenchHost host)
        {
            NamingState n = host.Model.Naming;
            if (n.Suppress)
            {
                return;
            }

            n.Suppress = true;
            n.Preset = FileNamingBuilder.PresetCustom;
            host.Model.NamingTemplate = FileNamingFields.RewriteFileFormats(host.Model.NamingTemplate, n.ToParts());
            n.RefreshLists(host.Model.NamingTemplate, host.Model.DestFolderTemplate);
            n.Suppress = false;
            Save(host);
        }

        public static void ToggleFile(IngestBenchHost host, string? field, bool include)
        {
            if (host.Model.Naming.Suppress || string.IsNullOrEmpty(field))
            {
                return;
            }

            if (include)
            {
                ApplyTemplate(host, FileNamingBuilder.InsertToken(
                    host.Model.NamingTemplate,
                    FileNamingFields.FileToken(field, host.Model.Naming.ToParts())));
                return;
            }

            ApplyTemplate(host, FileNamingFields.StripFileField(host.Model.NamingTemplate, field));
        }

        public static void ToggleFolder(IngestBenchHost host, string? token, bool include)
        {
            if (host.Model.Naming.Suppress || string.IsNullOrEmpty(token))
            {
                return;
            }

            if (include)
            {
                InsertFolderToken(host, token);
                return;
            }

            host.Model.DestFolderTemplate = FileNamingFields.Strip(host.Model.DestFolderTemplate, token, emptyOk: true);
            host.Model.Naming.FromFolderParts(FileNamingFields.SyncFromFolder(host.Model.DestFolderTemplate));
            host.Model.Naming.RefreshLists(host.Model.NamingTemplate, host.Model.DestFolderTemplate);
            Save(host);
        }

        public static void RefreshPreview(IngestBenchHost host)
        {
            host.Model.Naming.RefreshLists(host.Model.NamingTemplate, host.Model.DestFolderTemplate);
            Save(host);
        }

        public static void InsertFileToken(IngestBenchHost host, string? token) =>
            ApplyTemplate(host, FileNamingBuilder.InsertToken(host.Model.NamingTemplate, token));

        public static void RemoveFileToken(IngestBenchHost host, string? token) =>
            ApplyTemplate(host, FileNamingBuilder.RemoveToken(host.Model.NamingTemplate, token));

        public static void InsertFolderToken(IngestBenchHost host, string? token)
        {
            host.Model.DestFolderTemplate = FileNamingBuilder.InsertToken(host.Model.DestFolderTemplate, token);
            host.Model.Naming.FromFolderParts(FileNamingFields.SyncFromFolder(host.Model.DestFolderTemplate));
            host.Model.Naming.RefreshLists(host.Model.NamingTemplate, host.Model.DestFolderTemplate);
            Save(host);
        }

        public static void Save(IngestBenchHost host) =>
            new DestSettingsStore(host.Paths).Save(host.Model);
    }
}
