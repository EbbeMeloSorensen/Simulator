using System;
using System.Windows;
using System.Windows.Controls;
using Craft.ViewModels.Geometry2D.ScrollFree;
using Game.Zelda.ViewModel.ShapeViewModels;

namespace Game.Zelda.UI.WPF
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
                case ZeldaViewModel _:
                    {
                        return element.FindResource("Zelda") as DataTemplate;
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
