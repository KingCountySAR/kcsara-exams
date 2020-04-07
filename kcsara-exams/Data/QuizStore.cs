using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SarData;

namespace Kcsara.Exams.Data
{
  public class QuizStore
  {
    public List<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public static QuizStore init(string localFiles)
    {
      QuizStore store = new QuizStore();

      string path = Path.Combine(localFiles ?? ".", "exams.json");
      if (File.Exists(path))
      {
        string json = File.ReadAllText(path);
        store.Quizzes = JsonSerializer.Deserialize<List<Quiz>>(json, new JsonSerializerOptions().Setup());
      }
      return store;
    }
  }
}
