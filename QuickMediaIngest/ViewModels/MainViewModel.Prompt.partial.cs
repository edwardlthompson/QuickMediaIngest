#nullable enable
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core.Prompt;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : IUserPrompt
    {
        private IUserPrompt _userPrompt = SilentUserPrompt.Instance;
        private TaskCompletionSource<bool>? _promptTcs;

        [ObservableProperty] private bool showUserPromptDialog;
        [ObservableProperty] private string userPromptTitle = string.Empty;
        [ObservableProperty] private string userPromptBody = string.Empty;
        [ObservableProperty] private bool userPromptIsConfirm;

        public async Task NotifyAsync(string title, string body)
        {
            _ = await ShowOverlayAsync(title, body, isConfirm: false).ConfigureAwait(true);
        }

        public Task<bool> ConfirmAsync(string title, string body)
            => ShowOverlayAsync(title, body, isConfirm: true);

        internal void NotifyUser(string title, string body)
            => _ = _userPrompt.NotifyAsync(title ?? string.Empty, body ?? string.Empty);

        private Task<bool> ShowOverlayAsync(string title, string body, bool isConfirm)
        {
            title ??= string.Empty;
            body ??= string.Empty;
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                return dispatcher.InvokeAsync(() => ShowOverlayCore(title, body, isConfirm)).Task.Unwrap();
            }

            return ShowOverlayCore(title, body, isConfirm);
        }

        private Task<bool> ShowOverlayCore(string title, string body, bool isConfirm)
        {
            _promptTcs?.TrySetResult(false);
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _promptTcs = tcs;
            UserPromptTitle = title;
            UserPromptBody = body;
            UserPromptIsConfirm = isConfirm;
            ShowUserPromptDialog = true;
            return tcs.Task;
        }

        [RelayCommand]
        private void AcceptUserPrompt()
        {
            ShowUserPromptDialog = false;
            _promptTcs?.TrySetResult(true);
            _promptTcs = null;
        }

        [RelayCommand]
        private void CancelUserPrompt()
        {
            ShowUserPromptDialog = false;
            _promptTcs?.TrySetResult(false);
            _promptTcs = null;
        }
    }
}
