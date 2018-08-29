using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public class SummonerSpell
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int SummonerLevel { get; set; }
        public string Image { get; set; }
    }
}
