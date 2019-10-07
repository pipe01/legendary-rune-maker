using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Legendary_Rune_Maker.Utils
{
    public class DebugDataBindingConverter : MarkupExtension, IValueConverter
    {
        private static readonly bool InDesigner = DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!InDesigner)
                Debugger.Break();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!InDesigner)
                Debugger.Break();
            return value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new DebugDataBindingConverter();
    }
}
