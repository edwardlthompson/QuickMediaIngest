#nullable enable
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Prompt
{
    /// <summary>Auto-confirm / no-op prompt for tests and headless callers.</summary>
    public sealed class SilentUserPrompt : IUserPrompt
    {
        public static SilentUserPrompt Instance { get; } = new();

        public Task NotifyAsync(string title, string body)
        {
            _ = title;
            _ = body;
            return Task.CompletedTask;
        }

        public Task<bool> ConfirmAsync(string title, string body)
        {
            _ = title;
            _ = body;
            return Task.FromResult(true);
        }
    }
}
