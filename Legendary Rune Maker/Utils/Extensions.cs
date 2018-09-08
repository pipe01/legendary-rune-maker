using Legendary_Rune_Maker.Data;
using System.Windows;

namespace Legendary_Rune_Maker.Utils
{
    public static class Extensions
    {
        public static Position ToPosition(this string str)
        {
            switch (str)
            {
                case "TOP":
                    return Position.Top;
                case "JUNGLE":
                    return Position.Jungle;
                case "MIDDLE":
                    return Position.Mid;
                case "UTILITY":
                    return Position.Support;
                case "BOTTOM":
                    return Position.Bottom;
                default:
                    return Position.Fill;
            }
        }

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
