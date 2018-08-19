using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LoL_Rune_Maker.Data
{
    public class Rune
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("icon")]
        public string IconURL { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("shortDesc")]
        public string ShortDesc { get; set; }

        [JsonProperty("longDesc")]
        public string LongDesc { get; set; }

        public string UppercaseName => Name.ToUpper();

        public string RichLongDesc => Richify(LongDesc);

        private static string Richify(string desc)
        {
            IList<(string Pattern, string Replacement)> patterns = new List<(string, string)>
            {
                (@"<\/li><br>", "</li>"),
                ("â€”", "—"),
                ("<br>$", ""),
                ("<br><br><hr>", "<br><hr>"),
                ("<br>", "<LineBreak/>"),
                (@"<b>(.*?)<\/b>", "<Bold>$1</Bold>"),
                (@"<i>(.*?)<\/i>", "<Italic>$1</Italic>"),
                (@"<rules>(.*?)<\/rules>", "$1"),
                (@"<scale.*?>(.*?)<\/scale.*?>", "$1"),
                (@"<hr><\/hr>", "<Line/>"),
                (@"<\/li>", "</li> "),
                (@"<lol-uikit-tooltipped-keyword key='.*?'>(.*?)<\/lol-uikit-tooltipped-keyword>", "$1"),
                (@"<font color='(.*?)'>(.*?)<\/font>", "<Run Foreground=\"$1\">$2</Run>"),
            };

            bool b = desc.Contains("Noxian");

            foreach (var item in patterns)
            {
                desc = Regex.Replace(desc, item.Pattern, item.Replacement, RegexOptions.None);
            }

            var listMatches = Regex.Matches(desc, @"(<li>(.*?)<\/li>)+");

            if (listMatches.Count > 0)
            {
                string list = "</Paragraph><List>";
                int offset = 0;
                foreach (Match item in listMatches)
                {
                    desc = desc.Remove(item.Index - offset, item.Length);
                    offset += item.Length;

                    list += $"<ListItem><Paragraph>{item.Groups[2].Value}</Paragraph></ListItem>";
                }
                list += "</List><Paragraph>";

                desc = desc.Insert(listMatches[0].Index, list);
            }

            return $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><Paragraph>{desc}</Paragraph></FlowDocument>";
        }
    }
}
