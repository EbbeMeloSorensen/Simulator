using System;
using System.Windows;
using System.Windows.Controls;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Simulator.ViewModel.ShapeViewModels;
using Simulator.Laboratory.ViewModel.ShapeViewModels;

namespace Simulator.Laboratory.UI.WPF
{
    public class CustomizedShapeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(
            object item,
            DependencyObject container)
        {
            var element = container as FrameworkElement;

            return item switch
            {
                WalkerViewModel => element.FindResource("Walker") as DataTemplate,
                TaggedEllipseViewModel => element.FindResource("TaggedEllipse") as DataTemplate,
                RotatableEllipseViewModel => element.FindResource("RotatableEllipse") as DataTemplate,
                EllipseViewModel => element.FindResource("Ellipse") as DataTemplate,
                RotatableRectangleViewModel => element.FindResource("RotatableRectangle") as DataTemplate,
                RectangleViewModel => element.FindResource("Rectangle") as DataTemplate,
                _ => throw new ArgumentException("item doesn't correspond to any DataTemplate")
            };
        }
    }
}
