#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickMediaIngest.Core.Nav
{
    public enum OverlayId
    {
        Home = 0,
        Settings = 1,
        About = 2,
        Feedback = 3,
        History = 4,
        ScanExclusions = 5
    }

    /// <summary>Golden Path overlay stack. Home is empty; pop at home is a no-op.</summary>
    public sealed class OverlayNav
    {
        private readonly List<OverlayId> _stack = new();

        public IReadOnlyList<OverlayId> Stack => _stack;

        public OverlayId Peek => _stack.Count == 0 ? OverlayId.Home : _stack[^1];

        public bool CanPop => _stack.Count > 0;

        public bool IsVisible(OverlayId id) => Peek == id;

        public void Push(OverlayId id)
        {
            if (id == OverlayId.Home)
            {
                return;
            }

            if (_stack.Count > 0 && _stack[^1] == id)
            {
                return;
            }

            _stack.Add(id);
        }

        public OverlayId Pop()
        {
            if (_stack.Count == 0)
            {
                return OverlayId.Home;
            }

            OverlayId top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            return top;
        }

        /// <summary>Settings → App info becomes About (home → about), matching Golden Path smoke.</summary>
        public void OpenAboutFromSettings()
        {
            if (Peek == OverlayId.Settings)
            {
                Pop();
            }

            Push(OverlayId.About);
        }

        public void Reset() => _stack.Clear();

        public string Serialize() => string.Join(',', _stack.Select(static id => id.ToString()));

        public static OverlayNav Deserialize(string? raw)
        {
            var nav = new OverlayNav();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return nav;
            }

            foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse(part, ignoreCase: true, out OverlayId id) && id != OverlayId.Home)
                {
                    nav.Push(id);
                }
            }

            return nav;
        }
    }
}
