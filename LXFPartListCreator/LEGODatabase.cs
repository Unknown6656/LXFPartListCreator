// #define USE_OLD_BRICKOWL_IMPL

using System.Text.RegularExpressions;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

using CsQuery;

namespace LXF
{
    public interface ILEGODatabase
        : IDisposable
    {
        string Name { get; }
        bool CanOperate { get; }
        bool IsDisposed { get; }
        long CacheExpriration { get; } // expiration in seconds
        LEGODatabaseManager Manager { get; }

        void Load();
        void Save();
        void ClearAll();
    }

    public interface ILEGOPriceDatabase
        : ILEGODatabase
    {
        void UpdatePrice(int ID, bool force = false);
    }

    public interface ILEGOBrickDatabase
        : ILEGODatabase
    {
        BrickInfo this[int ID] { get; }
    }

    public interface ILEGOColorDatabase
        : ILEGODatabase
    {
        ColorInfo GetColor(int ID);
    }

    public interface ILEGOImageDatabase
        : ILEGODatabase
    {
        string GetImageByPartID(int? partID, Func<Bitmap, Bitmap> postproc2 = null);
        string GetImageByDesignID(int designID, int colorID = -1, Func<Bitmap, Bitmap> postproc2 = null);

        void ClearImages();
    }

    public abstract class Debuggable
    {
        public abstract string Name { get; }

