using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker
{
    public static class LRM
    {
        public static readonly string GitBranch;
        public static readonly string GitCommit;

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
