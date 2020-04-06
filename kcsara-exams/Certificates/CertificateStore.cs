using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SarData.Server.Apis.TableStore;

namespace Kcsara.Exams.Certificates
{
  public class CertificateStore
  {
    private static readonly string TABLE_NAME = "completedexams";

    private readonly ITableStore tableStore;
    private readonly ILogger<CertificateStore> logger;
    private readonly string url;
    private MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    public CertificateStore(ITableStore tableStore, IConfiguration config, ILogger<CertificateStore> logger)
    {
      this.tableStore = tableStore;
      this.logger = logger;
      this.url = config["siteRoot"] ?? "https://exams.kcsara.org";
    }

    internal async Task<RenderedPdf> GetCertificate(string id)
    {
      var existing = cache.Get<RenderedPdf>(id);
      if (existing == null)
      {
        var data = await tableStore.GetRow<CertificateEntity>(TABLE_NAME, id);
        if (data != null)
        {
          existing = await RenderCertificate(data);
          cache.Set(data.RowKey, existing, TimeSpan.FromMinutes(30));
        }
      }
      return existing;
    }

    public async Task AddCertificate(CertificateEntity data)
    {
      var existing = cache.Get<RenderedPdf>(data.RowKey);
      if (existing != null) throw new DuplicateTableRowException();

      logger.LogInformation("Storing certificate " + data.RowKey);
      await tableStore.InsertRow(TABLE_NAME, data);
      return;
    }

    private Task<RenderedPdf> RenderCertificate(CertificateEntity entity)
    {
      using MemoryStream ms = new MemoryStream();
      PdfDocument pdfDoc = new PdfDocument(new PdfWriter(ms));

      Document doc = new Document(pdfDoc, new PageSize(PageSize.LETTER.GetHeight(), PageSize.LETTER.GetWidth()));
      PageSize ps = pdfDoc.GetDefaultPageSize();
      doc.SetMargins(25f, 0f, 25f, 0f);

      PdfFont times = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
      PdfFont helvetica = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

      doc.SetFont(times).SetFontSize(34);
      doc.ShowTextAligned("King County Search And Rescue Association", ps.GetWidth() / 2, ps.GetHeight() - 100, TextAlignment.CENTER);

      var imgName = typeof(CertificateStore).Assembly.GetManifestResourceNames().Single(f => f.EndsWith(".kcsara_logo_color.jpg"));
      using (var stream = typeof(CertificateStore).Assembly.GetManifestResourceStream(imgName))
      {
        var logoData = new byte[stream.Length];
        stream.Read(logoData, 0, logoData.Length);
        doc.Add(new Image(ImageDataFactory.CreateJpeg(logoData), (ps.GetWidth() -172)/ 2, 330, 172));
      }

      doc.SetFont(helvetica).SetFontSize(18);
      doc.ShowTextAligned("This Certificate of Achievement is to acknowledge that", ps.GetWidth() / 2, 282, TextAlignment.CENTER);

      doc.SetFont(times).SetFontSize(30);
      doc.ShowTextAligned(entity.Name, ps.GetWidth() / 2, 230, TextAlignment.CENTER);

      doc.ShowTextAligned(
        new Paragraph().Add("has reaffirmed a dedication to serve the public, through continued\nprofessional development, and completion of a written exam for:")
          .SetFont(helvetica).SetFontSize(18).SetTextAlignment(TextAlignment.CENTER).SetMultipliedLeading(1.3f),
        ps.GetWidth() / 2, 160, TextAlignment.CENTER);

      doc.ShowTextAligned(new Paragraph().Add(entity.Title).SetFont(times).SetFontSize(26).SetTextAlignment(TextAlignment.CENTER),
        ps.GetWidth() / 2, 120, TextAlignment.CENTER);
      
      doc.ShowTextAligned(new Paragraph().Add(string.Format("Issued this {0}{1} Day of {2:MMMM, yyyy}", entity.Completed.Day, GetOrdinal(entity.Completed.Day), entity.Completed)).SetFont(helvetica).SetFontSize(15).SetTextAlignment(TextAlignment.CENTER),
        ps.GetWidth() / 2, 76, TextAlignment.CENTER);

      Color grey = new DeviceRgb(200, 200, 200);
      PdfCanvas canvas = new PdfCanvas(pdfDoc.GetFirstPage());

      doc.SetFont(helvetica).SetFontSize(7).SetFontColor(grey);
      doc.ShowTextAligned(string.Format("Submission {0}", entity.RowKey), 75, 28, TextAlignment.LEFT);
      doc.ShowTextAligned(url, ps.GetWidth() - 75, 28, TextAlignment.RIGHT);

      doc.Close();

      return Task.FromResult(new RenderedPdf
      {
        Data = ms.ToArray(),
        FileName = entity.Title.ToLowerInvariant().Replace(" ", "-") + ".pdf"
      });
    }

    private string GetOrdinal(int number)
    {
      if (number > 10 && number < 14) return "th";
      return (number % 10) switch
      {
        1 => "st",
        2 => "nd",
        3 => "rd",
        _ => "th",
      };
    }
  }
}
