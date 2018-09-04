using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker
{
    public interface IMainWindow
    {
        bool UploadOnLock { get; }
        int SelectedChampion { get; }
        Position SelectedPosition { get; set; }
        RunePage Page { get; }
        bool ValidPage { get; }
        bool Attached { get; }

        void SafeInvoke(Action act);
        void SetState(GameStates state);
        void ShowNotification(string title, string message = null, NotificationType type = NotificationType.Information);
        Task LoadPageFromDefaultProvider();
        Task SetChampion(Champion champ);
        Task SetChampion(int championId);
    }
}
