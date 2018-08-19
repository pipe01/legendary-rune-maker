using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoL_Rune_Maker.Data
{
    public class Champion
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string ImageURL { get; set; }
    }
}
