using System;
using System.Windows;
using System.Windows.Controls;
using Craft.ViewModels.Geometry2D.ScrollFree;

namespace Game.FlappyBird.UI.WPF
{
    public class CustomizedShapeTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(
            object item,
            DependencyObject container)
        {
            var element = container as FrameworkElement;


            if (item is EllipseViewModel)
            {
                return element.FindResource("Bird") as DataTemplate;
            }

            throw new ArgumentException("item doesn't correspond to any DataTemplate");
        }
    }
}
