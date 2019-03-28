using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Legendary_Rune_Maker.Data
{
    public class RuneBook : IList<RunePage>
    {
        public static RuneBook Instance { get; } = new RuneBook();

        private const string PagesFileName = "pages.json";

        public RunePage this[int index]
        {
            get => this.Inner[index];
            set
            {
                this.Inner[index] = value;
                Save();
            }
        }

        public int Count => this.Inner.Count;

        public bool IsReadOnly => ((IList<RunePage>)this.Inner).IsReadOnly;

        private readonly List<RunePage> Inner;

        public RuneBook()
        {
            if (File.Exists(PagesFileName))
            {
                Inner = JsonConvert.DeserializeObject<List<RunePage>>(File.ReadAllText(PagesFileName));
            }
            else
            {
                Inner = new List<RunePage>();
                Save();
            }
        }

        public RunePage Get(int championId, Position position, bool getFill = true)
        {
            return Inner.Find(o => o.ChampionID == championId && o.Position == position)
                ?? (getFill ? Inner.Find(o => o.ChampionID == championId && o.Position == Position.Fill) : null);
        }

        public void Remove(int championId, Position position)
        {
            var page = Get(championId, position, false);
            if (page != null)
                Remove(page);
        }

        public void Save()
        {
            File.WriteAllText(PagesFileName, JsonConvert.SerializeObject(this));
        }

        public void Add(RunePage item)
        {
            this.Inner.Add(item);
            Save();
        }

        public void Clear()
        {
            this.Inner.Clear();
            Save();
        }

        public bool Contains(RunePage item)
        {
            return this.Inner.Contains(item);
        }

        public void CopyTo(RunePage[] array, int arrayIndex)
        {
            this.Inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<RunePage> GetEnumerator()
        {
            return ((IList<RunePage>)this.Inner).GetEnumerator();
        }

        public int IndexOf(RunePage item)
        {
            return this.Inner.IndexOf(item);
        }

        public void Insert(int index, RunePage item)
        {
            this.Inner.Insert(index, item);
            Save();
        }

        public bool Remove(RunePage item)
        {
            bool b = this.Inner.Remove(item);
            Save();
            return b;
        }

        public void RemoveAt(int index)
        {
            this.Inner.RemoveAt(index);
            Save();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<RunePage>)this.Inner).GetEnumerator();
        }
    }
}
