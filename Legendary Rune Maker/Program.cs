using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Legendary_Rune_Maker
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--report")
            {
                string code = await UploadErrorLog();
                MessageBox.Show("Report code is " + code, "Reported successfully", MessageBoxButton.OK, MessageBoxImage.Information);
                // MessageBox.Show("Failed to report error", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                var t = new Thread(() =>
                {
                    var app = new App();
                    app.InitializeComponent();
                    app.Run();
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
        }

        private static async Task<string> UploadErrorLog()
        {
            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(LRM.Version), "version");

            using var logFile = File.Open("logs/lrm.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            content.Add(new StreamContent(logFile), "log", "log.txt");

            var resp = await client.PostAsync("https://pipe01.net/lrm/report.php", content);
            var str = await resp.Content.ReadAsStringAsync();

            if (!Regex.IsMatch(str, @"\w{3}-\w{3}"))
                return "ERROR";

            return str;
        }
    }
}
