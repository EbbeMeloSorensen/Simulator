using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ApplicationState = Craft.DataStructures.Graph.State;

namespace Game.Rocket.UI.WPF.ValueConverters
{
    public class ApplicationStateToVisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (!(value is ApplicationState))
            {
                return Visibility.Hidden;
            }

            var valueAsApplicationState = (ApplicationState)value;
            var parameterAsString = parameter as string;

            return valueAsApplicationState != null && valueAsApplicationState.Name.Contains(parameterAsString)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
