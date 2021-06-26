using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Kcsara.Exams.Certificates;
using Kcsara.Exams.Data;
using Kcsara.Exams.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SarData.Common.Apis.Database;
using SarData.Common.Apis.Database.Training;
using SarData.Common.Apis.Messaging;

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
    public async Task<IActionResult> Finish(string quizId, PresentExamModel formModel, [FromServices] IConfiguration configuration, [FromServices] IMessagingApi messaging, [FromServices] IDatabaseApi database)
    {
      var quiz = store.Quizzes.FirstOrDefault(f => f.Id.Equals(quizId, StringComparison.OrdinalIgnoreCase) || f.Title.Replace(" ", "-").Equals(quizId, StringComparison.OrdinalIgnoreCase));
      if (quiz == null)
      {
        logger.LogWarning("Can't find exam with id " + quizId);
        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
      }
      
      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState);
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
        Name = User.FindFirst("name")?.Value ?? Request.Form["Name"],
        Email = User.FindFirst("email")?.Value ?? Request.Form["Email"],
        MemberId = User.FindFirst("memberId")?.Value,

        Title = quiz.Title,
        QuizId = quiz.Id,

        Score = quiz.Questions.Count - numWrong,
        Possible = quiz.Questions.Count,

        Completed = DateTimeOffset.Now,
        Incorrect = incorrect
      };

      var passing = configuration.GetValue<float?>("passing_score") ?? 80;
      model.Percentage = model.Score / (float)model.Possible * 100.0f;
      model.Passed = model.Percentage >= passing;

      model.Duration = model.Completed - DateTimeOffset.Parse(Request.Form["Started"].Single());

      if (model.Passed)
      {
        var cert = await certificateStore.AddCertificate(new CertificateEntity
        {
          RowKey = model.Id.ToString().ToLowerInvariant(),
          Completed = model.Completed,
          Name = model.Name,
          Title = model.Title
        });

        // Send email to user
        string message = $"Full Name: {model.Name}<br/>Email: {model.Email}<br/><br/>Course: {model.Title}<br/>Results: {model.Score} out of {model.Possible}.<br/><br/>A certificate of completion is attached.<br/><br/><br/>--<br/>KCSARA Training Committee<br/>training@kcsara.org";
        await messaging.SendEmail(model.Email, "KCSARA Online Exam Results", message, new List<MessageAttachment>
        {
          new MessageAttachment { Base64 = Convert.ToBase64String(cert.Data), FileName = cert.FileName, MimeType = cert.MimeType }
        });

        if (Guid.TryParse(quiz.RecordsId, out Guid courseId) && Guid.TryParse(model.MemberId, out Guid memberId))
        {
          var record = await database.CreateTrainingRecord(new TrainingRecord
          {
            Completed = model.Completed,
            Course = new NameIdPair { Id = courseId },
            Member = new NameIdPair { Id = memberId },
            Comments = $"{configuration["siteRoot"]?.TrimEnd('/') ?? "https://exams.kcsara.org"}/certificate/{model.Id}"
          });
        }
      }

      return View(model);
    }
  }
}
