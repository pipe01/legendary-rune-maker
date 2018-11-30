using Anotar.Log4Net;
using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Pages;
using Newtonsoft.Json;
using Ninject;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
            LogTo.Info($"Starting LRM {LRM.GitCommit}@{LRM.GitBranch}");

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Config.Default.Culture;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!Debugger.IsAttached)
            {
                var exception = e.ExceptionObject as Exception;

                LogTo.FatalException("Unhandled exception", exception);

                var result = MessageBox.Show($"{exception.GetType().FullName}: {exception.Message}\n" +
                    "Create minidump? You can use this to report this issue to me.", "Unhandled exception",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    string date = DateTime.UtcNow.ToString("HHmmss");
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"dump_{date}.mdmp");

                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
                    {
                        MiniDump.Write(fs.SafeFileHandle, MiniDump.Option.Normal | MiniDump.Option.WithIndirectlyReferencedMemory | MiniDump.Option.WithDataSegs, MiniDump.ExceptionInfo.Present);

                        using (var file = File.OpenWrite(path + ".zip"))
                        using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                        using (var entry = zip.CreateEntry(Path.GetFileName(path)).Open())
                        {
                            fs.CopyTo(entry);
                        }
                    }

                    //File.Delete(path);

                    Process.Start("explorer.exe", $"/select, \"{path}.zip\"");
                }

                if (e.IsTerminating)
                {
                    Legendary_Rune_Maker.MainWindow.DisposeTaskbar();
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoL.BindNinject(Container);
            
            Container.Bind<Actuator>().ToSelf().InSingletonScope();
            Container.Bind<LoginDetector>().ToSelf();

            Container.Bind<MainWindow>().ToSelf().InSingletonScope();
            //Container.Bind<MainPage>().ToConstructor(o => new MainPage(o.Inject<ILoL>(), o.Inject<ChampSelectDetector>(),
            //    o.Inject<LoginDetector>(), o.Inject<ReadyCheckDetector>(),
            //    o.Context.Parameters.First().GetValue(o.Context, o.Context.Request.Target) as MainWindow));

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
