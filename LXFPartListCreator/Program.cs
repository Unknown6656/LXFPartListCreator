using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Newtonsoft.Json;

using NSass;

using XApplication = Microsoft.Office.Interop.Excel.Application;
using Rectangle = System.Drawing.Rectangle;

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
    --out=...               Output document file
    --type=...              Output document type/format. Only the following values are supported:
           HTML   [default]
           JSON
           XML
           EXCEL
    --x-type=...            The excel output format (only used with '--type=EXCEL')            
             XLS            Excel 95-2003 format
             XLSX           Excel 2007+ format (based on OpenXML)
             HTML           HTML document
             CSV            CSV file
    --ignore-working-dir    Runs the tool from the assembly's location instead of from the working directory
    --open-after-success    Opens the generated document file after a successful file generation
    --cache=...             The LEGO part cache directory. default:
                            '{currdir}/{PATH_CACHE}'
    --delete-cache          Deletes the cache index before generating the document file
    --delete-image-cache    Deletes the cached images before generating the document file

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

            if (hasopt("in", out string @in))
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

                        Dictionary<string, Type> gendic = new Dictionary<string, Type>
                        {
                            ["xml"] = typeof(XMLDocument),
                            ["html"] = typeof(HTMLDocument),
                            ["json"] = typeof(JSONDocument),
                            ["excel"] = typeof(EXCELDocument),
                        };

                        if (!hasopt("type", out string doctype) && !gendic.ContainsKey(doctype.ToLower()))
                            doctype = "html";

                        DocumentGenerator gen = Activator.CreateInstance(gendic[doctype.ToLower()]) as DocumentGenerator;

                        gen.Thumbnail = thumbnail;
                        gen.Options = opt;
                        gen.Database = db;

                        if (!hasopt("out", out string @out))
                        {
                            FileInfo ifile = new FileInfo(@in);

                            @out = $"{ifile.Directory.FullName}/{ifile.Name.Replace(ifile.Extension, "")}{gen.Extension}";
                        }

                        FileInfo nfo = new FileInfo(@out);
                        string doc;

                        doc = Generate(gen, lxfml);

                        if (nfo.Exists)
                            nfo.Delete();

                        using (FileStream fs = nfo.OpenWrite())
                        using (StreamWriter wr = new StreamWriter(fs, Encoding.UTF8))
                            wr.Write(doc);

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
                WriteLine("Press ESC to exit ...");

                while (ReadKey(true).Key != ConsoleKey.Escape)
                    ;
            }
        }

        private static string Generate<T>(LXFML lxfml)
            where T : DocumentGenerator, new() => Generate(new T(), lxfml);

        private static string Generate(Type gentype, LXFML lxfml) =>
            Generate(Activator.CreateInstance(gentype) as DocumentGenerator, lxfml);

        private static string Generate(DocumentGenerator gen, LXFML lxfml)
        {
            #region INIT
            
            StringBuilder sb = new StringBuilder();
            void Print(string msg) => WriteLine($"[{DateTime.Now:HH:mm:ss.ffffff}][<main>] {msg}");
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
                            select (
                                Count: count,
                                ID: g.Key.DesignID,
                                Matr: g.Key.Material,
                                Decor: g.Key.Decoration,
                                Bricks: g as IEnumerable<LXFMLBricksBrick>
                            )).ToArray();
            Price total = new Price(0, 0, 0);
            int partno = 0;
            int partcnt = lxfml.Bricks.Brick.Length;
            int listcnt = partlist.Length;

            gen.Partlist = partlist;
            gen.Print = Print;

            #endregion

            gen.BeforeIteration(ref sb);

            foreach (var parts in partlist)
            {
                #region INIT

                BrickInfo part = gen.Database[parts.ID];
                BrickVariation bvar1 = part?.Variations?.FirstOrDefault(v => parts.Matr.Contains((short)v.ColorID) || v.PartID == parts.ID);
                BrickVariation bvar2 = bvar1 ?? part?.Variations?.FirstOrDefault();

                Price pprice = new Price(bvar2?.PriceMin ?? float.NaN, bvar2?.PriceAvg ?? float.NaN, bvar2?.PriceMax ?? float.NaN);
                Price tprice = new Price(parts.Count * pprice.Min, parts.Count * pprice.Avg, parts.Count * pprice.Max);

                string sel(Func<string> f1, Func<string> f2, Func<string> f3) => (bvar1 != null ? f1 : bvar2 != null ? f2 : f3)();

                #endregion
                #region COLORIZER + IMAGE

                ColorInfo color = gen.Database.GetColor(bvar1?.ColorID ?? parts.Matr.FirstOrDefault()) ?? gen.Database.GetColor(bvar2?.ColorID ?? 0);
                string rgbclr = color?.RGB ?? "transparent";
                string png = gen.Database.GetImageByPartID(bvar2?.PartID ?? parts.ID, src =>
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
                
                gen.Iteration(ref sb, new DocumentGenerator.IterationData
                {
                    parts = parts,
                    PNG = png,
                    PartsPrice = tprice,
                    BrickPrice = pprice,
                    Color = color,
                    Part = part,
                    BrickVariation1 = bvar1,
                    BrickVariation2 = bvar2,
                });

                #region PRICE + DEBUG

                if (!float.IsNaN(tprice.Min)) total.Min += tprice.Min;
                if (!float.IsNaN(tprice.Avg)) total.Avg += tprice.Avg;
                if (!float.IsNaN(tprice.Max)) total.Max += tprice.Max;

                partcnt -= parts.Count;

                ++partno;

                if ((int)(partno % (listcnt / 400f)) == 0)
                    Print($"{100f * partno / listcnt:N3}% generated ({partno}/{listcnt}).   Current price: {total.Min:N2} EUR ... {total.Avg:N2} EUR ... {total.Max:N2} EUR");

                #endregion
            }

            gen.AfterIteration(ref sb, partcnt);
            
            if (partlist.Any())
            {
                if (total.Min == 0) total.Min = float.NaN;
                if (total.Avg == 0) total.Avg = float.NaN;
                if (total.Max == 0) total.Max = float.NaN;
            }

            int brickcount = lxfml?.Bricks?.Brick?.Length ?? partlist.Sum(x => x.Count);

            return gen.GenerateDocument(sb, total, brickcount);
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
                sb.Insert(0, $"[{err.HResult:x8}h] {err.Message}\n{err.StackTrace}");

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

    internal abstract class DocumentGenerator
    {
        public LXFML Model { set; get; }
        public string Thumbnail { set; get; }
        public Action<string> Print { set; get; }
        public LEGODatabaseManager Database { set; get; }
        public Dictionary<string, string> Options { set; get; }
        public (int Count, int ID, short[] Matr, short[] Decor, IEnumerable<LXFMLBricksBrick> Bricks)[] Partlist { set; get; }

        public abstract string Extension { get; }

        public abstract void BeforeIteration(ref StringBuilder sb);
        public abstract void Iteration(ref StringBuilder sb, IterationData data);
        public abstract void AfterIteration(ref StringBuilder sb, int partcnt);
        public abstract string GenerateDocument(StringBuilder sb, Price total, int brickcount);

        protected bool HasOption(string name, out string value)
        {
            (value, name) = (null, name.ToLower());

            if (Options?.ContainsKey(name) ?? false)
                value = Options[name];

            return value != null;
        }

        internal struct IterationData
        {
            public (int Count, int ID, short[] Matr, short[] Decor, IEnumerable<LXFMLBricksBrick> Bricks) parts { set; get; }

            public string PNG { set; get; }
            public Price PartsPrice { set; get; }
            public Price BrickPrice { set; get; }
            public ColorInfo Color { set; get; }
            public BrickInfo Part { set; get; }
            public BrickVariation BrickVariation1 { set; get; }
            public BrickVariation BrickVariation2 { set; get; }

            public int Count => parts.Count;
            public int DesignID => parts.ID;
            public int? PartID => (BrickVariation1 ?? BrickVariation2)?.PartID;
        }
    }

    internal unsafe sealed class HTMLDocument
        : DocumentGenerator
    {
        public override string Extension => ".html";

        public override void BeforeIteration(ref StringBuilder sb)
        {
        }

        public override string GenerateDocument(StringBuilder sb, Price total, int brickcount)
        {
            string css;

            using (MemoryStream ms = new MemoryStream(Resources.style))
            using (StreamReader sr = new StreamReader(ms, Encoding.UTF8))
                css = new SassCompiler().Compile(sr.ReadToEnd(), OutputStyle.Compressed, true);

            return Resources.template.DFormat(new Dictionary<string, object>
            {
                ["style"] = css,
                ["model_price_min"] = $"{total.Min:F2}€",
                ["model_price_avg"] = $"{total.Avg:F2}€",
                ["model_price_max"] = $"{total.Max:F2}€",
                ["price_per_brick_min"] = $"{total.Min / brickcount:F3}€",
                ["price_per_brick_avg"] = $"{total.Avg / brickcount:F3}€",
                ["price_per_brick_max"] = $"{total.Max / brickcount:F3}€",
                ["timestamp"] = DateTime.Now.ToString("dd. MMM yyyy - HH:mm:ss.ffffff"),
                ["model_path"] = Options["in"],
                ["model_name"] = Model?.name,
                ["model_version"] = $"{Model?.versionMajor ?? 0}.{Model?.versionMinor ?? 0}",
                ["tool_version"] = Model?.Meta?.ToString(),
                ["thumbnail"] = Thumbnail,
                ["bricks"] = sb.ToString(),
                ["bricklist_count"] = Partlist.Length,
                ["brick_count"] = brickcount,
            });
        }

        public override void AfterIteration(ref StringBuilder sb, int missing)
        {
            if (missing > 0)
                sb.Append($@"
<li>
    <table border=""0"" width=""100%"">
        <tr width=""100%"">
            <td class=""td1"">
                <div class=""img"" count=""{missing}""/>
            </td>
            <td>
                <h2>??????????????</h2>
                <span class=""mono"">
                    ID: p.???????/d.?????
                </span>
            </td>
            <td valign=""bottom"" align=""right"" class=""mono td3"">
                {missing} x ???€ = ???€<br/>
                {missing} x ???€ = ???€<br/>
                {missing} x ???€ = ???€<br/>
            </td>
        </tr>
    </table>
</li>");
        }

        public override void Iteration(ref StringBuilder sb, IterationData data)
        {
            sb.Append($@"
<li>
    <table border=""0"" width=""100%"">
        <tr width=""100%"">
            <td class=""td1"">
                <div class=""img"" count=""{data.Count}"" style=""background-image: url('{data.PNG}'); filter: drop-shadow(0px 0px 6px {data.Color?.RGB ?? "transparent"});""/>
            </td>
            <td>
                <h2>{data.Part?.Name ?? $"&lt;{data.DesignID}&gt;"} &nbsp; - &nbsp; {data.Color?.Name}</h2>
                <span class=""mono"">
                    ID: p.{data.BrickVariation2?.PartID}/d.{data.Part?.DesignID}
                </span>
            </td>
            <td valign=""bottom"" align=""right"" class=""mono td3"">
                <a target=""_blank"" href=""{string.Format(data.BrickVariation1 is null ? BricksetDotCom.URL_DESIGN : BricksetDotCom.URL_PART, data.BrickVariation1?.PartID ?? data.DesignID)}"">   	
                    &#8631; brickset.com<br/>
                </a>
                {data.parts.Count} x {data.BrickPrice.Min:F2}€ = {data.PartsPrice.Min:F2}€<br/>
                {data.parts.Count} x {data.BrickPrice.Avg:F2}€ = {data.PartsPrice.Avg:F2}€<br/>
                {data.parts.Count} x {data.BrickPrice.Max:F2}€ = {data.PartsPrice.Max:F2}€</td>
        </tr>
    </table>
</li>");
        }
    }

    internal unsafe sealed class JSONDocument
        : DocumentGenerator
    {
        private List<dynamic> cont = new List<dynamic>();
        private int missing;


        public override string Extension => ".json";

        public override void BeforeIteration(ref StringBuilder sb)
        {
        }

        public override void Iteration(ref StringBuilder sb, IterationData data) => cont.Add(new
        {
            DesignID = data.Part.DesignID,
            PartID = data.PartID,
            Material = data.parts.Matr,
            DEcoration = data.parts.Decor,
            UnitPrice = data.PartsPrice,
            Count = data.parts.Count,
            Thumbnail = data.PNG,
            Color = data.Color,
            Price = data.BrickPrice
        });

        public override void AfterIteration(ref StringBuilder sb, int missing) => this.missing = missing;

        public override string GenerateDocument(StringBuilder sb, Price total, int brickcount)
        {
            var data = new
            {
                Meta = new
                {
                    Path = Options["in"],
                    Name = Model?.name,
                    Version = $"{Model?.versionMajor ?? 0}.{Model?.versionMinor ?? 0}",
                    ToolVersion = Model?.Meta?.ToString(),
                    Timestamp = DateTime.Now.ToString("dd. MMM yyyy - HH:mm:ss.ffffff"),
                    Thumbnail = Thumbnail,
                },
                TotalPrice = total,
                PricePerBrick = new Price(total.Min / brickcount, total.Avg / brickcount, total.Max / brickcount),
                Partlist = cont.ToArray(),
                PartlistCount = Partlist.Length,
                BrickCount = brickcount,
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }

    internal unsafe sealed class XMLDocument
        : DocumentGenerator
    {
        JSONDocument doc = new JSONDocument();


        public override string Extension => ".xml";

        public override void AfterIteration(ref StringBuilder sb, int partcnt) => doc.AfterIteration(ref sb, partcnt);

        public override void BeforeIteration(ref StringBuilder sb) => doc.BeforeIteration(ref sb);

        public override string GenerateDocument(StringBuilder sb, Price total, int brickcount)
        {
            string json = doc.GenerateDocument(sb, total, brickcount);
            object obj = JsonConvert.DeserializeObject(json);
            XmlSerializer ser = new XmlSerializer(obj.GetType());

            using (MemoryStream ms = new MemoryStream())
            using (StreamReader sr = new StreamReader(ms))
            {
                ser.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);

                return sr.ReadToEnd();
            }
        }

        public override void Iteration(ref StringBuilder sb, IterationData data) => doc.Iteration(ref sb, data);
    }

    internal unsafe sealed class EXCELDocument
        : DocumentGenerator
    {
        private static readonly Missing missing = Missing.Value;
        private XApplication xapp;
        private Worksheet xsheet;
        private Workbook xbook;
        int partcount;
        int row;


        public override string Extension => HasOption("x-type", out string ext) ? ext.ToLower() : ".xls";


        ~EXCELDocument()
        {
            Marshal.ReleaseComObject(xsheet);
            Marshal.ReleaseComObject(xbook);
            Marshal.ReleaseComObject(xapp);
        }

        public EXCELDocument()
        {
            if ((xapp = new XApplication()) == null)
                throw new Exception("Microsoft Office Excel Tools 16.0 or higher has not been installed properly.");
            else
            {
                xbook = xapp.Workbooks.Add(missing);
                xsheet = xbook.Worksheets.Item[1] as Worksheet;
            }
        }

        public override void AfterIteration(ref StringBuilder sb, int partcnt) => partcount = partcnt;

        public override void BeforeIteration(ref StringBuilder sb)
        {
            row = 3;

            xsheet.Cells[row, 1] = "Count";
            xsheet.Cells[row, 2] = "Part ID";
            xsheet.Cells[row, 3] = "Design ID";
            xsheet.Cells[row, 4] = "Name";
            xsheet.Cells[row, 5] = "Color name";
            xsheet.Cells[row, 6] = "Color #RGB";
            xsheet.Cells[row, 7] = "Unit price (min)";
            xsheet.Cells[row, 8] = "Unit price (avg)";
            xsheet.Cells[row, 9] = "Unit price (max)";
            xsheet.Cells[row, 10] = "Price (min)";
            xsheet.Cells[row, 11] = "Price (avg)";
            xsheet.Cells[row, 12] = "Price (max)";
        }

        public override string GenerateDocument(StringBuilder sb, Price total, int brickcount)
        {
            row += 2;

            xsheet.Cells[row, 1] = "1";
            xsheet.Cells[row, 4] = Model?.name ?? "";
            xsheet.Cells[row, 10] = $"{total.Min:N2}€";
            xsheet.Cells[row, 11] = $"{total.Avg:N2}€";
            xsheet.Cells[row, 12] = $"{total.Max:N2}€";
            
            if (!HasOption("x-type", out string type))
                type = nameof(ExcelType.XLS);

            FileInfo tmp = new FileInfo($"{Directory.GetCurrentDirectory()}/{Guid.NewGuid():D}.{type.ToLower()}");

            xsheet.SaveAs(
                tmp.FullName,
                Enum.Parse(typeof(ExcelType), type, true),
                missing,
                missing,
                missing,
                missing,
                XlSaveAsAccessMode.xlExclusive,
                missing,
                missing,
                missing
            );
            xbook.Close(true, missing, missing);
            xapp.Quit();

            string cont;

            using (FileStream fs = tmp.OpenRead())
            using (StreamReader rd = new StreamReader(fs))
                cont = rd.ReadToEnd();

            tmp.Delete();

            return cont;
        }

        public override void Iteration(ref StringBuilder sb, IterationData data)
        {
            ++row;

            xsheet.Cells[row, 1] = data.parts.Count.ToString();
            xsheet.Cells[row, 2] = data.PartID.ToString();
            xsheet.Cells[row, 3] = data.DesignID.ToString();
            xsheet.Cells[row, 4] = data.Part?.Name;
            xsheet.Cells[row, 5] = data.Color?.Name;
            xsheet.Cells[row, 6] = data.Color?.RGB;
            xsheet.Cells[row, 7] = $"{data.BrickPrice.Min:N2}€";
            xsheet.Cells[row, 8] = $"{data.BrickPrice.Avg:N2}€";
            xsheet.Cells[row, 9] = $"{data.BrickPrice.Max:N2}€";
            xsheet.Cells[row, 10] = $"{data.PartsPrice.Min:N2}€";
            xsheet.Cells[row, 11] = $"{data.PartsPrice.Avg:N2}€";
            xsheet.Cells[row, 12] = $"{data.PartsPrice.Max:N2}€";
        }
    }

    internal struct Price
    {
        public float Min { set; get; }
        public float Avg { set; get; }
        public float Max { set; get; }

        public Price(float min, float avg, float max) =>
            (Min, Avg, Max) = (min, avg, max);
    }

    internal enum ExcelType
        : int
    {
        CSV = XlFileFormat.xlCSV,
        HTML = XlFileFormat.xlHtml,
        XLS = XlFileFormat.xlExcel8,
        XLSX = XlFileFormat.xlOpenXMLWorkbook,
    }
}
