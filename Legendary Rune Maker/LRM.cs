using System.IO;
using System.Reflection;

namespace Legendary_Rune_Maker
{
    public static class LRM
    {
        public static string GitBranch { get; }
        public static string GitCommit { get; }
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        static LRM()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Legendary_Rune_Maker.GitData.txt"))
            using (var reader = new StreamReader(stream))
            {
                GitBranch = reader.ReadLine();
                GitCommit = reader.ReadLine();
            }
        }
    }
}
