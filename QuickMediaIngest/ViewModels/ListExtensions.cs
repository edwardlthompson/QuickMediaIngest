using System.Collections.Generic;

namespace QuickMediaIngest.ViewModels
{
    public static class ListExtensions
    {
        public static void Move<T>(this IList<T> list, int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex) return;
            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }
    }
}
