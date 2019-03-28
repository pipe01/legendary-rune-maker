using Legendary_Rune_Maker.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Legendary_Rune_Maker.Utils
{
    public static class Extensions
    {
        public static bool? ShowDialog(this Window window, Window owner)
        {
            window.Owner = owner;
            return window.ShowDialog();
        }

        public static void Show(this Window window, Window owner)
        {
            window.Owner = owner;
            window.Show();
        }

        public static string FormatStr(this string str, params object[] args) => string.Format(str, args: args);

    }
}
