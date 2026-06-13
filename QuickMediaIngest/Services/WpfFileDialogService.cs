#nullable enable
using System.IO;
using Microsoft.Win32;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.Services
{
    public sealed class WpfFileDialogService : IFileDialogService
    {
        public bool TryPickFolder(string? initialDirectory, string title, out string selectedFolder)
        {
            selectedFolder = string.Empty;
            string initial = initialDirectory ?? string.Empty;
            if (string.IsNullOrWhiteSpace(initial) || !Directory.Exists(initial))
            {
                initial = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
            }

            var dialog = new OpenFolderDialog
            {
                InitialDirectory = initial,
                Title = title,
            };

            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                return false;
            }

            selectedFolder = dialog.FolderName;
            return true;
        }
    }
}
