using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
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

using Rectangle = System.Drawing.Rectangle;

namespace LXF
{
    using static ConsoleColor;
    using static Console;
    using static Math;

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
    --in=...                Input .LXF or .RAW file
    --raw                   Indicates that the input is a .RAW data file
    --out=...               Output document file
    --type=...              Output document type/format. Only the following values are supported:
           HTML   [default]
           JSON
           XML
           RAW              A pre-generated part list in a raw data format, which can be converted to other
                            formats
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

                        Dictionary<string, Type> gendic = new Dictionary<string, Type>
                        {
                            ["raw"] = typeof(RawDocument),
                            ["xml"] = typeof(XMLDocument),
                            ["html"] = typeof(HTMLDocument),
                            ["json"] = typeof(JSONDocument),
                            ["excel"] = typeof(ExcelDocument),
                        };

                        if (!hasopt("type", out string doctype) && !gendic.ContainsKey(doctype.ToLower()))
                            doctype = "html";
                        else
                            doctype = doctype.ToLower();
                        
                        using (DocumentGenerator gen = Activator.CreateInstance(gendic[doctype]) as DocumentGenerator)
                        {
                            FileInfo ifile = new FileInfo(@in);
                            FileInfo ofile;
                            string doc;

                            if (!hasopt("out", out string @out))
                                @out = $"{ifile.Directory.FullName}/{ifile.Name.Replace(ifile.Extension, "")}{gen.Extension}";

                            ofile = new FileInfo(@out);

                            gen.Options = opt;

                            if (hasopt("raw", out _))
                                using (FileStream fs = ifile.OpenRead())
                                using (StreamReader rd = new StreamReader(fs))
                                    doc = Generate(gen, rd.ReadToEnd().Unzip().Unzip());
                            else
                                using (ZipArchive lxf = ZipFile.Open(@in, ZipArchiveMode.Read))
                                using (Stream lxfms = lxf.GetEntry(PATH_LXFML).Open())
                                using (Stream pngms = lxf.GetEntry(PATH_PNG).Open())
                                using (Bitmap png = Image.FromStream(pngms) as Bitmap)
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    XmlSerializer xmlser = new XmlSerializer(typeof(LXFML));
                                    LXFML lxfml = xmlser.Deserialize(lxfms) as LXFML;

                                    png.Save(ms, ImageFormat.Png);

                                    gen.Model = lxfml;
                                    gen.Thumbnail = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";

                                    doc = Generate(gen, lxfml, db);
                                }

                            if (ofile.Exists)
                                ofile.Delete();

                            if (gendic[doctype] == typeof(ExcelDocument))
                                File.Move(doc, ofile.FullName);
                            else
                                using (FileStream fs = ofile.OpenWrite())
                                using (StreamWriter wr = new StreamWriter(fs, Encoding.UTF8))
                                    wr.Write(doc);

