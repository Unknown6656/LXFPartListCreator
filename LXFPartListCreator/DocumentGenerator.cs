using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Newtonsoft.Json;

using NSass;

using XApplication = Microsoft.Office.Interop.Excel.Application;

namespace LXF
{
    using Properties;


    public abstract class DocumentGenerator
        : IDisposable
    {
        public LXFML Model { set; get; }
        public string Thumbnail { set; get; }
        public Action<string> Print { set; get; }
        public Dictionary<string, string> Options { set; get; }

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

        public virtual void Dispose()
        {
        }

        [Serializable]
        public struct IterationData
        {
            public (int Count, int ID, short[] Matr, short[] Decor, IEnumerable<LXFMLBricksBrick> Bricks) parts { set; get; }

            public int IterationCount { set; get; }
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

    public unsafe sealed class HTMLDocument
        : DocumentGenerator
    {
        private int partcount;


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
                ["bricklist_count"] = partcount,
                ["brick_count"] = brickcount,
            });
        }

        public override void AfterIteration(ref StringBuilder sb, int partcnt) => partcount = partcnt;

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

    public unsafe sealed class JSONDocument
        : DocumentGenerator
    {
        private List<dynamic> cont = new List<dynamic>();
        private int partcount;


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

        public override void AfterIteration(ref StringBuilder sb, int partcnt) => partcount = partcnt;

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
                PartlistCount = partcount,
                BrickCount = brickcount,
            };

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }

    public unsafe sealed class XMLDocument
        : DocumentGenerator
    {
        JSONDocument doc = new JSONDocument();


        public override string Extension => ".xml";

        public override void Dispose()
        {
            doc.Dispose();

            base.Dispose();
        }

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

    public unsafe sealed class ExcelDocument
        : DocumentGenerator
    {
        private static readonly Missing missing = Missing.Value;
        private int partcount, row, col;
        private XApplication xapp;
        private Worksheet xsheet;
        private Workbook xbook;
        bool disposed;


        public override string Extension => HasOption("x-type", out string ext) ? ext.ToLower() : ".xls";


        ~ExcelDocument() => Dispose();

        public ExcelDocument()
        {
            if ((xapp = new XApplication()) == null)
                throw new Exception("Microsoft Office Excel Tools 16.0 or higher has not been installed properly.");
            else
            {
                xbook = xapp.Workbooks.Add(missing);
                xsheet = xbook.Worksheets.Item[1] as Worksheet;
            }
        }

        public override void Dispose()
        {
            if (!disposed)
            {
                Marshal.ReleaseComObject(xsheet);
                Marshal.ReleaseComObject(xbook);
                Marshal.ReleaseComObject(xapp);

                base.Dispose();
            }

            disposed = true;
        }

        public override void AfterIteration(ref StringBuilder sb, int partcnt) => partcount = partcnt;

        public override void BeforeIteration(ref StringBuilder sb)
        {
            row = 3;
            col = 1;

            xsheet.Cells[row, col + 1] = "Count";
            xsheet.Cells[row, col + 2] = "Part ID";
            xsheet.Cells[row, col + 3] = "Design ID";
            xsheet.Cells[row, col + 4] = "Name";
            xsheet.Cells[row, col + 5] = "Color name";
            xsheet.Cells[row, col + 6] = "Color #RGB";
            xsheet.Cells[row, col + 7] = "Unit price (min)";
            xsheet.Cells[row, col + 8] = "Unit price (avg)";
            xsheet.Cells[row, col + 9] = "Unit price (max)";
            xsheet.Cells[row, col + 10] = "Price (min)";
            xsheet.Cells[row, col + 11] = "Price (avg)";
            xsheet.Cells[row, col + 12] = "Price (max)";

            xsheet.Cells[++row, col] = "items:";
        }

        public override string GenerateDocument(StringBuilder sb, Price total, int brickcount)
        {
            ++row;

            xsheet.Cells[row, col] = "total:";
            xsheet.Cells[row, col + 1] = "1";
            xsheet.Cells[row, col + 4] = Model?.name ?? "";
            xsheet.Cells[row, col + 10] = $"{total.Min:N2}€";
            xsheet.Cells[row, col + 11] = $"{total.Avg:N2}€";
            xsheet.Cells[row, col + 12] = $"{total.Max:N2}€";

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

            return tmp.FullName;
        }

        public override void Iteration(ref StringBuilder sb, IterationData data)
        {
            xsheet.Cells[row, col + 1] = data.parts.Count.ToString();
            xsheet.Cells[row, col + 2] = data.PartID.ToString();
            xsheet.Cells[row, col + 3] = data.DesignID.ToString();
            xsheet.Cells[row, col + 4] = data.Part?.Name;
            xsheet.Cells[row, col + 5] = data.Color?.Name;
            xsheet.Cells[row, col + 6] = data.Color?.RGB;
            xsheet.Cells[row, col + 7] = $"{data.BrickPrice.Min:N2}€";
            xsheet.Cells[row, col + 8] = $"{data.BrickPrice.Avg:N2}€";
            xsheet.Cells[row, col + 9] = $"{data.BrickPrice.Max:N2}€";
            xsheet.Cells[row, col + 10] = $"{data.PartsPrice.Min:N2}€";
            xsheet.Cells[row, col + 11] = $"{data.PartsPrice.Avg:N2}€";
            xsheet.Cells[row, col + 12] = $"{data.PartsPrice.Max:N2}€";

            ++row;
        }
    }

    public unsafe sealed class RawDocument
        : DocumentGenerator
    {
        public override string Extension => ".raw";


        private List<IterationData> dat;
        private int partcount;


        public override void BeforeIteration(ref StringBuilder sb) => dat = new List<IterationData>();

        public override void Iteration(ref StringBuilder sb, IterationData data) => dat.Add(data);

        public override void AfterIteration(ref StringBuilder sb, int partcnt) => partcount = partcnt;

        public override string GenerateDocument(StringBuilder sb, Price total, int brickcount) => JsonConvert.SerializeObject(new RawData()
        {
            Parts = dat.ToArray(),
            PartCount = partcount,
            BrickCount = brickcount,
            TotalPrice = total,
            Model = Model,
            Options = Options.Select(kvp => (kvp.Key, kvp.Value)).ToArray(),
            Thumbnail = Thumbnail,
            PrintFunc = Convert.ToBase64String(Serializer.Serialize(Print))
        }).Zip().Zip();


        [Serializable]
        public class RawData
        {
            public IterationData[] Parts { get; set; }
            public int PartCount { get; set; }
            public int BrickCount { get; set; }
            public Price TotalPrice { get; set; }
            public LXFML Model { get; set; }
            public (string k, string v)[] Options { get; set; }
            public string Thumbnail { get; set; }
            public string PrintFunc { get; set; }
        }
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
