#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>SHA-256 manifests, 3-2-1 dest, XMP stamps, already-imported hash catalog.</summary>
    public static class IngestBenchPost
    {
        public static string CatalogFile(IAppPaths paths) =>
            Path.Combine(paths.AppDataRoot, "import-hashes.json");

        public static void LoadCatalog(IngestBenchHost host) =>
            host.HashCatalog.LoadAsync(CatalogFile(host.Paths)).GetAwaiter().GetResult();

        public static IngestOptions CreateOptions(IngestBenchAppModel model, bool dryRun)
        {
            PrefsState prefs = model.Prefs;
            return new IngestOptions
            {
                IsDryRun = dryRun,
                StripGpsAndPii = prefs.StripGps,
                SecondaryDestinationRoot = BlankToNull(prefs.SecondaryDestination),
                CreatorStamp = BlankToNull(prefs.CreatorStamp),
                CopyrightStamp = BlankToNull(prefs.CopyrightStamp),
                WriteXmpSidecarsOnly = prefs.WriteXmpSidecar,
                DestinationFolderTemplate = BlankToNull(model.DestFolderTemplate),
                DuplicateHandling = Duplicates(prefs.DuplicatePolicy),
                VerificationMode = string.Equals(prefs.VerificationMode, "Strict", StringComparison.OrdinalIgnoreCase)
                    ? ImportVerificationMode.Strict
                    : ImportVerificationMode.Fast,
            };
        }

        public static DuplicateHandlingMode Duplicates(string? policy) =>
            policy switch
            {
                "Skip" => DuplicateHandlingMode.Skip,
                "OverwriteIfNewer" => DuplicateHandlingMode.OverwriteIfNewer,
                _ => DuplicateHandlingMode.Suffix,
            };

        public static async Task SkipAlreadyImportedAsync(
            IngestBenchHost host,
            IEnumerable<ItemGroup> groups,
            CancellationToken cancellationToken = default)
        {
            foreach (ImportItem item in groups.SelectMany(g => g.Items).Where(i => i.IsSelected))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!File.Exists(item.SourcePath))
                {
                    continue;
                }

                string hash = await host.HashCatalog.ComputeFileHashAsync(item.SourcePath, cancellationToken);
                if (host.HashCatalog.IsAlreadyImported(hash))
                {
                    item.IsSelected = false;
                }
            }
        }

        public static async Task AfterImportAsync(
            IngestBenchHost host,
            IReadOnlyList<ItemGroup> groups,
            IngestBenchCopy copy,
            IReadOnlyDictionary<string, List<(string file, string hash)>>? manifests = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host.Model.DestinationRoot))
            {
                return;
            }

            if (host.Model.Prefs.WriteChecksumManifest && manifests != null)
            {
                foreach ((string folder, List<(string file, string hash)> rows) in manifests)
                {
                    await ShootChecksumManifestWriter.WritePrecomputedAsync(folder, rows, cancellationToken);
                }
            }

            await host.HashCatalog.SaveAsync(CatalogFile(host.Paths));
            IngestBenchThumbs.ForgetImported(
                host,
                groups.SelectMany(g => g.Items).Where(i => i.IsSelected));
            await IngestBenchEject.AfterImportAsync(host, groups, copy);
        }

        private static string? BlankToNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
