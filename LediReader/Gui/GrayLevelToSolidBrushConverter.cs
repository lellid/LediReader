using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace LediReader.Gui
{
    /// <summary>
    /// Converts a value between 0 and 255 into a Brush with a gray color with that level.
    /// </summary>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    public class GrayLevelToSolidBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value between 0 and 255 into a Brush with a gray color with that level.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A Brush with a gray color with that level.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v;
            if (value is int intValue)
                v = intValue;
            else if (value is double doubleValue)
                v = doubleValue;
            else
                v = 0;

            v = Math.Max(0, v);
            v = Math.Min(255, v);

            return new SolidColorBrush(Color.FromRgb((byte)v, (byte)v, (byte)v));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
