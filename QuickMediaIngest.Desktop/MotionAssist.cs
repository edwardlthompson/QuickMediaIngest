#nullable enable
using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Motion;

namespace QuickMediaIngest.Desktop;

public static class MotionAssist
{
    public static readonly AttachedProperty<bool> FadeOnVisibleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FadeOnVisible", typeof(MotionAssist));

    public static readonly AttachedProperty<bool> PressScaleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("PressScale", typeof(MotionAssist));

    public static readonly AttachedProperty<bool> PulseWhileEmptyProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("PulseWhileEmpty", typeof(MotionAssist));

    static MotionAssist()
    {
        FadeOnVisibleProperty.Changed.AddClassHandler<Control>(OnFadeChanged);
        PressScaleProperty.Changed.AddClassHandler<Control>(OnPressChanged);
        PulseWhileEmptyProperty.Changed.AddClassHandler<Control>(OnPulseChanged);
    }

    public static void SetFadeOnVisible(Control e, bool v) => e.SetValue(FadeOnVisibleProperty, v);

    public static bool GetFadeOnVisible(Control e) => e.GetValue(FadeOnVisibleProperty);

    public static void SetPressScale(Control e, bool v) => e.SetValue(PressScaleProperty, v);

    public static bool GetPressScale(Control e) => e.GetValue(PressScaleProperty);

    public static void SetPulseWhileEmpty(Control e, bool v) => e.SetValue(PulseWhileEmptyProperty, v);

    public static bool GetPulseWhileEmpty(Control e) => e.GetValue(PulseWhileEmptyProperty);

    private static bool Reduced(Control control) =>
        control.DataContext is IngestBenchAppModel model && model.ReducedMotion
        || TopLevel.GetTopLevel(control)?.DataContext is IngestBenchAppModel root && root.ReducedMotion;

    private static void OnFadeChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.PropertyChanged -= FadeVisible;
        if (Equals(e.NewValue, true))
        {
            control.PropertyChanged += FadeVisible;
        }
    }

    private static async void FadeVisible(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Control control || e.Property != Visual.IsVisibleProperty || !control.IsVisible)
        {
            return;
        }

        TimeSpan duration = MotionTimings.Duration(MotionTimings.OverlayEnterMs, Reduced(control));
        if (duration == TimeSpan.Zero)
        {
            control.Opacity = 1;
            return;
        }

        control.Opacity = 0;
        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
            },
        };
        await animation.RunAsync(control);
    }

    private static void OnPressChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.PointerPressed -= Press;
        control.PointerReleased -= Release;
        if (Equals(e.NewValue, true))
        {
            control.PointerPressed += Press;
            control.PointerReleased += Release;
        }
    }

    private static void Press(object? sender, PointerPressedEventArgs e) => Scale(sender as Control, MotionTimings.ImportPressScale);

    private static void Release(object? sender, PointerReleasedEventArgs e) => Scale(sender as Control, 1d);

    private static void Scale(Control? control, double scale)
    {
        if (control is null || Reduced(control))
        {
            return;
        }

        control.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        control.RenderTransform = new ScaleTransform(scale, scale);
    }

    private static void OnPulseChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.AttachedToVisualTree -= PulseStart;
        if (Equals(e.NewValue, true))
        {
            control.AttachedToVisualTree += PulseStart;
        }
    }

    private static async void PulseStart(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control || Reduced(control))
        {
            return;
        }

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(MotionTimings.SkeletonPulseMs),
            IterationCount = new IterationCount(4),
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0.35d) } },
                new KeyFrame { Cue = new Cue(0.5d), Setters = { new Setter(Visual.OpacityProperty, 0.85d) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 0.35d) } },
            },
        };
        await animation.RunAsync(control);
    }
}
