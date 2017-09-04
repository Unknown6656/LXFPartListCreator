using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Globalization;
using System.Diagnostics;
using System.Xml.Schema;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

using CsQuery;

namespace LXFPartListCreator
{
    public abstract class LEGODatabaseProvider
        : IDisposable
    {
        public const string FILE_INDEX = "index.db";
        public const string FILE_IMAGE = "part-{0}.dat";

        protected readonly FileInfo dbnfo;
        protected readonly DirectoryInfo dbdir;


        protected bool IsDisposed { private set; get; } = false;

        public abstract BrickInfo this[int ID] { internal set; get; }

        public abstract string Name { get; }

        //expiration in seconds
        public abstract long CacheExpriration { get; }


        public abstract ColorInfo GetColor(int ID);

        public abstract string GetImageByPartID(int? partID);

        public abstract string GetImageByDesignID(int designID, int colorID = -1);

        protected abstract void InternalDispose();

        public abstract void Save();

        protected internal abstract void Load();

        protected internal abstract void LoadMerge();

        public virtual void ClearIndex() => Exec(() =>
        {
            if (dbnfo.Exists)
                dbnfo.Delete();
        });

        public virtual void ClearImages() => Exec(() =>
        {
            foreach (FileInfo img in dbdir.EnumerateFiles(string.Format(FILE_IMAGE, '*')))
                if (img.Exists)
                    img.Delete();
        });

        public void ClearAll() => Exec(() =>
        {
            ClearIndex();
            ClearImages();
        });

        public void Dispose()
        {
            if (!IsDisposed)
            {
                InternalDispose();
                Save();
            }

            IsDisposed = true;
        }

        protected T Exec<T>(Func<T> f)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(BricksetDotCom));
            else
                return f();
        }

        protected void Exec(Action f)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(BricksetDotCom));
            else
                f();
        }

        public override string ToString() => Name;

        public LEGODatabaseProvider(string dir)
        {
            dbdir = new DirectoryInfo(dir);

            if (!dbdir.Exists)
                dbdir.Create();

            dbnfo = new FileInfo($"{dbdir.FullName}/{FILE_INDEX}");
        }
    }

    public sealed class LEGODatabaseManager
        : LEGODatabaseProvider
    {
        private readonly List<LEGODatabaseProvider> providers = new List<LEGODatabaseProvider>();

        public override string Name => "Database manager";


        public override BrickInfo this[int ID]
        {
            get
            {
                BrickInfo nfo = Each(p => p[ID]);

                this[ID] = nfo;
                
                return nfo;
            }
            internal set => Each(p => p[ID] = value);
        }

        public LEGODatabaseProvider[] Providers => providers.ToArray();

        public override long CacheExpriration => long.MaxValue;

        public override ColorInfo GetColor(int ID) => Each(p => p.GetColor(ID));

        public override string GetImageByPartID(int? partID) => Each(p => p.GetImageByPartID(partID));

        public override string GetImageByDesignID(int designID, int colorID = -1) => Each(p => p.GetImageByDesignID(designID, colorID));

        protected override void InternalDispose() => Each(p => p.Dispose());

        public override void Save() => Each(p =>
        {
            p.LoadMerge();
            p.Save();
        });

        protected internal override void Load() => Each(p => p.Load());

        protected internal override void LoadMerge() => Each(p => p.LoadMerge());

        private void Each(Action<LEGODatabaseProvider> f) => Exec(() =>
        {
            foreach (LEGODatabaseProvider prv in providers)
                f(prv);
        });

        private T Each<T>(Func<LEGODatabaseProvider, T> f)
            where T : class => Exec(() =>
            {
                T res = null;

                foreach (LEGODatabaseProvider prv in providers)
                    try
                    {
                        if ((res = f(prv)) != null)
                            break;
                    }
                    catch
                    {
                    }

                return res;
            });

        public void AddProvider(LEGODatabaseProvider provider) => Exec(() =>
        {
            if (provider != null)
                providers.Add(provider);
        });

        public void AddProvider<T>()
            where T : LEGODatabaseProvider => AddProvider(Activator.CreateInstance(typeof(T), dbdir.FullName) as LEGODatabaseProvider);

        public LEGODatabaseManager(string dir)
            : base(dir)
        {
        }
    }

    // implementation for http://brickset.com/
    public sealed class BricksetDotCom
        : LEGODatabaseProvider
    {
        public const string URL_DESIGN = "https://brickset.com/parts/design-{0}";
        public const string URL_PART = "https://brickset.com/parts/{0}/";
        public const string URL_BUY = "https://brickset.com/ajax/parts/buy?partID={0}";
        public const long CACHE_EXP = 60 * 60 * 12;

        internal static readonly XmlSerializer ser = new XmlSerializer(typeof(BrickDB));

        private Dictionary<int, ColorInfo> colors = new Dictionary<int, ColorInfo>();
        private Dictionary<int, BrickInfo> bricks = new Dictionary<int, BrickInfo>();
        private WebClient wc = new WebClient();


        public override BrickInfo this[int ID]
        {
            get
            {
                if (ID > 99999)
                    ID = GetDesignID(ID);

                if (!bricks.ContainsKey(ID))
                    AddUpdate(ID);

                return bricks[ID];
            }
            internal set
            {
                if (ID > 99999)
                    ID = GetDesignID(ID);

                bricks[ID] = value;

                Save();
            }
        }

        public override string Name => "brickset.com";

        public override long CacheExpriration => CACHE_EXP;

        public override ColorInfo GetColor(int ID) => Exec(() => colors.ContainsKey(ID) ? colors[ID] : null);

        public override string GetImageByDesignID(int designID, int colorID = -1) => Exec(() =>
        {
            BrickInfo nfo = this[designID];
            BrickVariation v = nfo?.Variations?.FirstOrDefault(var => var?.ColorID == colorID) ?? nfo?.Variations?.FirstOrDefault();

            return GetImageByPartID(v?.PartID);
        });

        public override string GetImageByPartID(int? partID) => Exec(() =>
        {
            if (partID is int id)
            {
                if (id <= 99999)
                    id = this[id]?.Variations?.FirstOrDefault()?.PartID ?? -1;
                
                return id < 0 ? "" : ImgGetB64(string.Format(dbdir.FullName + '/' + FILE_IMAGE, id));
            }
            else
                return "";
        });

        private int GetDesignID(int partID)
        {
            var pids = from b in bricks
                       where b.Value.Variations?.Any(v => v?.PartID == partID) ?? false
                       select b.Key;

            if (pids.Any())
                return pids.First();
            else
            {
                CQ dom = DownloadString(string.Format(URL_PART, partID));
                var id = dom["section.main img.partimage[alt]"][0]["alt"];

                return int.Parse(id);
            }
        }

        private void AddUpdate(int designID)
        {
            try
            {
                Console.WriteLine($"Fetching design No.{designID} ...");

                string html = DownloadString(string.Format(URL_DESIGN, designID));
                CQ dom = html;

                if (html is null)
                    Debugger.Break();

                var cs_iteminfo = "section.main div.iteminfo";

                var iteminfo = dom[cs_iteminfo];
                var col2 = dom[cs_iteminfo + " div.col"].ToList()?[1].Cq();
                var name = col2.Find("h1")[0].InnerHTML + "<br/>";
                var table = col2.Find("dd").ToList();
                var vars_raw = dom["section.main div.partlist > ul li.item"].ToList().ToArray() ?? new IDomObject[0];
                BrickVariation[] vars;

                name = name.Remove(name.ToLower().IndexOf("<br"));

                this[designID] = new BrickInfo
                {
                    Name = name,
                    DesignID = designID,
                    ProductionDate = GetYear(table[2].TextContent),
                    Variations = vars = FetchVariations(vars_raw),
                    FetchDate = (vars_raw.Length - vars.Length) > 0 ? -1 : DateTime.Now.Ticks,
                };
            }
            catch (Exception ex)
            {
                ex.Err();

                if (bricks.ContainsKey(designID))
                    bricks.Remove(designID);
            }
        }

        private BrickVariation[] FetchVariations(IDomObject[] variations)
        {
            var vlist = new List<BrickVariation>();

            foreach (var variation in variations)
                try
                {
                    string parturl = variation.Cq().Find("ul li a[href] img[title]").ToList()[0]["src"];
                    int partid = parturl.match(@"\/(?<id>[0-9]+)\.(jpe?g|(d|p)ng|bmp|gif|tiff)$", out Match m) ? int.Parse(m.Groups["id"].ToString()) : -1;

                    if (partid != -1)
                    {
                        Console.WriteLine($"Fetching part No.{partid} ...");

                        DownloadImage(parturl, partid);

                        CQ dom = DownloadString(string.Format(URL_PART, partid), partid);
                        CQ buy = DownloadString(string.Format(URL_BUY + "&_=" + DateTime.Now.Ticks, partid), partid);
                        var table = dom["section.featurebox div.text"].Find("dl dd").ToList();
                        var prices = from elem in buy["span.price a"].ToList()
                                     where elem.InnerTextAllowed
                                     let text = elem.InnerText
                                     where text.match(@"[0-9]+\.[0-9]{2}", out m)
                                     select float.Parse(m.ToString());
                        int colid;

                        vlist.Add(new BrickVariation
                        {
                            PartID = partid,
                            ColorID = colid = int.Parse(table[10].TextContent),
                            ProductionDate = GetYear(table[5].TextContent),
                            PriceAvg = prices.Any() ? prices.Average() : float.NaN,
                        });

                        AddUpdateColor(table);
                    }
                }
                catch (Exception ex)
                {
                    ex.Err();
                }

            return vlist.OrderBy(v => v.ColorID).ToArray();
        }

        private static string ImgGetB64(string path)
        {
            using (Bitmap bmp = Image.FromFile(path) as Bitmap)
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);

                return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
            }
        }

        private static int[] GetYear(string str) => str.Split('-', ',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToArray();

        private void AddUpdateColor(List<IDomObject> ctable)
        {
            string txt(int ndx) => ctable[ndx].TextContent.Trim();
            int colid = int.Parse(txt(10));

            if (!colors.ContainsKey(colid))
                colors[colid] = new ColorInfo
                {
                    FetchDate = DateTime.Now.Ticks,
                    Family = txt(6),
                    Name = txt(7),
                    RGB = txt(8),
                    Type = txt(9),
                    ID = colid,
                };
        }

        private void DownloadImage(string uri, int partID)
        {
            FileInfo path = new FileInfo(dbdir.FullName + '/' + string.Format(FILE_IMAGE, partID));

            if (!path.Exists)
                DownloadFile(uri.Replace("/1/", "/2/"), path.FullName);
        }

        protected override void InternalDispose() => wc?.Dispose();

        public override void Save()
        {
            if (dbnfo.Exists)
                dbnfo.Delete();

            using (FileStream fs = dbnfo.Create())
                ser.Serialize(fs, new BrickDB
                {
                    Bricks = bricks.Values.OrderBy(b => b?.DesignID).ToArray(),
                    Colors = colors.Values.OrderBy(c => c?.ID).ToArray(),
                });
        }

        public override void ClearIndex()
        {
            base.ClearIndex();

            bricks.Clear();
            colors.Clear();
        }

        protected internal override void Load()
        {
            if (dbnfo.Exists)
            {
                var b = new Dictionary<int, BrickInfo>(bricks);
                var c = new Dictionary<int, ColorInfo>(colors);

                bricks.Clear();
                colors.Clear();

                try
                {
                    LoadMerge();
                }
                catch (Exception ex)
                {
                    ex.Err();

                    bricks = b;
                    colors = c;
                }
            }
        }

        protected internal override void LoadMerge()
        {
            using (FileStream fs = dbnfo.OpenRead())
            {
                BrickDB db = ser.Deserialize(fs) as BrickDB;

                foreach (BrickInfo b in db?.Bricks ?? new BrickInfo[0])
                    if (b != null)
                        if (bricks.ContainsKey(b.DesignID))
                        {
                            if (b.FetchDate > bricks[b.DesignID].FetchDate)
                                bricks[b.DesignID] = b;
                        }
                        else
                            bricks[b.DesignID] = b;

                foreach (ColorInfo c in db.Colors ?? new ColorInfo[0])
                    if (c != null)
                        colors[c.ID] = c;

                long now = DateTime.Now.AddSeconds(-CacheExpriration).Ticks;

                foreach (int id in bricks.Keys)
                    if (bricks[id].FetchDate < now)
                        AddUpdate(id);

                foreach (int id in colors.Keys)
                    if (colors[id].FetchDate < now)
                        AddUpdate(id);
            }
        }

        private void SetHeaders(int id = -1)
        {
            wc.Headers.Add("authority", "brickset.com:443");
            wc.Headers.Add("x-requested-with", "XMLHttpRequest");
            wc.Headers[HttpRequestHeader.Host] = $"brickset.com:443";
            wc.Headers[HttpRequestHeader.Referer] = $"https://brickset.com/parts/{id}/";
            wc.Headers[HttpRequestHeader.Cookie] = @"
PreferredCountry2=CountryCode=DE&CountryName=Germany;
ActualCountry=CountryCode=DE&CountryName=Germany;
setsPageLength=200;
buyPageLength=200;
partsPageLength=200;
setsListFormat=List;
setsSortOrder=DERetailPrice;
buySortOrder=Price;
cookieconsent_dismissed=yes;
partsSortOrder=setcount;
partsListFormat=Images;
".Replace('\n', ' ').Replace('\r', ' ').Trim(); // yeah, cookies!
        }

        private T TryWebAction<T>(Func<T> f, int count = 3, int timeout = 9000)
        {
            WebException last = null;
            int @try = 0;

            while (@try < count)
                try
                {
                    if (@try == count - 1)
                    {
                        wc.Dispose();
                        wc = new WebClient(); // reset web client;
                    }

                    return f();
                }
                catch (WebException ex)
                {
                    int code = (int)(ex.Response as HttpWebResponse).StatusCode;

                    if ((code == 400) ||
                        (code == 401) ||
                        (code >= 500))
                    {
                        Console.WriteLine($"   attempt no.{++@try}");
                        Thread.Sleep(timeout / count);

                        last = ex;
                    }
                    else
                        throw;
                }

            Thread.Sleep(30000); // "cool down" web polls ... I should prob. do smth. better than 'thread::sleep' -__-

            wc.Dispose();
            wc = new WebClient(); // reset web client;

            return last is null ? default(T) : throw last;
        }

        private void DownloadFile(string url, string path) => TryWebAction(() =>
        {
            wc.DownloadFile(url, path);

            return null as object;
        });

        private string DownloadString(string url, int id = -1) => TryWebAction(() =>
        {
            SetHeaders(id);

            return wc.DownloadString(url);
        });

        public BricksetDotCom(string dir) : base(dir) => Load();
    }


    [Serializable, XmlType(AnonymousType = true), XmlRoot(Namespace = "", IsNullable = false)]
    public sealed partial class BrickDB
    {
        [XmlElement]
        public ColorInfo[] Colors { set; get; }
        [XmlElement]
        public BrickInfo[] Bricks { set; get; }
    }

    [Serializable, XmlType(AnonymousType = true)]
    public sealed partial class ColorInfo
    {
        [XmlAttribute]
        public long FetchDate { set; get; }

        [XmlAttribute]
        public int ID { set; get; }
        [XmlAttribute]
        public string Name { set; get; }
        [XmlAttribute]
        public string Family { set; get; }
        [XmlAttribute]
        public string Type { set; get; }
        [XmlAttribute]
        public string RGB { set; get; }
        [XmlIgnore]
        public float[] RGBAValues
        {
            get
            {
                if (RGB.match(@"\#(?<r>[0-9a-f]{2})(?<g>[0-9a-f]{2})(?<b>[0-9a-f]{2})(?<a>[0-9a-f]{2})?", out Match m))
                    return new float[] { getfloat("r"), getfloat("g"), getfloat("b"), getfloat("a", 1) };
                else
                    return new float[] { float.NaN, float.NaN, float.NaN, 1 };
                

                float getfloat(string group, float def = float.NaN) =>
                    m.Groups[group].Success ? int.Parse(m.Groups[group].ToString(), NumberStyles.HexNumber) / 255f : def;
            }
        }
    }

    [Serializable, XmlType(AnonymousType = true)]
    public sealed partial class BrickInfo
    {
        [XmlAttribute]
        public long FetchDate { set; get; }

        [XmlAttribute]
        public int DesignID { set; get; }
        [XmlAttribute]
        public string Name { set; get; }
        [XmlAttribute]
        public string Category { set; get; }
        [XmlElement]
        public int[] ProductionDate { set; get; }
        [XmlElement]
        public BrickVariation[] Variations { set; get; }
        [XmlIgnore]
        public float PriceAvg => Variations?.Select(_ => _.PriceAvg)?.Average() ?? float.NaN;
    }

    [Serializable, XmlType(AnonymousType = true)]
    public sealed partial class BrickVariation
    {
        [XmlElement]
        public int[] ProductionDate { set; get; }
        [XmlAttribute]
        public int PartID { set; get; }
        [XmlAttribute]
        public int ColorID { set; get; }
        [XmlAttribute]
        public float PriceAvg { set; get; }

        public override string ToString() => $"Color/ID: {ColorID}/{PartID}  (~{PriceAvg}€)";
    }
}
