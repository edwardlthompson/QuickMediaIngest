#nullable enable
using System;
using Avalonia.Threading;
using QuickMediaIngest.Core.Prompt;

namespace QuickMediaIngest.Desktop;

internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.Post(action);
    }
}
