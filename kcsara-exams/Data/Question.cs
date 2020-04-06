using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kcsara.Exams.Data
{
  public class Question
  {
    public string Id { get; set; }
    public string Text { get; set; }
    public bool Randomize { get; set; }
    public IList<Answer> Answers { get; set; } = new List<Answer>();
  }
}
