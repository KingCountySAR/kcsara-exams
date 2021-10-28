using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Kcsara.Exams.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Kcsara.Exams.Data;
using SarData.Common.Apis.Database;

namespace Kcsara.Exams.Controllers
{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
      _logger = logger;
    }

    [HttpGet("/")]
    [HttpGet("/exams")]
    public IActionResult Index([FromServices] QuizStore store)
    {
      return View(store.Quizzes
        .OrderBy(f => f.Title)
        .Where(f => f.Visible)
        .Select(f => new QuizListModel
        {
          Id = f.Id,
          Title = f.Title,
          Enabled = f.Enabled,
          RecordsId = f.RecordsId
        }));
    }

    [Authorize]
    public IActionResult Privacy()
    {
      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("signin")]
    public IActionResult Signin(string returnUrl)
    {
      return Challenge(new AuthenticationProperties {
        RedirectUri = returnUrl ?? "/"
      });
    }

    [Authorize]
    [HttpGet("signout")]
    public IActionResult Signout()
    {
      return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }
  }
}
