using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

using NSass;

namespace LXF
{
    using static ConsoleColor;
    using static Console;
    using static Math;

    using Properties;

    public static unsafe class Program
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
    --open-after-success    Opens the generated .HTML file after a successful file generation
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

                        db.AddProvider<BricksetDotCom>().BitmapPostprocessor = ImageCleanup.RemoveEdges;
                        db.AddProvider<BrickowlDotCom>();
                        // TODO : add other services/backup sites [?]

                        while (!db.CanOperate)
                        {
                            WriteLine("Cannot operate yet. Please check your network connection.");

                            Thread.Sleep(2000);
                        }

                        if (hasopt("delete-cache", out _))
                            db.ClearIndex();

                        if (hasopt("delete-image-cache", out _))
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
                        string html = GenerateHTML(lxfml, thumbnail, opt, db);

                        if (nfo.Exists)
                            nfo.Delete();

                        using (FileStream fs = nfo.OpenWrite())
                        using (StreamWriter wr = new StreamWriter(fs, Encoding.UTF8))
                            wr.Write(html);

                        if (hasopt("open-after-success", out _))
                            Process.Start(nfo.FullName);
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
            #region INIT

            StringBuilder sb = new StringBuilder();

            var partlist = (from brick in lxfml.Bricks.Brick
                            let part = brick.Part
                            //from mat in part?.materials ?? new int[0]
                            //from dec in part?.decoration ?? new int[0]
                            //where mat != 0
                            let mat = part?.materials?.Where(m => m > 0).ToArray() ?? new short[0]
                            let dec = part?.decoration?.Where(m => m > 0).ToArray() ?? new short[0]
                            group brick by new LXFMLBrickGrouping(part.designID, mat, dec) into g
                            let count = g.Count()
                            orderby g.Key.DesignID ascending
                            orderby count descending
                            select new
                            {
                                Count = count,
                                ID = g.Key.DesignID,
                                Matr = g.Key.Material,
                                Decor = g.Key.Decoration,
                                Bricks = g as IEnumerable<LXFMLBricksBrick>
                            }).ToArray();
            (float min, float avg, float max) total = (0, 0, 0);
            int partno = 0;
            int partcnt = lxfml.Bricks.Brick.Length;
            int listcnt = partlist.Length;

            void Print(string msg) => WriteLine($"[{DateTime.Now:HH:mm:ss.ffffff}][<main>] {msg}");

            #endregion

