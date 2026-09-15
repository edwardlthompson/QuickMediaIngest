#nullable enable
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.AppModel
{
    public static class IngestBenchImportCancel
    {
        public static async Task RequestAsync(IngestBenchHost host, IngestBenchCopy copy)
        {
            if (host.ImportCts is null)
            {
                return;
            }

            if (!await host.Prompt.ConfirmAsync(
                    copy.Get("Msg_CancelImport_ConfirmTitle"),
                    copy.Get("Msg_CancelImport_ConfirmBody")))
            {
                return;
            }

            host.ImportCts.Cancel();
        }
    }
}
