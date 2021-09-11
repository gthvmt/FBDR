using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace FBDR.Converter
{
    class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (Models.Color)value;
            if (color is null)
            {
                return new SolidColorBrush(Colors.White);
            }
            return new SolidColorBrush(Color.FromArgb(
                color.Alpha, color.Red, color.Green, color.Blue));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