            foreach (var parts in partlist)
            {
                #region INIT

                BrickInfo part = db[parts.ID];
                BrickVariation bvar1 = part?.Variations?.FirstOrDefault(v => parts.Matr.Contains((short)v.ColorID)  || v.PartID == parts.ID);
                BrickVariation bvar2 = bvar1 ?? part?.Variations?.FirstOrDefault();

                string sel(Func<string> f1, Func<string> f2, Func<string> f3) => (bvar1 != null ? f1 : bvar2 != null ? f2 : f3)();

                #endregion
                #region COLORIZER + IMAGE

                ColorInfo color = db.GetColor(bvar1?.ColorID ?? parts.Matr.FirstOrDefault()) ?? db.GetColor(bvar2?.ColorID ?? 0);
                string rgbclr = color?.RGB ?? "transparent";
                (float min, float avg, float max) pprice = (bvar2?.PriceMin ?? float.NaN, bvar2?.PriceAvg ?? float.NaN, bvar2?.PriceMax ?? float.NaN);
                (float min, float avg, float max) tprice = (parts.Count * pprice.min, parts.Count * pprice.avg, parts.Count * pprice.max);
                string png = db.GetImageByPartID(bvar2?.PartID ?? parts.ID, src =>
                {
                    int w = src.Width;
                    int h = src.Height;
                    Bitmap dst = new Bitmap(w, h, src.PixelFormat);
                    BitmapData dsrc = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, src.PixelFormat);
                    BitmapData ddst = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, dst.PixelFormat);
                    ARGB* psrc = (ARGB*)dsrc.Scan0;
                    ARGB* pdst = (ARGB*)ddst.Scan0;
                    ARGB clr = color.RGBAValues;
                    double φ(double x) => .5 - Pow(2 * x - 1, 2);
                    
                    Parallel.For(0, w * h, i =>
                    {
                        double gr = psrc[i].Gray / 255d;
                        double intp = φ(gr);
                        double cintp = (1 - intp) * gr;

                        pdst[i].A = psrc[i].A;
                        pdst[i].R = (byte)Min(255, 255 * (intp * clr[0] + cintp));
                        pdst[i].G = (byte)Min(255, 255 * (intp * clr[1] + cintp));
                        pdst[i].B = (byte)Min(255, 255 * (intp * clr[2] + cintp));
                    });
                    
                    src.UnlockBits(dsrc);
                    dst.UnlockBits(ddst);

                    return dst;
                }) ?? "";
                
                #endregion
                #region HTML CORE

                sb.Append($@"
<li>
    <table border=""0"" width=""100%"">
        <tr width=""100%"">
            <td class=""td1"">
                <div class=""img"" count=""{parts.Count}"" style=""background-image: url('{png}'); filter: drop-shadow(0px 0px 6px {color?.RGB ?? "transparent"});""/>
            </td>
            <td>
                <h2>{part?.Name ?? $"&lt;{parts.ID}&gt;"} &nbsp; - &nbsp; {color?.Name}</h2>
                <span class=""mono"">
                    ID: p.{bvar2?.PartID}/d.{part?.DesignID}
                </span>
            </td>
            <td valign=""bottom"" align=""right"" class=""mono td3"">
                <a target=""_blank"" href=""{string.Format(bvar1 is null ? BricksetDotCom.URL_DESIGN : BricksetDotCom.URL_PART, bvar1?.PartID ?? parts.ID)}"">   	
                    &#8631; brickset.com<br/>
                </a>
                {parts.Count} x {pprice.min:F2}€ = {tprice.min:F2}€<br/>
                {parts.Count} x {pprice.avg:F2}€ = {tprice.avg:F2}€<br/>
                {parts.Count} x {pprice.max:F2}€ = {tprice.max:F2}€</td>
        </tr>
    </table>
</li>");

                #endregion
                #region PRICE + DEBUG

                if (!float.IsNaN(tprice.min)) total.min += tprice.min;
                if (!float.IsNaN(tprice.avg)) total.avg += tprice.avg;
                if (!float.IsNaN(tprice.max)) total.max += tprice.max;
                
                partcnt -= parts.Count;

                ++partno;
                
                if ((int)(partno % (listcnt / 400f)) == 0)
                    Print($"{100f * partno / listcnt:N3}% generated ({partno}/{listcnt}).   Current price: {total.min:N2} EUR ... {total.avg:N2} EUR ... {total.max:N2} EUR");

                #endregion
            }

            #region APPEND MISSING

            if (partcnt > 0)
                sb.Append($@"
<li>
    <table border=""0"" width=""100%"">
        <tr width=""100%"">
            <td class=""td1"">
                <div class=""img"" count=""{partcnt}""/>
            </td>
            <td>
                <h2>??????????????</h2>
                <span class=""mono"">
                    ID: p.???????/d.?????
                </span>
            </td>
            <td valign=""bottom"" align=""right"" class=""mono td3"">
                {partcnt} x ???€ = ???€<br/>
                {partcnt} x ???€ = ???€<br/>
                {partcnt} x ???€ = ???€<br/>
            </td>
        </tr>
    </table>
</li>");

            #endregion
            #region PRICE + FORMAT

            if (partlist.Any())
            {
                if (total.min == 0) total.min = float.NaN;
                if (total.avg == 0) total.avg = float.NaN;
                if (total.max == 0) total.max = float.NaN;
            }

            string css;

            using (MemoryStream ms = new MemoryStream(Resources.style))
            using (StreamReader sr = new StreamReader(ms, Encoding.UTF8))
                css = new SassCompiler().Compile(sr.ReadToEnd(), OutputStyle.Compressed, true);

            int brickcount = lxfml.Bricks.Brick.Length;

            return Resources.template.DFormat(new Dictionary<string, object>
            {
                ["style"] = css,
                ["model_price_min"] = $"{total.min:F2}€",
                ["model_price_avg"] = $"{total.avg:F2}€",
                ["model_price_max"] = $"{total.max:F2}€",
                ["price_per_brick_min"] = $"{total.min / brickcount:F3}€",
                ["price_per_brick_avg"] = $"{total.avg / brickcount:F3}€",
                ["price_per_brick_max"] = $"{total.max / brickcount:F3}€",
                ["timestamp"] = DateTime.Now.ToString("dd. MMM yyyy - HH:mm:ss.ffffff"),
                ["model_path"] = opt["in"],
                ["model_name"] = lxfml.name,
                ["model_version"] = $"{lxfml.versionMajor}.{lxfml.versionMinor}",
                ["tool_version"] = $"{lxfml.Meta}",
                ["thumbnail"] = thumbnail,
                ["bricks"] = sb.ToString(),
                ["bricklist_count"] = listcnt,
                ["brick_count"] = brickcount,
            });

            #endregion
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


        private struct LXFMLBrickGrouping
        {
            public int DesignID { get; }
            public short[] Material { get; }
            public short[] Decoration { get; }

            private short[] mat => Material.Distinct().ToArray();
            private short[] dec => Material.Distinct().ToArray();


            public override bool Equals(object obj) =>
                obj is LXFMLBrickGrouping g ? g.GetHashCode() == GetHashCode() : false;

            public override int GetHashCode()
            {
                uint hc = (uint)(mat.Length ^ (dec.Length << 16));

                for (int i = 0; i < Math.Min(mat.Length, dec.Length); ++i)
                {
                    hc = (hc << 1) | (hc >> 31);
                    hc ^= (ushort)mat[i] | ((uint)dec[i] << 16);
                }

                return (int)hc ^ ~DesignID;
            }

            public LXFMLBrickGrouping(int id, short[] mat, short[] dec) =>
                (DesignID, Material, Decoration) = (id, mat, dec);
        }
    }
}
