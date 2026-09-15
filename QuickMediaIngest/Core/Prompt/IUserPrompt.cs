#nullable enable
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Prompt
{
    /// <summary>Non-destructive user prompt. Both heads implement this; Core stays WPF-free.</summary>
    public interface IUserPrompt
    {
        Task NotifyAsync(string title, string body);

        Task<bool> ConfirmAsync(string title, string body);
    }
}
