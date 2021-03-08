using Anotar.Log4Net;
using LCU.NET;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Utils;
using Legendary_Rune_Maker.Windows;
using Ninject;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
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
            LogTo.Info($"Starting LRM {LRM.GitCommit}@{LRM.GitBranch}");

            Container = new StandardKernel();
        }

        public App()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Config.Current.Culture;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!Debugger.IsAttached || true)
            {
                var exception = e.ExceptionObject as Exception;

                LogTo.FatalException("Unhandled exception", exception);

                var result = MessageBox.Show($"{exception.GetType().FullName}: {exception.Message}\n" +
                    "This error will be automatically reported. Create minidump? You can use it to manually report the issue to me.", "Unhandled exception",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Error);

                Report();

                if (result == MessageBoxResult.Yes)
                {
                    string date = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dumps", $"dump_{date}.mdmp");
                    string folder = Path.GetDirectoryName(path);

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
                    {
                        MiniDump.Write(fs.SafeFileHandle, MiniDump.Option.Normal | MiniDump.Option.WithIndirectlyReferencedMemory | MiniDump.Option.WithDataSegs, MiniDump.ExceptionInfo.Present);
                    }

                    using (var file = File.OpenWrite(path + ".zip"))
                    using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                    {
                        using (var dumpFile = File.OpenRead(path))
                        using (var entry = zip.CreateEntry(Path.GetFileName(path)).Open())
                        {
                            dumpFile.CopyTo(entry);
                        }

                        using (var logFile = File.Open("logs/lrm.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var entry = zip.CreateEntry("log.txt").Open())
                        {
                            logFile.CopyTo(entry);
                        }
                    }

                    File.Delete(path);

                    Process.Start("explorer.exe", $"/select, \"{path}.zip\"");
                }

                if (e.IsTerminating)
                {
                    Legendary_Rune_Maker.MainWindow.DisposeTaskbar();
                    Process.GetCurrentProcess().Kill();
                }
            }

            static void Report()
            {
                var currProc = Process.GetCurrentProcess();
                Process.Start(currProc.MainModule.FileName, "--report");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoL.BindNinject(Container);

            Container.Bind<Config>().ToMethod(_ => Config.Current);
            Container.Bind<Container<Config>>().ToConstant(Config.Container);

            Container.Bind<Actuator>().ToSelf().InSingletonScope();
            Container.Bind<Detector>().To<LoginDetector>();
            Container.Bind<Detector>().To<ChampSelectDetector>();
            Container.Bind<Detector>().To<ReadyCheckDetector>();

            Container.Bind<MainWindow>().ToSelf().InSingletonScope();
            Container.Bind<OverlayWindow>().ToSelf().InSingletonScope();

            Container.Bind<ITeamGuesser>().To<TeamGuesser>().InSingletonScope();

            //Container.Get<OverlayWindow>().Show();
            //return;

#if RELEASE
            if (Config.Current.SendTrackRequest)
            {
                Task.Run(async () =>
                {
                    LogTo.Debug("Sending track request");

                    try
                    {
                        await new HttpClient().PostAsync("http://pipe01.net/lrm/track.php?version=" + LRM.Version, null);
                    }
                    catch (Exception ex)
                    {
                        LogTo.ErrorException("Failed to send track request", ex);
                    }
                });
            }
#endif

            var loadingWindow = Container.Get<LoadingWindow>();
            Current.MainWindow = loadingWindow;

            if (!loadingWindow.IsClosed)
                Current.MainWindow.Show();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == e.Source)
                (sender as Window)?.DragMove();
        }
    }
}
