using Legendary_Rune_Maker.Data;
using System.ComponentModel;

namespace Legendary_Rune_Maker.Overlay
{
    public class Enemy : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Champion Champion { get; set; }
        public Champion[] GoodPicks { get; set; }
        public Champion[] BadPicks { get; set; }
    }
}
