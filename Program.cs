using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace LXFPartListCreator
{
    using static ConsoleColor;
    using static Console;

    using Properties;

    public static class Program
    {
        public const string PATH_LXFML = "IMAGE100.LXFML";
        public const string PATH_PNG = "IMAGE100.PNG";


        public static void Main(string[] argv)
        {
            Dictionary<string, string> opt = GetOptions(argv);
            bool hasopt(string n, out string a)
            {
                (a, n) = (null, n.ToLower());

                if (opt.ContainsKey(n))
                    a = opt[n];

                return a != null;
            }

            if (!argv.Any() || hasopt("help", out _))
            {
                WriteLine(@"
+---------------------------------------------------------+
|        LXF Part List Creator Tool by Unknown6665        |
+---------------------------------------------------------+

Usage:
    --in=...                Input .LXF file
    --out=...               Output .HTML file
    --ignore-working-dir    Runs the tool from the assembly's location
                            instead of from the working directory
    -- open-after-success   Opens the generated .HTML file after a
                            successful file generation
");

                return;
            }

            if (hasopt("ignore-working-dir", out _))
                Directory.SetCurrentDirectory(new FileInfo(typeof(Program).Assembly.Location).Directory.FullName);
            
            if (hasopt("in", out string @in) && hasopt("out", out string @out))
                try
                {
                    string thumbnail;
                    LXFML lxfml;

                    using (ZipArchive lxf = ZipFile.Open(@in, ZipArchiveMode.Read))
                    using (Stream lxfms = lxf.GetEntry(PATH_LXFML).Open())
                    using (Stream pngms = lxf.GetEntry(PATH_PNG).Open())
                    using (Bitmap png = Image.FromStream(pngms) as Bitmap)
                    using (MemoryStream ms = new MemoryStream())
                    {
                        XmlSerializer xmlser = new XmlSerializer(typeof(LXFML));

                        lxfml = xmlser.Deserialize(lxfms) as LXFML;

                        png.Save(ms, ImageFormat.Png);

                        thumbnail = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                    }

                    FileInfo nfo = new FileInfo(@out);

                    if (nfo.Exists)
                        nfo.Delete();

                    using (FileStream fs = nfo.OpenWrite())
                    using (StreamWriter wr = new StreamWriter(fs, Encoding.UTF8))
                        wr.Write(GenerateHTML(lxfml, thumbnail, opt));

                    if (hasopt("open-after-success", out _))
                        Process.Start(@out);
                }
                catch (Exception ex)
                {
                    ex.Err();
                }
            else
                "No input and/or output file path has been provided.".Err();

            if (Debugger.IsAttached)
            {
                WriteLine("Press any key to exit ...");
                ReadKey(true);
            }
        }

        private static string GenerateHTML(LXFML lxfml, string thumbnail, Dictionary<string, string> opt)
        {




            return Resources.template.DFormat(new Dictionary<string, string>
            {
                ["model_path"] = opt["in"],
                ["model_name"] = opt["in"], // TODO
                ["thumbnail"] = thumbnail,
            });
        }

        private static Dictionary<string, string> GetOptions(string[] argv)
        {
            Dictionary<string, string> dic = new Dictionary<string, string> {
                [""] = "",
            };

            foreach (string s in argv ?? new string[0])
                if (!string.IsNullOrWhiteSpace(s))
                {
                    if (s.match(@"(\-\-(?<name>[\w\-]+)(\=(?<val>.*))?|\-(?<name>[\w\-]+)(\=(?<val>.*))?|\/(?<name>[\w\-]+)(\:(?<val>.*))?)", out Match m))
                        dic[m.Groups["name"].ToString().ToLower()] = m.Groups["val"].ToString();
                    else
                        dic[""] += ' ' + s;
                }

            return dic;
        }

        private static string DFormat(this string formatstring, Dictionary<string, string> dic)
        {
            Dictionary<string, int> kti = new Dictionary<string, int>();
            StringBuilder sb = new StringBuilder(formatstring);
            int i = 0;

            sb = sb.Replace("{", "{{")
                   .Replace("}", "}}");

            foreach (var tuple in dic)
            {
                sb = sb.Replace("§" + tuple.Key + "§", "{" + i.ToString() + "}");

                kti.Add(tuple.Key, i);

                ++i;
            }

            sb = sb.Replace("§§", "§");

            return String.Format(sb.ToString(), dic.OrderBy(x => kti[x.Key]).Select(x => x.Value).ToArray());
        }

        private static bool match(this string s, string pat, out Match m, RegexOptions opt = RegexOptions.Compiled | RegexOptions.IgnoreCase) =>
            (m = Regex.Match(s, pat, opt)).Success;

        private static void Err(this string str)
        {
            ConsoleColor clr = ForegroundColor;

            ForegroundColor = Red;
            WriteLine(str);
            ForegroundColor = clr;
        }

        private static void Err(this Exception err)
        {
            StringBuilder sb = new StringBuilder();

            while (err != null)
            {
                sb.AppendLine(err.Message);
                sb.AppendLine(err.StackTrace);

                err = err.InnerException;
            }

            sb.ToString().Err();
        }
    }
}
