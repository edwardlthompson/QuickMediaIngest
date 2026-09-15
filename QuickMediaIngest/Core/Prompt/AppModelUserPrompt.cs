#nullable enable
using System.Threading.Tasks;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Core.Prompt
{
    /// <summary>Drives <see cref="IngestBenchAppModel"/> prompt overlay fields (WPF-free).</summary>
    public sealed class AppModelUserPrompt : IUserPrompt
    {
        private readonly IngestBenchAppModel _model;
        private readonly IUiDispatcher _ui;
        private TaskCompletionSource<bool>? _tcs;

        public AppModelUserPrompt(IngestBenchAppModel model, IUiDispatcher? ui = null)
        {
            _model = model;
            _ui = ui ?? InlineUiDispatcher.Instance;
        }

        public Task NotifyAsync(string title, string body) => Show(title, body, isConfirm: false);

        public Task<bool> ConfirmAsync(string title, string body) => Show(title, body, isConfirm: true);

        public void Complete(bool accepted)
        {
            _ui.Post(() =>
            {
                _model.ShowUserPrompt = false;
                _tcs?.TrySetResult(accepted);
                _tcs = null;
            });
        }

        private Task<bool> Show(string title, string body, bool isConfirm)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ui.Post(() =>
            {
                _tcs?.TrySetResult(false);
                _tcs = tcs;
                _model.UserPromptTitle = title ?? string.Empty;
                _model.UserPromptBody = body ?? string.Empty;
                _model.UserPromptIsConfirm = isConfirm;
                _model.ShowUserPrompt = true;
            });
            return tcs.Task;
        }
    }
}
