#nullable enable
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.ImportUi;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Import meter + afterglow (Open folder via xdg-open). Progress stays in the status bar.</summary>
    public static class IngestBenchScene
    {
        public static void Begin(IngestBenchAppModel model)
        {
            model.ShowAfterglow = false;
            IngestBenchActivity.Begin(model, IngestBenchActivity.Import, "Importing…", indeterminate: false);
            model.IsImporting = true;
        }

        public static void Progress(IngestBenchAppModel model, int percent, string status)
        {
            model.ImportPercent = ImportProgressScene.ClampPercent(percent);
            model.ImportStatus = status ?? string.Empty;
        }

        public static void Complete(IngestBenchAppModel model, IngestBenchCopy copy, int importedCount, int failedCount = 0)
        {
            IngestBenchActivity.Idle(model);
            model.IsImporting = false;
            model.ImportPercent = 100;
            model.ImportStatus = IngestBenchActivity.Counts("Import", importedCount, failedCount);
            model.AfterglowFolder = model.DestinationRoot ?? string.Empty;
            string folder = ImportAfterglow.FolderLabel(model.AfterglowFolder);
            model.AfterglowText = failedCount > 0
                ? copy.Format("ImportAfterglow_TextWarning", new object[] { importedCount, folder, failedCount })
                : copy.Format("ImportAfterglow_Text", new object[] { importedCount, folder });
            model.ShowAfterglow = importedCount > 0 && ImportProgressScene.ShowOpenDestination(false, model.AfterglowFolder);
            IngestBenchA11y.Announce(model, model.AfterglowText);
            if (importedCount > 0 && !string.IsNullOrWhiteSpace(model.AfterglowText))
            {
                model.Notifications.Insert(0, model.AfterglowText);
                model.UnreadNotificationCount++;
                while (model.Notifications.Count > 20)
                {
                    model.Notifications.RemoveAt(model.Notifications.Count - 1);
                }
            }
        }

        public static void End(IngestBenchAppModel model, string status)
        {
            model.IsImporting = false;
            IngestBenchActivity.Idle(model);
            model.ImportStatus = status ?? string.Empty;
        }

        public static bool OpenFolder(IngestBenchAppModel model)
        {
            bool opened = ShellOpen.TryOpen(model.AfterglowFolder);
            if (!opened && !string.IsNullOrWhiteSpace(model.AfterglowFolder))
            {
                model.ImportStatus = "Could not open folder: " + model.AfterglowFolder;
            }

            return opened;
        }

        public static void Dismiss(IngestBenchAppModel model) => model.ShowAfterglow = false;
    }
}
