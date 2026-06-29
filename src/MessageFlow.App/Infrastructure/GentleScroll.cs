using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MessageFlow.App.Infrastructure;

public static class GentleScroll
{
    public static readonly DependencyProperty WheelStepProperty = DependencyProperty.RegisterAttached(
        "WheelStep",
        typeof(double),
        typeof(GentleScroll),
        new PropertyMetadata(0d, OnWheelStepChanged));

    public static void SetWheelStep(DependencyObject element, double value)
    {
        element.SetValue(WheelStepProperty, value);
    }

    public static double GetWheelStep(DependencyObject element)
    {
        return (double)element.GetValue(WheelStepProperty);
    }

    private static void OnWheelStepChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not UIElement element)
        {
            return;
        }

        element.PreviewMouseWheel -= Element_PreviewMouseWheel;
        if ((double)e.NewValue > 0)
        {
            element.PreviewMouseWheel += Element_PreviewMouseWheel;
        }
    }

    private static void Element_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject)
        {
            return;
        }

        var scrollViewer = dependencyObject as ScrollViewer ?? FindDescendantScrollViewer(dependencyObject);
        if (scrollViewer is null)
        {
            return;
        }

        var step = GetWheelStep(dependencyObject);
        if (step <= 0)
        {
            step = GetWheelStep(scrollViewer);
        }

        if (step <= 0)
        {
            return;
        }

        var direction = e.Delta > 0 ? -1 : 1;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + direction * step);
        e.Handled = true;
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject parent)
    {
        var children = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < children; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            var descendant = FindDescendantScrollViewer(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
