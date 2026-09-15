#nullable enable
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuickMediaIngest.Core.Motion;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Controls.Behaviors
{
    public static class MotionAssist
    {
        public static readonly DependencyProperty FadeOnVisibleProperty =
            DependencyProperty.RegisterAttached(
                "FadeOnVisible",
                typeof(bool),
                typeof(MotionAssist),
                new PropertyMetadata(false, OnFadeOnVisibleChanged));

        public static readonly DependencyProperty PressScaleProperty =
            DependencyProperty.RegisterAttached(
                "PressScale",
                typeof(bool),
                typeof(MotionAssist),
                new PropertyMetadata(false, OnPressScaleChanged));

        public static bool GetFadeOnVisible(DependencyObject obj) => (bool)obj.GetValue(FadeOnVisibleProperty);

        public static void SetFadeOnVisible(DependencyObject obj, bool value) => obj.SetValue(FadeOnVisibleProperty, value);

        public static bool GetPressScale(DependencyObject obj) => (bool)obj.GetValue(PressScaleProperty);

        public static void SetPressScale(DependencyObject obj, bool value) => obj.SetValue(PressScaleProperty, value);

        private static void OnFadeOnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
            {
                return;
            }

            element.IsVisibleChanged -= HandleVisible;
            if (Equals(e.NewValue, true))
            {
                element.IsVisibleChanged += HandleVisible;
            }
        }

        private static void HandleVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not FrameworkElement element || !element.IsVisible)
            {
                return;
            }

            TimeSpan duration = MotionTimings.Duration(
                MotionTimings.OverlayEnterMs,
                AccessibilityPreferencesDetector.IsReducedMotionPreferred());
            if (duration == TimeSpan.Zero)
            {
                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.Opacity = 1;
                return;
            }

            element.Opacity = 0;
            element.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } },
                HandoffBehavior.SnapshotAndReplace);
        }

        private static void OnPressScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
            {
                return;
            }

            element.PreviewMouseLeftButtonDown -= HandlePress;
            element.PreviewMouseLeftButtonUp -= HandleRelease;
            element.MouseLeave -= HandleRelease;
            if (Equals(e.NewValue, true))
            {
                if (element.RenderTransform is not ScaleTransform)
                {
                    element.RenderTransform = new ScaleTransform(1, 1);
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                element.PreviewMouseLeftButtonDown += HandlePress;
                element.PreviewMouseLeftButtonUp += HandleRelease;
                element.MouseLeave += HandleRelease;
            }
        }

        private static void HandlePress(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            ScaleTo(sender, MotionTimings.ImportPressScale);

        private static void HandleRelease(object sender, RoutedEventArgs e) => ScaleTo(sender, 1);

        private static void ScaleTo(object sender, double scale)
        {
            if (sender is not FrameworkElement element || element.RenderTransform is not ScaleTransform transform)
            {
                return;
            }

            TimeSpan duration = MotionTimings.Duration(
                MotionTimings.ImportPressMs,
                AccessibilityPreferencesDetector.IsReducedMotionPreferred());
            if (duration == TimeSpan.Zero)
            {
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                transform.ScaleX = scale;
                transform.ScaleY = scale;
                return;
            }

            var anim = new DoubleAnimation(scale, duration);
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, anim, HandoffBehavior.SnapshotAndReplace);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, anim, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
