#nullable enable
using System;

namespace QuickMediaIngest.Core.Prompt
{
    /// <summary>Marshal prompt/UI mutations. Desktop uses Avalonia; tests run inline.</summary>
    public interface IUiDispatcher
    {
        void Post(Action action);
    }

    public sealed class InlineUiDispatcher : IUiDispatcher
    {
        public static InlineUiDispatcher Instance { get; } = new();

        public void Post(Action action) => action();
    }
}
