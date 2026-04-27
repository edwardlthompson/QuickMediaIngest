#nullable enable
using System;
using System.Windows.Markup;

namespace QuickMediaIngest.Localization
{
    /// <summary>
    /// XAML: <c>Title="{loc:Loc Main_WindowTitle}"</c> with xmlns <c>xmlns:loc="clr-namespace:QuickMediaIngest.Localization"</c>.
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public sealed class Loc : MarkupExtension
    {
        public Loc() { }

        public Loc(string key) => Key = key;

        /// <summary>Resource key in Strings.resx.</summary>
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            AppLocalizer.Get(Key);
    }
}
