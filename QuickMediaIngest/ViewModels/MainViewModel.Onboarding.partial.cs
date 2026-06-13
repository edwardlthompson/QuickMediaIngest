using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Media;
using System.Net;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Localization;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest;


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        private static string ExtractFtpFolderPath(string sourcePath)
        {
            string normalized = NormalizeFtpPath(sourcePath);
            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                return "/";
            }

            return normalized.Substring(0, lastSlash);
        }

        private static string? GetParentFtpPath(string path)
        {
            string normalized = NormalizeFtpPath(path);
            if (string.Equals(normalized, "/", StringComparison.Ordinal))
            {
                return null;
            }

            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                return "/";
            }

            return normalized.Substring(0, lastSlash);
        }

        private static string ResolveLocalScanPath(string localRoot, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                return localRoot;
            }

            string trimmed = candidatePath.Trim();
            if (Path.IsPathRooted(trimmed))
            {
                return trimmed;
            }

            return Path.Combine(localRoot, trimmed.TrimStart('\\', '/'));
        }

        public bool IsFirstRun { get; set; } = true;

        public void ShowOnboarding(Window owner, bool markNotFirstRun = true)
        {
            var dialog = new OnboardingDialog { Owner = owner };
            if (dialog.ShowDialog() == true && markNotFirstRun)
            {
                IsFirstRun = false;
                SaveConfig();
            }
        }
    }
}
