using Anotar.Log4Net;
using LCU.NET;
using Legendary_Rune_Maker.Pages;
using Legendary_Rune_Maker.Utils;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public abstract class Detector
    {
        protected Container<Actuator.State> State { get; private set; }
        protected ILoL LoL { get; }

        public bool Enabled { get; set; } = true;

        public Detector(ILoL lol)
        {
            this.LoL = lol;
        }

        public async Task Init(Container<Actuator.State> state)
        {
            this.State = state;

            LogTo.Debug("Initializing " + GetType().Name);
            await Init();
            LogTo.Debug("Initialized");
        }

        protected void Notify(string title, string text, NotificationType notificationType)
        {
            MainWindow.ShowNotification(title, text, notificationType);
        }

        protected abstract Task Init();
    }
}
