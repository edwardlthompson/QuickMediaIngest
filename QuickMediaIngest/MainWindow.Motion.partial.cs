#nullable enable
using System.Windows;
using System.Windows.Media.Animation;
using QuickMediaIngest.Core.Motion;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty SidebarWidthProxyProperty =
            DependencyProperty.Register(
                nameof(SidebarWidthProxy),
                typeof(double),
                typeof(MainWindow),
                new PropertyMetadata(MotionTimings.SidebarExpandedPx, OnSidebarWidthProxyChanged));

        public double SidebarWidthProxy
        {
            get => (double)GetValue(SidebarWidthProxyProperty);
            set => SetValue(SidebarWidthProxyProperty, value);
        }

        private static void OnSidebarWidthProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MainWindow window && window.SidebarColumn != null)
            {
                window.SidebarColumn.Width = new GridLength((double)e.NewValue);
            }
        }

        private void AnimateSidebarWidth(bool isCollapsed)
        {
            double to = isCollapsed ? MotionTimings.SidebarCollapsedPx : MotionTimings.SidebarExpandedPx;
            TimeSpan duration = MotionTimings.Duration(
                MotionTimings.SidebarMs,
                AccessibilityPreferencesDetector.IsReducedMotionPreferred());
            if (duration == TimeSpan.Zero)
            {
                BeginAnimation(SidebarWidthProxyProperty, null);
                SidebarWidthProxy = to;
                return;
            }

            BeginAnimation(
                SidebarWidthProxyProperty,
                new DoubleAnimation(SidebarWidthProxy, to, duration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                },
                HandoffBehavior.SnapshotAndReplace);
        }

        internal void FlashImportSuccess()
        {
            if (StatusFlashBar == null)
            {
                return;
            }

            TimeSpan duration = MotionTimings.Duration(
                MotionTimings.SuccessFlashMs,
                AccessibilityPreferencesDetector.IsReducedMotionPreferred());
            if (duration == TimeSpan.Zero)
            {
                return;
            }

            StatusFlashBar.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation(0.35, 0, duration),
                HandoffBehavior.SnapshotAndReplace);
        }

        private void ViewModel_ImportCompletedFlash(object? sender, EventArgs e) => FlashImportSuccess();
    }
}
