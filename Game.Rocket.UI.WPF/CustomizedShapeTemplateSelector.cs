using System;
using System.Windows;
using System.Windows.Controls;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Game.Rocket.ViewModel.ShapeViewModels;

namespace Game.Rocket.UI.WPF
{
    public class CustomizedShapeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(
            object item,
            DependencyObject container)
        {
            var element = container as FrameworkElement;

            switch (item)
            {
                case RocketViewModel _:
                {
                    return element.FindResource("Rocket") as DataTemplate;
                }
                case MeteorViewModel _:
                {
                    return element.FindResource("Meteor") as DataTemplate;
                }
                case FragmentViewModel _:
                {
                        return element.FindResource("Fragment") as DataTemplate;
                }
                case ProjectileViewModel _:
                {
                    return element.FindResource("Projectile") as DataTemplate;
                }
                case RectangleViewModel _:
                {
                    return element.FindResource("Wall") as DataTemplate;
                }
                default:
                {
                    throw new ArgumentException("item doesn't correspond to any DataTemplate");
                }
            }
        }
    }
}
