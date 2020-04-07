using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kcsara.Exams.Models
{
  public class QuizListModel
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public bool Enabled { get; set; }

    public string RecordsId { get; set; }
  }
}
