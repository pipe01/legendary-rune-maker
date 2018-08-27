using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Legendary_Rune_Maker.Utils
{
    class WindowCommandConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Window window))
                return null;

            string func = parameter as string;

            if (func == "Close")
                return new RelayCommand(_ => window.Close());
            else if (func == "Minimize")
                return new RelayCommand(_ => window.WindowState = WindowState.Minimized);

            return new RelayCommand(_ => value.GetType().GetMethod((string)parameter).Invoke(value, null));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new WindowCommandConverter();
    }
}
