using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kcsara.Exams.Data
{
  public class Quiz
  {
    public string Id { get; set; }
    public string RecordsId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Visible { get; set; } = true;

    public bool Randomize { get; set; } = true;
    public IList<Question> Questions { get; set; } = new List<Question>();
  }
}
