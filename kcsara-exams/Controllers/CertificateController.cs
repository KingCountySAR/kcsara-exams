using System.Net.Mime;
using System.Threading.Tasks;
using Kcsara.Exams.Certificates;
using Microsoft.AspNetCore.Mvc;

namespace Kcsara.Exams.Controllers
{
  public class CertificateController : Controller
  {
    private readonly CertificateStore certificateStore;

    public CertificateController(CertificateStore certificateStore)
    {
      this.certificateStore = certificateStore;
    }

    [HttpGet("/certificate/{id}")]
    public async Task<IActionResult> GetCertificate(string id)
    {
      var rendered = await certificateStore.GetCertificate(id);
      if (rendered == null) return NotFound();


      Response.Headers.Add("Content-Disposition", new ContentDisposition { FileName = rendered.FileName, Inline = true }.ToString());
      Response.Headers.Add("X-Content-Type-Options", "nosniff");

      return File(rendered.Data, "application/pdf");
    }
  }
}
