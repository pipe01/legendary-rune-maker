using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Data
{
    public class RuneSlot
    {
        [JsonProperty("runes")]
        public Rune[] Runes { get; set; }
    }
}
