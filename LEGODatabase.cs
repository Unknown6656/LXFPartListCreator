using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Xml.Schema;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

using CsQuery;

namespace LXFPartListCreator
{
    public abstract class LEGODatabase
        : IDisposable
    {
        public const string FILE_INDEX = "index.db";
        public const string FILE_IMAGE = "part-{0}.dat";

        protected readonly FileInfo dbnfo;
        protected readonly DirectoryInfo dbdir;


        protected bool IsDisposed { private set; get; } = false;

        public abstract BrickInfo this[int ID] { internal set; get; }


        public abstract ColorInfo GetColor(int ID);

        public abstract string GetImageByPartID(int? partID);

        public abstract string GetImageByDesignID(int designID, int colorID = -1);

        protected abstract void InternalDispose();

        public abstract void Save();

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
                throw new ObjectDisposedException(nameof(BricksetDatabase));
            else
                return f();
        }

        protected void Exec(Action f)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(BricksetDatabase));
            else
                f();
        }

        public LEGODatabase(string dir)
        {
            dbdir = new DirectoryInfo(dir);

            if (!dbdir.Exists)
                dbdir.Create();

            dbnfo = new FileInfo($"{dbdir.FullName}/{FILE_INDEX}");
        }
    }

    // implementation for http://brickset.com/
    public sealed class BricksetDatabase
        : LEGODatabase
    {
        public const string URL_DESIGN = "https://brickset.com/parts/design-{0}";
        public const string URL_PART = "https://brickset.com/parts/{0}/";

        internal static readonly XmlSerializer ser = new XmlSerializer(typeof(BrickDB));

        private readonly Dictionary<int, ColorInfo> colors = new Dictionary<int, ColorInfo>();
        private readonly Dictionary<int, BrickInfo> bricks = new Dictionary<int, BrickInfo>();
        private readonly WebClient wc = new WebClient();


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
        
        public override ColorInfo GetColor(int ID) => Exec(() => colors.ContainsKey(ID) ? colors[ID] : null);

        public override string GetImageByDesignID(int designID, int colorID = -1) => Exec(() =>
        {
            BrickInfo nfo = this[designID];
            BrickVariation v = nfo?.Variations?.FirstOrDefault(var => var?.ColorID == colorID) ?? nfo?.Variations?.FirstOrDefault();

            return GetImageByPartID(v?.PartID);
        });

        public override string GetImageByPartID(int? partID) => Exec(() => partID is int id ? ImgGetB64(string.Format(dbdir.FullName + '/' + FILE_IMAGE, id)) : "");

        private int GetDesignID(int partID)
        {
            var pids = from b in bricks
                       where b.Value.Variations?.Any(v => v?.PartID == partID) ?? false
                       select b.Key;

            if (pids.Any())
                return pids.First();
            else
            {
                CQ dom = wc.DownloadString(string.Format(URL_PART, partID));
                var id = dom["section.main img.partimage[alt]"][0]["alt"];

                return int.Parse(id);
            }
        }

        private void AddUpdate(int designID)
        {
            try
            {
                Console.WriteLine($"Fetching design No.{designID} ...");

                string html = wc.DownloadString(string.Format(URL_DESIGN, designID));
                CQ dom = html;

                var cs_iteminfo = "section.main div.iteminfo";

                var iteminfo = dom[cs_iteminfo];
                var img = dom[cs_iteminfo + " img[src]"][0]?["src"];
                var col2 = dom[cs_iteminfo + " div.col"].ToList()?[1].Cq();
                var name = col2.Find("h1")[0].InnerHTML + "<br/>";
                var table = col2.Find("dd").ToList();

                name = name.Remove(name.ToLower().IndexOf("<br"));

                this[designID] = new BrickInfo
                {
                    Name = name,
                    DesignID = designID,
                    ProductionDate = GetYear(table[2].TextContent),
                    FetchDate = DateTime.Now.Ticks,
                    Variations = FetchVariations(dom["section.main div.partlist > ul li.item"].ToList().ToArray() ?? new IDomObject[0])
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
                    var a = variation.Cq().Find("ul li a[href] img[title]").ToList();
                    string parturl = a[0]["src"];
                    int partid = parturl.match(@"\/(?<id>[0-9]+)\.(jpe?g|(d|p)ng|bmp|gif|tiff)$", out Match m) ? int.Parse(m.Groups["id"].ToString()) : -1;

                    if (partid != -1)
                    {
                        Console.WriteLine($"Fetching part No.{partid} ...");

                        DownloadImage(parturl, partid);

                        CQ dom = wc.DownloadString(string.Format(URL_PART, partid));
                        var table = dom["section.featurebox div.text"].Find("dl dd").ToList();
                        var prices = from elem in dom["section.buy span.price a"].ToList()
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

            return vlist.ToArray();
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
                wc.DownloadFile(uri, path.FullName);
        }

        protected override void InternalDispose() => wc?.Dispose();

        public override void Save()
        {
            if (dbnfo.Exists)
                dbnfo.Delete();

            using (FileStream fs = dbnfo.Create())
                ser.Serialize(fs, new BrickDB
                {
                    Bricks = bricks.Values.ToArray(),
                    Colors = colors.Values.ToArray(),
                });
        }

        public override void ClearIndex()
        {
            base.ClearIndex();

            bricks.Clear();
            colors.Clear();
        }
        
        public BricksetDatabase(string dir)
            : base(dir)
        {
            if (dbnfo.Exists)
                try
                {
                    using (FileStream fs = dbnfo.OpenRead())
                    {
                        BrickDB db = ser.Deserialize(fs) as BrickDB;

                        bricks = db.Bricks.ToDictionary(b => b.DesignID, b => b);
                        colors = db.Colors.ToDictionary(b => b.ID, b => b);
                    }
                }
                catch (Exception ex)
                {
                    ex.Err();
                }
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
        public int PartID => Variations?.Select(_ => _.PartID)?.FirstOrDefault() ?? -1;
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
