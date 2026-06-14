using System;
using System.Windows;
using QuickMediaIngest.Localization;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest.Services
{
    internal static class DeleteAfterImportConfirmHelper
    {
        public static void HandleChecked(MainViewModel vm, ref bool userInitiated, Action? revertCheck)
        {
            if (!userInitiated)
            {
                return;
            }

            userInitiated = false;

            if (vm.DeleteAfterImportPromptDismissed)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                AppLocalizer.Get("Msg_DeleteAfterImport_ConfirmBody"),
                AppLocalizer.Get("Msg_DeleteAfterImport_ConfirmTitle"),
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                vm.DeleteAfterImportPromptDismissed = true;
                return;
            }

            revertCheck?.Invoke();
        }
    }
}
