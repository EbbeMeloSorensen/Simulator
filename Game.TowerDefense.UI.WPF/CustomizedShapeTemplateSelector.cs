using Craft.ViewModels.Geometry2D.ScrollFree;
using Game.TowerDefense.ViewModel.ShapeViewModels;
using Simulator.ViewModel.ShapeViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Game.TowerDefense.UI.WPF
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
                TaggedEllipseViewModel => element.FindResource("TaggedEllipse") as DataTemplate,
                CannonViewModel => element.FindResource("Cannon") as DataTemplate,
                RotatableEllipseViewModel => element.FindResource("RotatableEllipse") as DataTemplate,
                ProjectileViewModel => element.FindResource("Projectile") as DataTemplate,
                EllipseViewModel => element.FindResource("Ellipse") as DataTemplate,
                RotatableRectangleViewModel => element.FindResource("RotatableRectangle") as DataTemplate,
                RectangleViewModel => element.FindResource("Rectangle") as DataTemplate,
                _ => throw new ArgumentException("item doesn't correspond to any DataTemplate")
            };
        }
    }
}
