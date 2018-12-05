using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Utils
{
    public class Container<T> where T : class
    {
        public T Value { get; set; }
        public bool HasValue => Value != null;

        public Container()
        {
        }

        public Container(T value)
        {
            this.Value = value;
        }

        public static implicit operator T(Container<T> c) => c.Value;
    }
}
