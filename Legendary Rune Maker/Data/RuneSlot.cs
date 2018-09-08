using Newtonsoft.Json;

namespace Legendary_Rune_Maker.Data
{
    public class RuneSlot
    {
        [JsonProperty("runes")]
        public Rune[] Runes { get; set; }
    }
}
