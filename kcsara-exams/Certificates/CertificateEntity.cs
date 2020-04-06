using System;
using Microsoft.Azure.Cosmos.Table;

namespace Kcsara.Exams.Certificates
{
  public class CertificateEntity : TableEntity
  {
    public string Name { get; set; }
    public string Title { get; set; }
    public DateTimeOffset Completed { get; set; }
    public int PdfVersion { get; set; } = 1;
  }
}
