using System.Linq;
using CommunityToolkit.Mvvm.Input;

namespace QuickMediaIngest.ViewModels
{
    /// <summary>Naming-token UI commands (partial MainViewModel).</summary>
    public partial class MainViewModel
    {
        [RelayCommand]
        private void RemoveToken(TokenItem? item)
        {
            if (item == null) return;
            if (SelectedTokens.Contains(item))
            {
                SelectedTokens.Remove(item);
                UpdateNamingFromTokens();

                var value = item.Value;
                if (!string.IsNullOrEmpty(value) &&
                    value.StartsWith("[") &&
                    value.EndsWith("]") &&
                    !AvailableTokens.Contains(value))
                {
                    AvailableTokens.Add(value);
                }
            }
        }

        [RelayCommand]
        private void InsertToken(TokenInsertPayload? payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.Token)) return;

            int insertIndex = payload.Index;
            if (insertIndex < 0 || insertIndex > SelectedTokens.Count) insertIndex = SelectedTokens.Count;

            if (payload.FromSelected)
            {
                int existingIndex = SelectedTokens.Select((item, index) => new { item, index })
                    .FirstOrDefault(x => x.item.Value == payload.Token)?.index ?? -1;

                if (existingIndex >= 0)
                {
                    var movingItem = SelectedTokens[existingIndex];
                    SelectedTokens.RemoveAt(existingIndex);
                    if (existingIndex < insertIndex) insertIndex--;
                    if (insertIndex < 0) insertIndex = 0;
                    if (insertIndex > SelectedTokens.Count) insertIndex = SelectedTokens.Count;
                    SelectedTokens.Insert(insertIndex, movingItem);
                    UpdateNamingFromTokens();
                }

                return;
            }

            if (payload.Token.StartsWith("[") && payload.Token.EndsWith("]") && SelectedTokens.Any(t => t.Value == payload.Token))
            {
                return;
            }

            SelectedTokens.Insert(insertIndex, new TokenItem { Value = payload.Token });
            UpdateNamingFromTokens();

            if (payload.Token.StartsWith("[") && payload.Token.EndsWith("]") && AvailableTokens.Contains(payload.Token))
            {
                AvailableTokens.Remove(payload.Token);
            }
        }
    }
}