        protected void Print(string msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.ffffff}][{Name}] {msg}");
    }

    public abstract class LEGODatabaseProvider
        : Debuggable
        , ILEGOBrickDatabase
        , ILEGOColorDatabase
        , ILEGOImageDatabase
    {
        public const string FILE_INDEX = "index.db";
        public const string FILE_IMAGE = "part-{0}.dat";

        protected readonly FileInfo dbnfo;
        protected readonly DirectoryInfo dbdir;


        public bool IsDisposed { private set; get; } = false;

        public LEGODatabaseManager Manager { internal set; get; }

        public abstract BrickInfo this[int ID] { internal set; get; }

        public abstract long CacheExpriration { get; }

        public abstract bool CanOperate { get; }

        public abstract ColorInfo GetColor(int ID);

        public abstract string GetImageByPartID(int? partID, Func<Bitmap, Bitmap> postproc2 = null);

        public abstract string GetImageByDesignID(int designID, int colorID = -1, Func<Bitmap, Bitmap> postproc2 = null);

        protected abstract void InternalDispose();

        public abstract void Save();

        public abstract void Load();

        public abstract void LoadMerge();

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
                throw new ObjectDisposedException(nameof(LEGODatabaseProvider));
            else
                return f();
        }

        protected void Exec(Action f)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(LEGODatabaseProvider));
            else
                f();
        }

        public override string ToString() => Name;

        public static bool IsPartID(int ID) => ID > 99999;

        public LEGODatabaseProvider(string dir, LEGODatabaseManager manager)
        {
            Manager = manager;
            dbdir = new DirectoryInfo(dir);

            if (!dbdir.Exists)
                dbdir.Create();

            dbnfo = new FileInfo($"{dbdir.FullName}/{FILE_INDEX}");
        }
    }
    
    public sealed class LEGODatabaseManager
        : LEGODatabaseProvider
    {
        private WebClient wc = new WebClient();


        internal WebClient WebClient => wc;

        internal void ResetWebClient()
        {
            wc.Dispose();
            wc = new WebClient();
        }
        
        internal void SetBricksetHeaders(int id = -1)
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

        public ILEGODatabase this[ILEGODatabase prov] => this[prov.GetType()];

        public ILEGODatabase this[Type prov] => providers.FirstOrDefault(p => prov.IsAssignableFrom(p.GetType()));

        private readonly List<ILEGODatabase> providers = new List<ILEGODatabase>();

        public override string Name => "Database manager";

        public override BrickInfo this[int ID]
        {
            get
            {
                Each<ILEGOPriceDatabase>(p =>
                {
                    p.UpdatePrice(ID);
                    p.Save();
                });

                BrickInfo nfo = EachF(p => p[ID]);

                this[ID] = nfo;
                
                return nfo;
            }
            internal set => EachF(p => p[ID] = value);
        }

        public ILEGODatabase[] Providers => providers.ToArray();

        public override long CacheExpriration => long.MaxValue;

        public override bool CanOperate
        {
            get
            {
                bool co = true;

                foreach (ILEGODatabase prov in providers)
                    co &= prov?.CanOperate ?? true;

                return co;
            }
        }

        public ILEGODatabase GetProvider(string name) => GetProviders(name).FirstOrDefault();

        public ILEGODatabase[] GetProviders(string name) => providers.Where(p => p?.Name == name).ToArray();

        public T GetProvider<T>()
            where T : ILEGODatabase => GetProviders<T>().FirstOrDefault();

        public T[] GetProviders<T>()
            where T : ILEGODatabase => providers.Where(p => p is T).Select(p => (T)p).ToArray();

        public override ColorInfo GetColor(int ID) => EachF(p => p.GetColor(ID));

        public override string GetImageByPartID(int? partID, Func<Bitmap, Bitmap> postproc2 = null) => EachF(p => p.GetImageByPartID(partID, postproc2));

        public override string GetImageByDesignID(int designID, int colorID = -1, Func<Bitmap, Bitmap> postproc2 = null) => EachF(p => p.GetImageByDesignID(designID, colorID, postproc2));

        protected override void InternalDispose()
        {
            Each(p => p.Dispose());

            wc.Dispose();
            wc = null;
        }

        public override void Save() => Each(p =>
        {
            p.LoadMerge();
            p.Save();
        });

        public override void Load() => Each(p => p.Load());

        public override void LoadMerge() => Each(p => p.LoadMerge());

        public void UpdatePrice(int ID) => Each<ILEGOPriceDatabase>(p => p.UpdatePrice(ID));

        private T EachF<P, T>(Func<P, T> f)
            where P : ILEGODatabase
            where T : class => Exec(() =>
            {
                T res = null;

                foreach (ILEGODatabase dat in providers)
                    if (dat is P prv)
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

        private void Each<P>(Action<P> f)
            where P : ILEGODatabase => Exec(() =>
            {
                foreach (ILEGODatabase dat in providers)
                    if (dat is P prv)
                        f(prv);
            });

        private T EachF<T>(Func<LEGODatabaseProvider, T> f)
            where T : class => EachF<LEGODatabaseProvider, T>(f);

        private void Each(Action<LEGODatabaseProvider> f) => Each<LEGODatabaseProvider>(f);

        public ILEGODatabase AddProvider(ILEGODatabase provider) => Exec(() =>
        {
            if (provider != null)
                if (!providers.Contains(provider))
                {
                    providers.Add(provider);

                    try
                    {
                        provider.GetType().GetProperty(nameof(ILEGODatabase.Manager)).SetValue(provider, provider.Manager);
                    }
                    catch
                    {
                    }

                    return provider;
                }

            return null;
        });

        public T AddProvider<T>()
            where T : class, ILEGODatabase => AddProvider(Activator.CreateInstance(typeof(T), dbdir.FullName, this) as ILEGODatabase) as T;

        public LEGODatabaseManager(string dir)
            : base(dir, null) => Print("Manager initialized");
    }

    // implementation for http://brickset.com/
    public sealed class BricksetDotCom
        : LEGODatabaseProvider
    {
        public const string URL_BROWL = "https://api.brickowl.com/v1/{1}?key={0}&{2}";
        public const string URL_DESIGN = "https://brickset.com/parts/design-{0}";
        public const string URL_PART = "https://brickset.com/parts/{0}/";
        public const string URL_BUY = "https://brickset.com/ajax/parts/buy?partID={0}";
        public const long CACHE_EXP = 60 * 60 * 24 * 14; // update once every two weeks

        internal static readonly XmlSerializer ser = new XmlSerializer(typeof(BrickDB));

        internal Dictionary<int, ColorInfo> colors = new Dictionary<int, ColorInfo>();
        internal Dictionary<int, BrickInfo> bricks = new Dictionary<int, BrickInfo>();


        public Func<Bitmap, Bitmap> BitmapPreprocessor { set; private get; }
        public Func<Bitmap, Bitmap> BitmapPostprocessor { set; private get; }

        public override BrickInfo this[int ID]
        {
            get
            {
                if (IsPartID(ID))
                    ID = GetDesignID(ID);

                if (!bricks.ContainsKey(ID))
                    AddUpdate(ID);

                return bricks.ContainsKey(ID) ? bricks[ID] : null;
            }
            internal set
            {
                if (IsPartID(ID))
                    ID = GetDesignID(ID);

                bricks[ID] = value;

                Save();
            }
        }

        public BrickowlDotCom Brickowl => Manager[typeof(BrickowlDotCom)] as BrickowlDotCom;

        public override string Name => "brickset.com";

        public override long CacheExpriration => CACHE_EXP;

        public override bool CanOperate
        {
            get
            {
                using (Ping p = new Ping())
                    return p.Send(Name).Status == IPStatus.Success;
            }
        }

        public ColorInfo FindColor(string input) => Exec(() =>
        {
            int hamming(string s1, string s2) => s1.Zip(s2, (l, r) => l - r == 0 ? 0 : 1).Sum();
            int levenshtein(string s, string t)
            {
                int n = s.Length;
                int m = t.Length;
                int[,] d = new int[n + 1, m + 1];

                if (n == 0)
                    return m;
                else if (m == 0)
                    return n;

                for (int i = 0; i <= n; d[i, 0] = i++) ;
                for (int j = 0; j <= m; d[0, j] = j++) ;

                for (int i = 1; i <= n; i++)
                    for (int j = 1; j <= m; j++)
                    {
                        int cost = t[j - 1] == s[i - 1] ? 0 : 1;

                        d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    }

                return d[n, m];
            }

            ColorInfo clr = colors.Values.FirstOrDefault(c => input.Equals(c.Name, StringComparison.InvariantCultureIgnoreCase));
            string[] frags = input.Split(' ');

            return clr ?? (from c in colors.Values
                           let name = c?.Name
                           where name != null
                           let nfrags = name.Split(' ')
                           where nfrags.Length == frags.Length
                           let comp = nfrags.Length == frags.Length ? (from i in Enumerable.Range(0, nfrags.Length)
                                                                       let n_i = nfrags[i]
                                                                       let c_i = frags[i]
                                                                       let ham = hamming(n_i, c_i)
                                                                       where ham < (n_i.Length + 1) / 2
                                                                       where ham < (c_i.Length + 1) / 2
                                                                       let lev = levenshtein(n_i, c_i)
                                                                       where lev < (n_i.Length + 1) / 2
                                                                       where lev < (c_i.Length + 1) / 2
                                                                       select new
                                                                       {
                                                                           Hamming = ham,
                                                                           Levenshtein = lev
                                                                       }).ToArray() : new[] {
                                                                           new
                                                                           {
                                                                               Hamming = hamming(name, input),
                                                                               Levenshtein = levenshtein(name, input)
                                                                           }
                                                                       }
                           where nfrags.Length == frags.Length ? comp.Length == frags.Length : (comp[0].Hamming < name.Length / 3) &&
                                                                                               (comp[0].Hamming < input.Length / 3) &&
                                                                                               (comp[0].Levenshtein < name.Length / 3) &&
                                                                                               (comp[0].Levenshtein < input.Length / 3)
                           let dist = Math.Sqrt(Math.Pow(comp.Sum(c => c.Hamming), 2) + Math.Pow(comp.Sum(c => c.Levenshtein), 2))
                           orderby dist ascending
                           // orderby ham ascending
                           select new
                           {
                               // Hamming = ham,
                               // Levenshtein = lev,
                               Color = c
                           }).FirstOrDefault()?.Color;
        });

        public override ColorInfo GetColor(int ID) => Exec(() => colors.ContainsKey(ID) ? colors[ID] : null);

        public override string GetImageByDesignID(int designID, int colorID = -1, Func<Bitmap, Bitmap> postproc2 = null) => Exec(() =>
        {
            BrickInfo nfo = this[designID];
            BrickVariation v = nfo?.Variations?.FirstOrDefault(var => var?.ColorID == colorID) ?? nfo?.Variations?.FirstOrDefault();

            return GetImageByPartID(v?.PartID, postproc2);
        });

        public override string GetImageByPartID(int? partID, Func<Bitmap, Bitmap> postproc2 = null) => Exec(() =>
        {
            if (partID is int id)
            {
                if (!IsPartID(id))
                {
                    int nid = this[id]?.Variations?.FirstOrDefault()?.PartID ?? -1;

                    if (nid == -1)
                    {
                        BrickInfo brick = Brickowl.FetchBrick(id);

                        if (brick != null)
                            this[brick.DesignID] = brick;

                        id = brick?.Variations?.FirstOrDefault()?.PartID ?? -1;
                    }
                    else
                        id = nid;
                }

                string path = string.Format(dbdir.FullName + '/' + FILE_IMAGE, id);
                string b64 = File.Exists(path) ? ImgGetB64(path, postproc2) : null;

                if (string.IsNullOrWhiteSpace(b64))
                {
                    AddUpdate(GetDesignID(id));

                    return GetImageByDesignID(id, postproc2: postproc2);
                }

                return id < 0 ? "" : b64;
            }
            else
                return "";
        });

        internal int GetDesignID(int partID)
        {
            if (IsPartID(partID))
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
            else
                return partID;
        }

        private void AddUpdate(int designID)
        {
            try
            {
                BrickInfo bi;

                if (designID == -1)
                    bi = Brickowl?.FetchBrick(designID);
                else
                {
                    Print($"Fetching design No.{designID} ...");

                    string html = DownloadString(string.Format(URL_DESIGN, designID));
                    CQ dom = html;
                    
                    var cs_iteminfo = "section.main div.iteminfo";
                    var iteminfo = dom[cs_iteminfo];
                    var col2 = dom[cs_iteminfo + " div.col"].ToList()?[1].Cq();
                    var name = col2.Find("h1")[0].InnerHTML + "<br/>";
                    var table = col2.Find("dd").ToList();
                    var vars_raw = dom["section.main div.partlist > ul li.item"].ToList().ToArray() ?? new IDomObject[0];
                    BrickVariation[] vars;

                    name = name.Remove(name.ToLower().IndexOf("<br"));
                    
                    bi = string.IsNullOrWhiteSpace(name) ? Brickowl?.FetchBrick(designID) : new BrickInfo
                    {
                        Name = name,
                        DesignID = designID,
                        ProductionDate = GetYear(table[2].TextContent),
                        Variations = vars = FetchVariations(vars_raw),
                        FetchDate = (vars_raw.Length - vars.Length) > 0 ? -1 : DateTime.Now.Ticks,
                    };
                }

                if (bi != null)
                {
                    if (bi.DesignID > 0)
                        this[bi.DesignID] = bi;

                    Manager.GetProvider<BrickowlDotCom>()?.UpdatePrice(bi.DesignID);
                }
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
                        Print($"Fetching part No.{partid} ...");

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
                            PriceMin = prices.Any() ? prices.Min() : float.NaN,
                            PriceAvg = prices.Any() ? prices.Average() : float.NaN,
                            PriceMax = prices.Any() ? prices.Max() : float.NaN,
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

        private string ImgGetB64(string path, Func<Bitmap, Bitmap> postproc2 = null)
        {
            using (Bitmap bmp = Image.FromFile(path) as Bitmap)
            using (MemoryStream ms = new MemoryStream())
            {
                if ((bmp.Width < 192) && (bmp.Height < 192))
                {
                    bmp.Dispose();
                    ms.Dispose();

                    File.Delete(path);

                    path.match(@"part\-(?<id>[0-9]+)\.dat", out Match m);

                    int pid = int.Parse(m.Groups["id"].ToString());
                    int did = GetDesignID(pid);

                    AddUpdate(did);

                    return ImgGetB64(path, postproc2);
                }

                Bitmap res = BitmapPostprocessor?.Invoke(bmp) ?? bmp;
                
                using (res = postproc2?.Invoke(res) ?? res)
                    res.Save(ms, ImageFormat.Png);

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

        internal void DownloadImage(string uri, int partID)
        {
            Print($"Fetching image for {partID}...");

            FileInfo path = new FileInfo(dbdir.FullName + '/' + string.Format(FILE_IMAGE, partID));

            if (!path.Exists)
            {
                DownloadFile(uri.Replace("/1/", "/2/"), path.FullName);

                string tmp = $"{path.FullName}/../{Guid.NewGuid():D}.tmp";

                using (Bitmap src = Image.FromFile(path.FullName) as Bitmap)
                using (Bitmap res = BitmapPreprocessor?.Invoke(src) ?? src)
                    res.Save(tmp, ImageFormat.Png);

                path.Delete();

                File.Move(tmp, path.FullName);
            }
        }

        protected override void InternalDispose()
        {
        }

        public override void Save()
        {
            using (FileStream fs = new FileStream(dbnfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.SetLength(0);
                fs.Seek(0, SeekOrigin.Begin);

                ser.Serialize(fs, new BrickDB
                {
                    Bricks = bricks.Values.OrderBy(b => b?.DesignID).ToArray(),
                    Colors = colors.Values.OrderBy(c => c?.ID).ToArray(),
                });
            }
        }

        public override void ClearIndex()
        {
            base.ClearIndex();

            bricks.Clear();
            colors.Clear();
        }

        public override void Load()
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

        public override void LoadMerge()
        {
            if (dbnfo.Exists)
            {
                BrickDB db;

                using (FileStream fs = dbnfo.OpenRead())
                    db = ser.Deserialize(fs) as BrickDB;

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

                foreach (int id in bricks.Keys.ToArray())
                    if ((id <= 0) || (bricks[id]?.Variations == null))
                        bricks.Remove(id);
                    else if ((bricks[id].FetchDate < now))
                        AddUpdate(id);

                foreach (int id in colors.Keys.ToArray())
                    if (colors[id].FetchDate < now)
                        AddUpdate(id);

                Print($"{bricks.Count} bricks loaded.");
                Print($"{colors.Count} colors loaded.");
            }
        }
        
        private T TryWebAction<T>(Func<T> f, int count = 3, int timeout = 9000)
        {
            WebException last = null;
            int @try = 0;

            while (@try < count)
                try
                {
                    if (@try == count - 1)
                        Manager.ResetWebClient();

                    return f();
                }
                catch (WebException ex)
                {
                    int code = (int?)(ex.Response as HttpWebResponse)?.StatusCode ?? 200;

                    if ((code == 400) ||
                        (code == 401) ||
                        (code >= 500))
                    {
                        Print($"   attempt no.{++@try}");
                        Thread.Sleep(timeout / count);

                        last = ex;
                    }
                    else
                        throw;
                }

            Thread.Sleep(30000); // "cool down" web polls ... I should prob. do smth. better than 'thread::sleep' -__-

            Manager.ResetWebClient();

            return last is null ? default(T) : throw last;
        }

        private void DownloadFile(string url, string path) => TryWebAction(() =>
        {
            if (url.StartsWith("//"))
                url = "http:" + url;

            Manager.WebClient.DownloadFile(url, path);

            return null as object;
        });

        private string DownloadString(string url, int id = -1) => TryWebAction(() =>
        {
            Manager.SetBricksetHeaders(id);

            return Manager.WebClient.DownloadString(url);
        });

        public BricksetDotCom(string dir, LEGODatabaseManager manager) : base(dir, manager) => Load();
    }
    
    // partial implementation for http://brickowl.com/
    public sealed class BrickowlDotCom
        : Debuggable
        , ILEGOPriceDatabase
    {
        public const string URL_SEARCH = "http://www.brickowl.com/search/catalog?query={0}&cat=1";

        private Dictionary<int, (float Min, float Max, float Avg, long Date)> prices = new Dictionary<int, (float, float, float, long)>();

        public override string Name => "brickowl.com";
        public long CacheExpriration => 60 * 60 * 24 * 7; // update once a week
        public bool IsDisposed { private set; get; } = false;


        public LEGODatabaseManager Manager { internal set; get; }

        internal BricksetDotCom Brickset => Manager.GetProvider<BricksetDotCom>();

        public bool CanOperate
        {
            get
            {
                using (Ping p = new Ping())
                    return p.Send(Name).Status == IPStatus.Success;
            }
        }

        private (int Design, int Part) FetchID(int ID) =>
            (LEGODatabaseProvider.IsPartID(ID) ? Brickset.GetDesignID(ID) : ID, Brickset[ID]?.Variations?.FirstOrDefault()?.PartID ?? ID);

        private (CQ dom, int DesignID, int PartID) SearchFor(int ID, bool use_brickset = true)
        {
            (int desg_id, int part_id) = use_brickset ? FetchID(ID) : (ID, ID);

            Print($"Searching for {ID}...");

            CQ dom = Manager.WebClient.DownloadString(string.Format(URL_SEARCH, part_id > 0 ? part_id : desg_id));
            string piece_url = dom["ul.category-grid a.category-item-image[href]"].Attr("href");

            return (Manager.WebClient.DownloadString("http://www.brickowl.com" + piece_url), desg_id, part_id);
        }

        public BrickInfo FetchBrick(int ID)
        {
            if (ID > 0)
                try
                {
                    (CQ dom, int _, int _) = SearchFor(ID, false);

                    Print($"Fetching brick data for {ID}...");

                    var meta = dom["#item-right"];
                    var title = dom["#page-title.title.item.name"].Text();

                    if (title.match(@"(?<name>.*)\([0-9_\-\s\\\/]+\)", out Match m))
                        title = m.Groups["name"].ToString();

                    title = title.Replace("LEGO", "").Trim();

                    var cat = meta.Find("p a[href]").Text();
                    var props = meta.Find("div").ToList();
                    var prod_dt = (from Match ma in Regex.Matches(props[0].Cq().Text(), @"(\b|\s)+[0-9]{4}(\b|\s)+")
                                   select int.Parse(ma.ToString())).ToArray();
                    var des_id_node = meta.Clone()
                                          .Children()
                                          .Remove()
                                          .End()
                                          .Html(); // see https://stackoverflow.com/a/8851526/3902603 for the black magic involved

                    des_id_node = trimat(des_id_node, "Peeron");
                    des_id_node = trimat(des_id_node, "BOID");

                    int desid = (from Match ma in Regex.Matches(des_id_node, @"(\b|\s)+[0-9]{5,8}(\b|\s)+")
                                 select int.Parse(ma.ToString())).Concat(new[] { -1 }).First();

                    string trimat(string src, string frag)
                    {
                        int ndx = src.ToLower().IndexOf(frag.ToLower());

                        if (ndx >= 0)
                            src = src.Remove(ndx);

                        return src;
                    }
                    BrickInfo brick = new BrickInfo
                    {
                        Name = title,
                        DesignID = desid,
                        Category = cat,
                        ProductionDate = prod_dt,
                        FetchDate = DateTime.Now.Ticks,
                    };

                    FetchVariations(ref brick, dom);
                
                    return brick;
                }
                catch (Exception ex)
                {
                    ex.Err();
                }

            return null;
        }

        public void FetchVariations(ref BrickInfo brick, CQ dom)
        {
            List<BrickVariation> vars = new List<BrickVariation>();

            foreach (var row in dom["div#colors table tbody tr"])
            {
                CQ[] td = (from tde in row.Cq().Find("td") select tde.Cq()).ToArray();

                if (td.Length > 0)
                    foreach (int id in from Match ma in Regex.Matches(td[4].Html(), @"[0-9]+")
                                       select int.Parse(ma.ToString()))
                    {
                        var img_url = td[0].Find("a[href]").Attr("href");
                        BrickVariation var = new BrickVariation
                        {
                            PartID = id,
                            ProductionDate = brick.ProductionDate.Clone() as int[],
                        };
                        var clr_str = td[1].Text();
                        
                        if (clr_str.match(@"(lego\b)?(?<name>.*)\([0-9_\-\s\\\/]+\)", out Match m))
                            clr_str = m.Groups["name"].ToString().Trim();

                        int cndx = clr_str.ToLower().IndexOf(brick.Name.ToLower());

                        if (cndx >= 0)
                            clr_str = clr_str.Remove(cndx);

                        ColorInfo clr = Brickset.FindColor(clr_str.Trim());

                        var.ColorID = clr?.ID ?? -1;

                        Brickset.DownloadImage(img_url, var.PartID);

                        vars.Add(var);
                    }
            }

            brick.Variations = vars.ToArray();
        }
        
        public void UpdatePrice(int ID, bool force = false)
        {
#if USE_OLD_BRICKOWL_IMPL
            if (LEGODatabaseProvider.IsPartID(ID))
                try
                {
                    Print($"Fetching price for {ID}...");

                    CQ dom = Manager.WebClient.DownloadString(string.Format(URL_SEARCH, ID));
                    string piece_url = dom["ul.category-grid a.category-item-image[href]"].Attr("href");

                    CQ piece = Manager.WebClient.DownloadString("http://www.brickowl.com" + piece_url);

                    string lo = piece["span[itemprop=lowPrice] span.price"].Html();
                    string hi = piece["span[itemprop=highPrice] span.price"].Html();
                    string curr = piece["meta[itemprop=priceCurrency]"].Attr("content");

                    if (string.IsNullOrWhiteSpace(curr))
                        curr = "EUR";

                    if (string.IsNullOrWhiteSpace(lo))
                        lo = piece["meta[itemprop=lowPrice]"].Attr("content");

                    if (string.IsNullOrWhiteSpace(hi))
                        hi = piece["meta[itemprop=highPrice]"].Attr("content");

                    curr = curr.ToLower();
                    lo = (lo ?? "").ToLower().Replace(curr, "").Trim();
                    hi = (hi ?? "").ToLower().Replace(curr, "").Trim();

                    if (!float.TryParse(lo, out float price))
                    {
                        if (!float.TryParse(hi, out price))
                            price = float.NaN;
                    }
                    else if (float.TryParse(hi, out float aux))
                        price = (price * .25f) + (aux * .75f);

                    prices[ID] = (price, DateTime.Now.Ticks);

                    Save(ID);
                }
                catch (Exception ex)
                {
                    ex.Err();
                }
            else
                foreach (BrickVariation var in Brickset[ID]?.Variations ?? new BrickVariation[0])
                    if ((var != null) && (var.PartID != ID))
                        UpdatePrice(var.PartID);
#else
            (int desg_id, int part_id) = FetchID(ID);

            if (!force && prices.ContainsKey(part_id))
            {
                var prc = prices[part_id];
                long now = DateTime.Now.AddSeconds(-CacheExpriration).Ticks;
                
                if (!float.IsNaN(prc.Min) && !float.IsNaN(prc.Avg) && !float.IsNaN(prc.Max) && (prc.Date >= now))
                    return;
            }

            try
            {
                Print($"Fetching price for {desg_id}/{part_id}...");
                
                (CQ piece, int _, int _) = SearchFor(part_id);

                string lo = piece["span[itemprop=lowPrice] span.price"].Html();
                string hi = piece["span[itemprop=highPrice] span.price"].Html();
                string curr = piece["meta[itemprop=priceCurrency]"].Attr("content");

                if (string.IsNullOrWhiteSpace(curr))
                    curr = "EUR";

                if (string.IsNullOrWhiteSpace(lo))
                    lo = piece["meta[itemprop=lowPrice]"].Attr("content");

                if (string.IsNullOrWhiteSpace(hi))
                    hi = piece["meta[itemprop=highPrice]"].Attr("content");

                curr = curr.ToLower();
                lo = (lo ?? "").ToLower().Replace(curr, "").Trim();
                hi = (hi ?? "").ToLower().Replace(curr, "").Trim();

                float p_avg;

                if (!float.TryParse(lo, out float p_lo))
                    p_lo = float.NaN;

                if (!float.TryParse(hi, out float p_hi))
                    p_hi = float.NaN;

                if (!float.IsNaN(p_lo))
                {
                    p_avg = p_lo;

                    if (!float.IsNaN(p_hi))
                        p_avg = (p_avg * .85f) + (p_hi * .15f);
                }
                else
                    p_avg = p_hi;
                
                foreach (BrickVariation var in Brickset[ID]?.Variations ?? new BrickVariation[0])
                    if (var != null)
                    {
                        prices[ID] = (p_lo, p_hi, p_avg, DateTime.Now.Ticks);

                        Save(ID);
                    }
            }
            catch (Exception ex)
            {
                ex.Err();
            }
#endif
        }

        public void UpdateOld()
        {
            long now = DateTime.Now.AddSeconds(-CacheExpriration).Ticks;

            foreach (int id in prices.Keys.ToArray())
                UpdatePrice(id);

            try
            {
                Brickset.Save();
            }
            catch
            {
            }
        }

        public void Load()
        {
            BricksetDotCom bs = Brickset;
            
            bs.LoadMerge();
            
            prices = (from kvp in bs.bricks
                      let brick = kvp.Value
                      from @var in brick?.Variations ?? new BrickVariation[0]
                      where var != null
                      select (id: var.PartID, min: var.PriceMin, avg: var.PriceAvg, max: var.PriceMax, dat: brick.FetchDate))
                     .ToDictionary(v => v.id, v => (v.min, v.max, v.avg, v.dat));

            UpdateOld();

            Print($"{prices.Count * 3} prices loaded.");
        }

        public void Save()
        {
            foreach (int id in prices.Keys.ToArray())
                Save(id);
        }

        public void Save(int ID)
        {
            BrickInfo brick = Brickset[ID];

            if (brick != null)
            {
                foreach (BrickVariation var in brick.Variations ?? new BrickVariation[0])
                    if (var != null)
                    {
                        var.PriceMin = prices[ID].Min;
                        var.PriceAvg = prices[ID].Avg;
                        var.PriceMax = prices[ID].Max;
                    }

                brick.FetchDate = prices[ID].Date;
            }
        }

        public void ClearAll()
        {
            prices?.Clear();

            foreach (int id in Brickset.bricks.Keys)
                Brickset[id].ResetPrice();
        }

        public void Dispose()
        {
            if (!IsDisposed)
                Save();

            IsDisposed = true;
        }

        public BrickowlDotCom(string dir, LEGODatabaseManager manager)
        {
            Manager = manager;

            Load();
        }
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

        public override string ToString() => $"{Name} ({ID}, {Family}, {Type}, {RGB})";
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
        [XmlIgnore]
        public float PriceMin => Variations?.Select(_ => _.PriceMin)?.Average() ?? float.NaN;
        [XmlIgnore]
        public float PriceMax => Variations?.Select(_ => _.PriceMax)?.Average() ?? float.NaN;

        internal void ResetPrice()
        {
            foreach (BrickVariation var in Variations ?? new BrickVariation[0])
                if (var != null)
                {
                    var.PriceAvg = float.NaN;
                    var.PriceMin = float.NaN;
                    var.PriceMax = float.NaN;
                }
        }

        public override string ToString() => $"{Name} ({DesignID}, {Category}; {string.Join("-", ProductionDate.Take(2))}; {Variations?.Length ?? 0} variations; ~{PriceAvg:N2}€, {PriceMin:N2}...{PriceMax:N2}€)";
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
        public float PriceMin { set; get; } = float.NaN;
        [XmlAttribute]
        public float PriceAvg { set; get; } = float.NaN;
        [XmlAttribute]
        public float PriceMax { set; get; } = float.NaN;

        public override string ToString() => $"Color/ID: {ColorID}/{PartID}  (~{PriceAvg:N2}€, {PriceMin:N2}...{PriceMax:N2}€)";
    }
}
