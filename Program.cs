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

using NSass;

namespace LXFPartListCreator
{
    using static ConsoleColor;
    using static Console;

    using Properties;

    public static class Program
    {
        public const string PATH_CACHE = "part-cache";
        public const string PATH_LXFML = "IMAGE100.LXFML";
        public const string PATH_PNG = "IMAGE100.PNG";


        public static void Main(string[] argv)
        {
            string currdir = new FileInfo(typeof(Program).Assembly.Location).Directory.FullName;
            Dictionary<string, string> opt = GetOptions(argv);
            bool hasopt(string n, out string a)
            {
                (a, n) = (null, n.ToLower());

                if (opt.ContainsKey(n))
                    a = opt[n];

                return a != null;
            }

            if (argv.All(string.IsNullOrWhiteSpace) || !argv.Any() || hasopt("help", out _))
            {
                int year = DateTime.Now.Year;
                var clr = ForegroundColor;

                ForegroundColor = Yellow;
                WriteLine($@"
_____________________________________________________________________________________________________________

                                LXF Part List Creator Tool by Unknown6665
_____________________________________________________________________________________________________________

Usage:
    --in=...                Input .LXF file
    --out=...               Output .HTML file
    --ignore-working-dir    Runs the tool from the assembly's location instead of from the working directory
    -- open-after-success   Opens the generated .HTML file after a successful file generation
    --cache=...             The LEGO part cache directory. default:
                            '{currdir}/{PATH_CACHE}'
    --delete-cache          Deletes the cache index before generating the .HTML file
    --delete-image-cache    Deletes the cached images before generating the .HTML file

_____________________________________________________________________________________________________________

                                  Copyright © 2017-{year}, Unknown6656
  
  LEGO, LXF, LXFML, LDD and the Brick and Knob configurations are trademarks of the LEGO Group of Companies. 
                                    Copyright © {year} The LEGO Group.
");
                ForegroundColor = clr;

                return;
            }

            if (hasopt("ignore-working-dir", out _))
                Directory.SetCurrentDirectory(currdir);

            if (!hasopt("cache", out string cache))
                cache = $"{currdir}/{PATH_CACHE}";

            if (hasopt("in", out string @in) && hasopt("out", out string @out))
                using (LEGODatabaseManager db = new LEGODatabaseManager(cache))
                    try
                    {
                        string thumbnail;
                        LXFML lxfml;

                        db.AddProvider<BricksetDotCom>();
                        // TODO : add other services/backup sites [?]

                        if (hasopt("--delete-cache", out _))
                            db.ClearIndex();

                        if (hasopt("--delete-image-cache", out _))
                            db.ClearImages();

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
                            wr.Write(GenerateHTML(lxfml, thumbnail, opt, db));

                        if (hasopt("open-after-success", out _))
                            Process.Start(@out);
                    }
                    catch (Exception ex)
                    {
                        ex.Err();
                    }
                    finally
                    {
                        db.Save();
                    }
            else
                "No input and/or output file path has been provided.".Err();

            if (Debugger.IsAttached)
            {
                WriteLine("Press any key to exit ...");
                ReadKey(true);
            }
        }

        private static string GenerateHTML(LXFML lxfml, string thumbnail, Dictionary<string, string> opt, LEGODatabaseManager db)
        {
            StringBuilder sb = new StringBuilder();

            var partlist = (from brick in lxfml.Bricks.Brick
                            let part = brick.Part
                            from mat in part?.materials ?? new int[0]
                            from dec in part?.decoration ?? new int[0]
                            where mat != 0
                            group brick by (id: part.designID, mat: mat, dec: dec) into g
                            let count = g.Count()
                            orderby g.Key.id ascending
                            orderby count descending
                            select new
                            {
                                Count = count,
                                ID = g.Key.id,
                                Matr = g.Key.mat,
                                Decor = g.Key.dec,
                                Bricks = g as IEnumerable<LXFMLBricksBrick>
                            }).ToArray();
            float total = 0;

            foreach (var parts in partlist.Take(20))
            {
                BrickInfo part = db[parts.ID];
                BrickVariation bvar1 = part.Variations.FirstOrDefault(v => v.ColorID == parts.Matr || v.PartID == parts.ID);
                BrickVariation bvar2 = bvar1 ?? part.Variations.FirstOrDefault();
                ColorInfo color = db.GetColor(bvar1?.ColorID ?? parts.Matr) ?? db.GetColor(bvar2?.ColorID ?? 0);
                string rgbclr = color?.RGB ?? "transparent";
                float pprice = bvar2?.PriceAvg ?? float.NaN;
                float tprice = parts.Count * pprice;

                sb.Append(@"
<li>
    <table border=""0"" width=""100%"">
        <tr width=""100%"">
            <td>")
                  .Append(sel(
                      () => $@"<div class=""img"" count=""{parts.Count}"" style=""background-image: url('{db.GetImageByPartID(bvar1.PartID)}');""/>",
                      () => $@"<div class=""img invalid"" count=""{parts.Count}"" style=""background-image: url('{db.GetImageByPartID(bvar2.PartID)}'); box-shadow: inset 0px 0px 72px 72px {rgbclr}, 0px 0px 4px 4px {rgbclr};""/>",
                      () => $@"<div class=""img invalid"" count=""{parts.Count}""/>"
                  ))
                  .Append($@"
                    </td>
                    <td>
                        <h2>{part.Name} &nbsp; - &nbsp; {color?.Name}</h2>
                        <span class=""mono"">
                            ID: p.{bvar2?.PartID}/d.{part.DesignID}
                        </span>
")
                  .Append(sel(
                      () => "",
                      () => "",
                      () => ""
                  ))
                  .Append($@"
            </td>
            <td valign=""bottom"" align=""right"" class=""mono"">{parts.Count} x {pprice:F2}€ = {tprice:F2}€</td>
        </tr>
    </table>
</li>");
                if (tprice != float.NaN)
                    total += tprice;

                string sel(Func<string> f1, Func<string> f2, Func<string> f3) => (bvar1 != null ? f1 : bvar2 != null ? f2 : f3)();
            }

            if (total == 0 && partlist.Any())
                total = float.NaN;

            string css;

            using (MemoryStream ms = new MemoryStream(Resources.style))
            using (StreamReader sr = new StreamReader(ms, Encoding.UTF8))
                css = new SassCompiler().Compile(sr.ReadToEnd(), OutputStyle.Compressed, true);

            return Resources.template.DFormat(new Dictionary<string, object>
            {
                ["model_price"] = $"{total:F2}€",
                ["style"] = css,
                ["timestamp"] = DateTime.Now.ToString("dd. MMM yyyy - HH:mm:ss.ffffff"),
                ["model_path"] = opt["in"],
                ["model_name"] = lxfml.name,
                ["model_version"] = $"{lxfml.versionMajor}.{lxfml.versionMinor}",
                ["tool_version"] = $"{lxfml.Meta}",
                ["thumbnail"] = thumbnail,
                ["bricks"] = sb.ToString(),
                ["brick_count"] = lxfml.Bricks.Brick.Length,
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

        internal static string DFormat(this string formatstring, Dictionary<string, object> dic)
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

            return string.Format(sb.ToString(), dic.OrderBy(x => kti[x.Key]).Select(x => x.Value).ToArray());
        }

        internal static bool match(this string s, string pat, out Match m, RegexOptions opt = RegexOptions.Compiled | RegexOptions.IgnoreCase) =>
            (m = Regex.Match(s, pat, opt)).Success;

        internal static void Err(this string str)
        {
            ConsoleColor clr = ForegroundColor;

            ForegroundColor = Red;
            WriteLine(str);
            ForegroundColor = clr;
        }

        internal static void Err(this Exception err)
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
