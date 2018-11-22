using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Pages;
using Ninject;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Legendary_Rune_Maker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly IKernel Container;

        static App()
        {
            Container = new StandardKernel();
        }

        public App()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Config.Default.Culture;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoL.BindNinject(Container);
            //ILoL lol = LoL.CreateNew();
            
            //Container.Bind<ILoL>().ToMethod(o =>  lol);
            //Container.Bind<ILeagueClient>().ToConstant(lol.Client);
            //Container.Bind<ILeagueSocket>().ToConstant(lol.Socket);
            
            Container.Bind<Actuator>().ToSelf();
            Container.Bind<ChampSelectDetector>().ToSelf();
            Container.Bind<LoginDetector>().ToSelf();
            Container.Bind<ReadyCheckDetector>().ToSelf();

            Container.Bind<MainPage>().ToConstructor(o => new MainPage(o.Inject<ILoL>(), o.Inject<ChampSelectDetector>(),
                o.Inject<LoginDetector>(), o.Inject<ReadyCheckDetector>(),
                o.Context.Parameters.First().GetValue(o.Context, o.Context.Request.Target) as MainWindow));

            Current.MainWindow = Container.Get<LoadingWindow>();
            Current.MainWindow.Show();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == e.Source)
                (sender as Window)?.DragMove();
        }
    }
}
