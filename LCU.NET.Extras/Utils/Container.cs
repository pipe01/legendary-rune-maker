using System;

namespace Legendary_Rune_Maker.Utils
{
    public class Container<T> where T : class
    {
        private static Random random = new Random();

        public T Value { get; set; }
        public bool HasValue => Value != null;
        public int Test = random.Next(0, 1000);

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
