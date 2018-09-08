using System;
using System.Globalization;
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
                return new RelayCommand(_ => 
                {
                    if (!TryInvoke(value, "CloseCustom")) window.Close();
                });
            else if (func == "Minimize")
                return new RelayCommand(_ => window.WindowState = WindowState.Minimized);

            return new RelayCommand(_ => TryInvoke(value, (string)parameter));
        }
        
        private bool TryInvoke(object obj, string funcName)
        {
            var method = obj.GetType().GetMethod(funcName);

            if (method != null)
            {
                method.Invoke(obj, null);
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new WindowCommandConverter();
    }
}
