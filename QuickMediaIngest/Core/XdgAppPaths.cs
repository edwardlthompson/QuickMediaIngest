#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>XDG config home / QuickMediaIngest (Linux Mint .deb).</summary>
    public sealed class XdgAppPaths : IAppPaths
    {
        public string AppDataRoot
        {
            get
            {
                string? xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                string configHome = string.IsNullOrWhiteSpace(xdg)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
                    : xdg.Trim();
                return Path.Combine(configHome, "QuickMediaIngest");
            }
        }
    }
}
