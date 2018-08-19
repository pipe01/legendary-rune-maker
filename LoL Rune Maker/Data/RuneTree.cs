using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoL_Rune_Maker.Data
{
    public class RuneTree
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("icon")]
        public string IconURL { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slots")]
        public RuneSlot[] Slots { get; set; }
    }
}
