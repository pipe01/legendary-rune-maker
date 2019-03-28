using Anotar.Log4Net;
using LCU.NET;
using LCU.NET.Extras;
using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Game;
using Legendary_Rune_Maker.Pages;
using Legendary_Rune_Maker.Utils;
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
            LogTo.Info($"Starting LRM {LRM.GitCommit}@{LRM.GitBranch}");

            Container = LCUApp.Container;
        }

        public App()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = Config.Current.Culture;

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
