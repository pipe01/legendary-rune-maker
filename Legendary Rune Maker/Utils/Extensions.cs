using Legendary_Rune_Maker.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public static IDictionary<TValue, TKey> Invert<TKey, TValue>(this IDictionary<TKey, TValue> dic)
            => dic.ToDictionary(o => o.Value, o => o.Key);

        public static T ArrayLast<T>(this T[] arr) => arr[arr.Length - 1];

        public static async Task<T[]> AwaitAll<T>(this Task<T>[] tasks)
        {
            var ret = new T[tasks.Length];

            for (int i = 0; i < tasks.Length; i++)
            {
                ret[i] = await tasks[i];
            }

            return ret;
        }
    }
}
