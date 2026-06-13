#nullable enable
using System.Windows;
using System.Windows.Controls;

namespace QuickMediaIngest.Controls.Behaviors
{
    public static class PasswordBoxAssist
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxAssist),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssist), new PropertyMetadata(false));

        public static string GetBoundPassword(DependencyObject obj) => (string)obj.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject obj, string value) => obj.SetValue(BoundPasswordProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox box)
            {
                return;
            }

            box.PasswordChanged -= HandlePasswordChanged;
            if (!(bool)box.GetValue(UpdatingPasswordProperty))
            {
                box.Password = e.NewValue as string ?? string.Empty;
            }

            box.PasswordChanged += HandlePasswordChanged;
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox box)
            {
                return;
            }

            box.SetValue(UpdatingPasswordProperty, true);
            SetBoundPassword(box, box.Password);
            box.SetValue(UpdatingPasswordProperty, false);
        }
    }
}