                            if (hasopt("open-after-success", out _))
                                Process.Start(ofile.FullName);
                        }
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
        
        private static string Generate(DocumentGenerator gen, LXFML lxfml, LEGODatabaseManager database)
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
            
            gen.Print = Print;

            #endregion

            gen.BeforeIteration(ref sb);

            foreach (var parts in partlist)
            {
                #region INIT

                BrickInfo part = database[parts.ID];
                BrickVariation bvar1 = part?.Variations?.FirstOrDefault(v => parts.Matr.Contains((short)v.ColorID) || v.PartID == parts.ID);
                BrickVariation bvar2 = bvar1 ?? part?.Variations?.FirstOrDefault();

                Price pprice = new Price(bvar2?.PriceMin ?? float.NaN, bvar2?.PriceAvg ?? float.NaN, bvar2?.PriceMax ?? float.NaN);
                Price tprice = new Price(parts.Count * pprice.Min, parts.Count * pprice.Avg, parts.Count * pprice.Max);

                string sel(Func<string> f1, Func<string> f2, Func<string> f3) => (bvar1 != null ? f1 : bvar2 != null ? f2 : f3)();

                #endregion
                #region COLORIZER + IMAGE

                ColorInfo color = database.GetColor(bvar1?.ColorID ?? parts.Matr.FirstOrDefault()) ?? database.GetColor(bvar2?.ColorID ?? 0);
                string rgbclr = color?.RGB ?? "transparent";
                string png = database.GetImageByPartID(bvar2?.PartID ?? parts.ID, src =>
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
                    IterationCount = partno,
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
        
        private static string Generate(DocumentGenerator gen, string rawjson)
        {
            RawDocument.RawData raw = JsonConvert.DeserializeObject<RawDocument.RawData>(rawjson);
            StringBuilder sb = new StringBuilder();

            gen.Model = raw.Model;
            gen.Thumbnail = raw.Thumbnail;
            gen.Print = (Action<string>)Serializer.Deserialize(Convert.FromBase64String(raw.PrintFunc));

            foreach ((string k, string v) in raw.Options)
                if (!gen.Options.ContainsKey(k))
                    gen.Options[k] = v;

            gen.BeforeIteration(ref sb);
            
            foreach (DocumentGenerator.IterationData dat in raw.Parts)
                gen.Iteration(ref sb, dat);

            gen.AfterIteration(ref sb, raw.PartCount);

            return gen.GenerateDocument(sb, raw.TotalPrice, raw.BrickCount);
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
        
        internal static string Zip(this string str)
        {
            using (MemoryStream msi = new MemoryStream(Encoding.Default.GetBytes(str)))
            using (MemoryStream mso = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(mso, CompressionMode.Compress))
                    msi.CopyTo(gz);

                return Encoding.Default.GetString(mso.ToArray());
            }
        }

        internal static string Unzip(this string str)
        {
            using (MemoryStream msi = new MemoryStream(Encoding.Default.GetBytes(str)))
            using (MemoryStream mso = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(msi, CompressionMode.Decompress))
                    gz.CopyTo(mso);

                return Encoding.Default.GetString(mso.ToArray());
            }
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

    public static class Serializer
    {
        public static byte[] Serialize(Delegate del)
        {
            BinaryFormatter bfm = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                bfm.Serialize(ms, new SerializeDelegate(del));

                return ms.ToArray();
            }
        }

        public static Delegate Deserialize(byte[] arr)
        {
            BinaryFormatter bfm = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream(arr))
            {
                object value = bfm.Deserialize(ms);

                if (value is SerializeDelegate del)
                    return del.Delegate;
                else
                    throw new InvalidOperationException();
            }
        }
    }

    [Serializable]
    public class SerializeDelegate
        : ISerializable
    {
        internal const string S_DELTYPE = "delegateType";
        internal const string S_CLSTYPE = "classType";
        internal const string S_ISSER = "isSerializable";
        internal const string S_DEL = "delegate";
        internal const string S_METH = "method";
        internal const string S_CLS = "class";

        public Delegate Delegate { get; }


        internal SerializeDelegate(Delegate func) => Delegate = func;

        internal SerializeDelegate(SerializationInfo info, StreamingContext context)
        {
            Type tp = info.GetValue(S_DELTYPE, typeof(Type)) as Type;

            if (info.GetBoolean(S_ISSER))
                Delegate = info.GetValue(S_DEL, tp) as Delegate;
            else
            {
                MethodInfo method = info.GetValue(S_METH, typeof(MethodInfo)) as MethodInfo;
                AnonymousClassWrapper w = info.GetValue(S_CLS, typeof(AnonymousClassWrapper)) as AnonymousClassWrapper;

                Delegate = Delegate.CreateDelegate(tp, w.Value, method);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(S_DELTYPE, Delegate.GetType());

            if ((Delegate.Target is null || Delegate.Method.DeclaringType.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0) && Delegate != null)
            {
                info.AddValue(S_ISSER, true);
                info.AddValue(S_DEL, Delegate);
            }
            else
            {
                info.AddValue(S_ISSER, false);
                info.AddValue(S_METH, Delegate.Method);
                info.AddValue(S_CLS, new AnonymousClassWrapper(Delegate.Method.DeclaringType, Delegate.Target));
            }
        }

        [Serializable]
        internal class AnonymousClassWrapper
            : ISerializable
        {
            public object Value { get; }
            public Type Type { get; }


            internal AnonymousClassWrapper(Type bclass, object bobject) => (Type, Value) = (bclass, bobject);

            internal AnonymousClassWrapper(SerializationInfo info, StreamingContext context)
            {
                Type c_tp = info.GetValue(S_CLSTYPE, typeof(Type)) as Type;

                Value = Activator.CreateInstance(c_tp);

                foreach (FieldInfo nfo in c_tp.GetFields())
                    if (typeof(Delegate).IsAssignableFrom(nfo.FieldType))
                        nfo.SetValue(Value, ((SerializeDelegate)info.GetValue(nfo.Name, typeof(SerializeDelegate))).Delegate);
                    else if (!nfo.FieldType.IsSerializable)
                        nfo.SetValue(Value, ((AnonymousClassWrapper)info.GetValue(nfo.Name, typeof(AnonymousClassWrapper))).Value);
                    else
                        nfo.SetValue(Value, info.GetValue(nfo.Name, nfo.FieldType));
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(S_CLSTYPE, Type);

                foreach (FieldInfo field in Type.GetFields())
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                        info.AddValue(field.Name, new SerializeDelegate((Delegate)field.GetValue(Value)));
                    else if (!field.FieldType.IsSerializable)
                        info.AddValue(field.Name, new AnonymousClassWrapper(field.FieldType, field.GetValue(Value)));
                    else
                        info.AddValue(field.Name, field.GetValue(Value));
            }
        }
    }

    [Serializable]
    public struct Price
    {
        public float Min { set; get; }
        public float Avg { set; get; }
        public float Max { set; get; }

        public Price(float min, float avg, float max) =>
            (Min, Avg, Max) = (min, avg, max);
    }
}
