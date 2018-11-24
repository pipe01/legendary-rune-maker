using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public struct StatRune : IEquatable<StatRune>
    {
        public int ID { get; }
        public string Description { get; }

        public StatRune(int id, string description)
        {
            this.ID = id;
            this.Description = description;
        }

        public bool Equals(StatRune other) => this.ID == other.ID;

        public override string ToString()
        {
            return this.Description;
        }

        public override bool Equals(object obj)
        {
            return obj is StatRune r && this.Equals(r);
        }

        public override int GetHashCode() => ID * 17;

        public static bool operator ==(StatRune a, StatRune b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(StatRune a, StatRune b)
        {
            return !a.Equals(b);
        }
    }
}
