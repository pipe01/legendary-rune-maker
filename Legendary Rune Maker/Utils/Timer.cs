using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Utils
{
    public static class Timer
    {
        public static async Task<(TimeSpan Time, T Result)> Time<T>(Func<Task<T>> func)
        {
            var sw = Stopwatch.StartNew();
            var result = await func();

            return (sw.Elapsed, result);
        }
    }
}
