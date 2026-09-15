#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.CrashCapture;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Prompt;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Wires AppData, first-run persist, crash load, and <see cref="IUserPrompt"/> for both heads.</summary>
    public sealed class IngestBenchHost : IDisposable
    {
        private DebouncedPathWatcher? _watch;
        private string[] _scanRoots = Array.Empty<string>();
        private readonly IUiDispatcher _ui;
        private readonly DestSettingsStore _dest;
        public const string SkipOnboardingEnv = "QMI_SKIP_ONBOARDING";

        public IngestBenchHost(
            IAppPaths paths,
            IngestBenchAppModel model,
            OnboardingStore onboarding,
            IPendingCrashStore crashes,
            IUserPrompt prompt,
            IUiDispatcher? ui = null)
        {
            Paths = paths;
            Model = model;
            Onboarding = onboarding;
            Crashes = crashes;
            Prompt = prompt;
            _ui = ui ?? InlineUiDispatcher.Instance;
            _dest = new DestSettingsStore(paths);
        }

        public IAppPaths Paths { get; }

        public IngestBenchAppModel Model { get; }

        public OnboardingStore Onboarding { get; }

        public IPendingCrashStore Crashes { get; }

        public IUserPrompt Prompt { get; }

        internal string[] ScanRoots
        {
            get => _scanRoots;
            set => _scanRoots = value ?? Array.Empty<string>();
        }

        internal List<ItemGroup> Groups { get; } = new();

        internal Queue<PendingImportPlan> ImportJobs { get; } = new();

        internal List<string> FailedPaths { get; } = new();

        internal ImportHashCatalog HashCatalog { get; } = new();

        internal WatchFolderService? FolderWatch { get; set; }

        internal CancellationTokenSource? ImportCts { get; set; }

        public static IAppPaths CreatePaths() =>
            OperatingSystem.IsLinux() ? new XdgAppPaths() : DefaultAppPaths.Instance;

        public static IngestBenchHost Create(IUiDispatcher? ui = null, IAppPaths? paths = null, IUserPrompt? prompt = null)
        {
            IAppPaths resolved = paths ?? CreatePaths();
            var model = new IngestBenchAppModel();
            IUiDispatcher dispatcher = ui ?? InlineUiDispatcher.Instance;
            IUserPrompt resolvedPrompt = prompt ?? new AppModelUserPrompt(model, dispatcher);
            var host = new IngestBenchHost(
                resolved,
                model,
                new OnboardingStore(resolved),
                new FilePendingCrashStore(resolved),
                resolvedPrompt,
                dispatcher);
            host.ApplyPersistedState();
            return host;
        }

        public void ApplyPersistedState()
        {
            _dest.ApplyTo(Model);
            IngestBenchFtp.Load(this);
            IngestBenchAdb.Apply(this);
            IngestBenchHistory.Load(this);
            IngestBenchExclusions.Load(this);
            IngestBenchPrefs.Load(this);
            IngestBenchDest.KeepCustomWhenDestDiffers(this);
            IngestBenchAbout.RememberInstall(this);
            IngestBenchQueue.Load(this);
            IngestBenchPost.LoadCatalog(this);
            IngestBenchWatch.Attach(this);
            IngestBenchThumbs.NoteIcc(this);
            bool skip = string.Equals(Environment.GetEnvironmentVariable(SkipOnboardingEnv), "1", StringComparison.Ordinal);
            Model.IsFirstRun = !skip && !Onboarding.IsDismissed();
            IngestBenchCrash.Load(this);
        }

        public void DismissOnboarding()
        {
            Model.DismissOnboarding();
            Onboarding.SaveDismissed();
        }

        public void DismissCrash() => IngestBenchCrash.Dismiss(this);

        public void CompletePrompt(bool accepted)
        {
            if (Prompt is AppModelUserPrompt appPrompt)
            {
                appPrompt.Complete(accepted);
                return;
            }

            Model.ShowUserPrompt = false;
        }

        public void SetDestination(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            Model.DestinationRoot = Path.GetFullPath(path);
            _dest.Save(Model);
        }

        public void SetNamingTemplate(string template) =>
            IngestBenchNaming.ApplyTemplate(this, template);

        public void BeginDrivePick(IEnumerable<string>? extraRoots = null) =>
            IngestBenchScan.BeginPick(this, extraRoots);

        public void CancelDrivePick() => Model.ShowDrivePicker = false;

        public void ConfirmDrivePick()
        {
            IngestBenchScan.Remember(this);
            _scanRoots = Model.Volumes.Where(v => v.IsSelected && Directory.Exists(v.Path)).Select(v => v.Path).ToArray();
            RecalcSourceCount();
            Model.ShowDrivePicker = false;
            AttachWatch();
            IngestBenchShoots.Rebuild(this);
        }

        public void RefreshScan() => IngestBenchScan.Refresh(this);

        public Task<bool> RunImportAsync(bool dryRun, IngestBenchCopy? copy = null) =>
            IngestBenchImport.RunAsync(this, dryRun, copy ?? IngestBenchCopy.Neutral);

        public void Persist()
        {
            IngestBenchPrefs.Save(this);
            _dest.Save(Model);
            if (Model.Volumes.Count > 0)
            {
                ScanSourceStore.Save(Paths, Model.Volumes);
            }
        }

        public void Post(Action action) => _ui.Post(action);

        public void Dispose()
        {
            Persist();
            IngestBenchWatch.Detach(this);
            _watch?.Dispose();
            _watch = null;
        }

        internal void RetargetWatch() => AttachWatch();

        private void RecalcSourceCount() =>
            _ui.Post(() => Model.NoteSources(VolumeScan.CountMedia(_scanRoots, excludeFolders: Model.ExcludedFolders)));

        private void AttachWatch()
        {
            _watch?.Dispose();
            _watch = null;
            string? root = _scanRoots.FirstOrDefault(Directory.Exists);
            if (root is null)
            {
                return;
            }

            _watch = new DebouncedPathWatcher(root, RecalcSourceCount);
        }
    }
}
