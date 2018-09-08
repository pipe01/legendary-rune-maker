using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Legendary_Rune_Maker.Utils
{
    public class UppercaseConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string)?.ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new UppercaseConverter();
    }
}
