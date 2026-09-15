#nullable enable
using System.Threading.Tasks;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class UserPromptTests
{
    [Fact]
    public async Task Silent_Notify_Completes()
    {
        await SilentUserPrompt.Instance.NotifyAsync("", "");
    }

    [Fact]
    public async Task Silent_Confirm_IsTrue()
    {
        Assert.True(await SilentUserPrompt.Instance.ConfirmAsync("", ""));
    }

    [Fact]
    public async Task Recording_CapturesNotifyAndConfirm()
    {
        var prompt = new RecordingUserPrompt { NextConfirm = false };
        await prompt.NotifyAsync("n", "nb");
        Assert.False(await prompt.ConfirmAsync("c", "cb"));
        Assert.Single(prompt.Notifies);
        Assert.Equal(("n", "nb"), prompt.Notifies[0]);
        Assert.Equal(("c", "cb"), prompt.Confirms[0]);
    }
}

public sealed class RecordingUserPrompt : IUserPrompt
{
    public System.Collections.Generic.List<(string Title, string Body)> Notifies { get; } = new();
    public System.Collections.Generic.List<(string Title, string Body)> Confirms { get; } = new();
    public bool NextConfirm { get; set; } = true;

    public Task NotifyAsync(string title, string body)
    {
        Notifies.Add((title, body));
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string body)
    {
        Confirms.Add((title, body));
        return Task.FromResult(NextConfirm);
    }
}
