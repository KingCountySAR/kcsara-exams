using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kcsara.Exams.Certificates;
using Kcsara.Exams.Data;
using Kcsara.Exams.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Kcsara.Exams.Controllers
{
  public class ExamsController : Controller
  {
    private readonly QuizStore store;
    private readonly CertificateStore certificateStore;
    private readonly ILogger<ExamsController> logger;

    public ExamsController(QuizStore store, CertificateStore certificateStore, ILogger<ExamsController> logger)
    {
      this.store = store;
      this.certificateStore = certificateStore;
      this.logger = logger;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="quizId"></param>
    /// <returns></returns>
    [HttpGet("/exams/{quizId}")]
    public IActionResult Start(string quizId)
    {
      var quiz = store.Quizzes.FirstOrDefault(f => f.Id.Equals(quizId, StringComparison.OrdinalIgnoreCase) || f.Title.Replace(" ", "-").Equals(quizId, StringComparison.OrdinalIgnoreCase));
      if (quiz == null)
      {
        logger.LogWarning("Can't find exam with id " + quizId);
        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
      }

      var model = new PresentExamModel
      {
        Id = Guid.NewGuid(),
        Name = User.FindFirst("name")?.Value,
        Email = User.FindFirst("email")?.Value,
        MemberId = User.FindFirst("memberId")?.Value,
        Started = DateTimeOffset.UtcNow,
        Quiz = quiz
      };
      return View(model);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="quizId"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    [HttpPost("/exams/{quizId}")]
    public IActionResult Finish(string quizId, [FromServices] IConfiguration configuration)
    {
      var quiz = store.Quizzes.FirstOrDefault(f => f.Id.Equals(quizId, StringComparison.OrdinalIgnoreCase) || f.Title.Replace(" ", "-").Equals(quizId, StringComparison.OrdinalIgnoreCase));
      if (quiz == null)
      {
        logger.LogWarning("Can't find exam with id " + quizId);
        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
      }

      var numWrong = 0;
      var incorrect = new List<string>();
      foreach (var question in quiz.Questions)
      {
        if (Request.Form.TryGetValue(question.Id + "[]", out StringValues postedAnswer))
        {
          // user answered the question
          var expected = question.Answers.Where(f => f.Correct).Select(f => f.Id.ToLowerInvariant()).ToArray();
          var actual = postedAnswer.Select(f => f.ToLowerInvariant()).ToArray();

          if (expected.Length != actual.Length)
          {
            incorrect.Add(question.Text);
            numWrong++;
            continue;
          }

          for (int i=0; i<expected.Length; i++)
          {
            if (expected[i] != actual[i])
            {
              incorrect.Add(question.Text);
              numWrong++;
              continue;
            }
          }
        }
        else
        {
          incorrect.Add(question.Text);
          // user did not answer the question
          numWrong++;
        }
      }

      var model = new TestResultsModel
      {
        Id = new Guid(Request.Form["Id"].Single()),
        Name = User.FindFirst("name")?.Value,
        Email = User.FindFirst("email")?.Value,
        MemberId = User.FindFirst("memberId")?.Value,

        Title = quiz.Title,
        QuizId = quiz.Id,

        Score = quiz.Questions.Count - numWrong,
        Possible = quiz.Questions.Count,

        Completed = DateTimeOffset.Now,
        Incorrect = incorrect
      };
            
      var passing = configuration.GetValue<float?>("passing_score") ?? 0.8;
      model.Percentage = model.Score / (float)model.Possible * 100.0f;
      model.Passed = model.Percentage >= passing;

      model.Duration = model.Completed - DateTimeOffset.Parse(Request.Form["Started"].Single());

      var cert = certificateStore.AddCertificate(new CertificateEntity
      {
        RowKey = model.Id.ToString().ToLowerInvariant(),
        Completed = model.Completed,
        Name = model.Name,
        Title = model.Title
      });

      return View(model);
    }
  }
}
