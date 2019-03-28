using Legendary_Rune_Maker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCU.NET.Extras.Utils
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

        public static IDictionary<TValue, TKey> Invert<TKey, TValue>(this IDictionary<TKey, TValue> dic)
            => dic.ToDictionary(o => o.Value, o => o.Key);
    }
}
